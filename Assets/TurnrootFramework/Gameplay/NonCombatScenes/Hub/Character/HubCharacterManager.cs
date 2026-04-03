using System.Collections;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    /// <summary>
    /// Hub-level generalized controller for unit/character interactions.
    /// Place one instance in the hub scene (not on individual POIs — those are spawned dynamically).
    /// When the player selects a unit POI, <see cref="NotifyCharacterVisited"/> receives the character
    /// at runtime; the unit model is resolved via <see cref="UnitAppearanceBrain.GetModelForUnit"/>.
    /// </summary>
    public class HubCharacterManager : MonoBehaviour
    {
        [BoxGroup("Dialogue")]
        [Tooltip(
            "Per-chapter one-shot dialogue data. At runtime the entry matching the current chapter is used."
        )]
        public HubCharacterOneShotChapter[] ChapterOneshots;

        [BoxGroup("Interaction")]
        [Tooltip(
            "Reference to the HubCharacterInteraction component. Can be on this GameObject or elsewhere in the scene."
        )]
        public HubCharacterInteraction CharacterInteraction;

        // ── Runtime ──────────────────────────────────────────────────────────

        private CharacterInstance _activeCharacter;
        private GameObject _avatarModel;
        private Transform _activeAvatarPoint;

        [HideInInspector]
        public Brain.Brain _brain;
        private AudioBrain _audioBrain;

        private Coroutine _turnCoroutine;

        /// <summary>The character currently being interacted with, or null if none.</summary>
        public CharacterInstance ActiveCharacter => _activeCharacter;

        private void Awake()
        {
            _brain = FindFirstObjectByType<Brain.Brain>();
            _audioBrain = _brain?.audioBrain;

            if (CharacterInteraction == null)
            {
                CharacterInteraction = GetComponent<HubCharacterInteraction>();
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns a random one-shot of <paramref name="type"/> for <paramref name="character"/> in the given chapter.
        /// Returns <c>default</c> if no matching dialogue is configured.
        /// </summary>
        public OneShot GetRandomOneShotForType(
            CharacterInstance character,
            int chapterNumber,
            HubCharacterOneShotType type
        )
        {
            if (character == null || _audioBrain == null)
            {
                return default;
            }

            var characterData = character.CharacterTemplate;
            if (characterData == null)
            {
                return default;
            }

            OneShotDialogue[] dialogues = null;
            if (ChapterOneshots != null)
            {
                foreach (var chapter in ChapterOneshots)
                {
                    if (chapter.ChapterNumber == chapterNumber)
                    {
                        dialogues = chapter.GetOneShotsForCharacter(characterData, type);
                        break;
                    }
                }
            }

            if (dialogues == null || dialogues.Length == 0)
            {
                return default;
            }

            var oneShots = _audioBrain.ConvertToOneShots(dialogues, characterData.DisplayName);
            return _audioBrain.GetRandomOneShot(oneShots);
        }

        /// <summary>Returns a random <see cref="HubCharacterOneShotType.StartInteraction"/> one-shot.</summary>
        public OneShot GetRandomWelcomeOneShot(CharacterInstance character, int chapterNumber) =>
            GetRandomOneShotForType(
                character,
                chapterNumber,
                HubCharacterOneShotType.StartInteraction
            );

        /// <summary>Returns a random <see cref="HubCharacterOneShotType.EndInteraction"/> one-shot.</summary>
        public OneShot GetRandomFarewellOneShot(CharacterInstance character, int chapterNumber) =>
            GetRandomOneShotForType(
                character,
                chapterNumber,
                HubCharacterOneShotType.EndInteraction
            );

        /// <summary>
        /// Plays the farewell one-shot for the currently active character.
        /// Returns <c>true</c> if a dialogue was found and played; the caller should wait for the
        /// conversation to finish before calling <c>NotifyCharacterExited</c>.
        /// </summary>
        public bool TriggerFarewellOneShot(int chapterNumber)
        {
            var oneShot = GetRandomFarewellOneShot(_activeCharacter, chapterNumber);
            if (string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                return false;
            }

            var player = _audioBrain?.GetOrCreateOneShotPlayer();
            if (player == null)
            {
                $"HubCharacterManager '{name}': Could not obtain OneShotPlayer for farewell dialogue.".LogWarning();
                return false;
            }

            player.PlayOneShot(oneShot);
            return true;
        }

        /// <summary>
        /// Called when the player selects a unit POI.
        /// Spawns the avatar model, starts the unit turn, and plays the welcome one-shot.
        /// </summary>
        public void NotifyCharacterVisited(
            CharacterInstance character,
            int chapterNumber,
            Transform avatarPoint
        )
        {
            _activeCharacter = character;
            _activeAvatarPoint = avatarPoint;

            CharacterInteraction?.Initialize(character);
            SpawnAvatarModel(character);
            BeginUnitTurn(character);
            PlayWelcomeOneShot(character, chapterNumber);
            _brain?.PublishHubCharacterInteracted(character);
        }

        /// <summary>
        /// Called when the player exits the character interaction.
        /// Destroys the avatar model and hides the actions menu.
        /// </summary>
        public void NotifyCharacterExited()
        {
            if (_avatarModel != null)
            {
                Destroy(_avatarModel);
                _avatarModel = null;
            }

            if (_turnCoroutine != null)
            {
                StopCoroutine(_turnCoroutine);
                _turnCoroutine = null;
            }

            CharacterInteraction?.HideActionsMenu();
            _activeCharacter = null;
            _activeAvatarPoint = null;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void SpawnAvatarModel(CharacterInstance character)
        {
            if (_activeAvatarPoint == null || character == null || _brain == null)
            {
                return;
            }

            if (_avatarModel != null)
            {
                Destroy(_avatarModel);
                _avatarModel = null;
            }

            var avatarInstance = _brain.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatarInstance == null)
            {
                $"HubCharacterManager '{name}': Could not find Avatar character instance in persistent roster.".LogWarning();
                return;
            }

            var model = _brain.unitAppearanceBrain?.CreateModelForUnit(avatarInstance);
            if (model == null)
            {
                $"HubCharacterManager '{name}': Failed to create avatar model for {avatarInstance.CharacterTemplate?.DisplayName}.".LogWarning();
                return;
            }

            model.transform.SetPositionAndRotation(
                _activeAvatarPoint.position,
                _activeAvatarPoint.rotation
            );
            model.transform.SetParent(_activeAvatarPoint, worldPositionStays: true);

            _brain.unitAppearanceBrain.SetupHubIdleAnimation(model, avatarInstance);
            _avatarModel = model;
        }

        private void BeginUnitTurn(CharacterInstance character)
        {
            if (_activeAvatarPoint == null || character == null || _brain == null)
            {
                return;
            }

            // Resolve the already-spawned hub unit model from the appearance brain.
            var unitModel = _brain.unitAppearanceBrain?.GetModelForUnit(character.Id);
            if (unitModel == null)
            {
                return;
            }

            if (_turnCoroutine != null)
            {
                StopCoroutine(_turnCoroutine);
            }

            _turnCoroutine = StartCoroutine(TurnUnitTowardLookPoint(unitModel, character));
        }

        private void PlayWelcomeOneShot(CharacterInstance character, int chapterNumber)
        {
            if (_brain == null || _audioBrain == null)
            {
                return;
            }

            var oneShot = GetRandomWelcomeOneShot(character, chapterNumber);
            if (string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                return;
            }

            var player = _audioBrain.GetOrCreateOneShotPlayer();
            if (player == null)
            {
                $"HubCharacterManager '{name}': Could not obtain OneShotPlayer for welcome dialogue.".LogWarning();
                return;
            }

            player.PlayOneShot(oneShot);
        }

        private IEnumerator TurnUnitTowardLookPoint(
            GameObject unitModel,
            CharacterInstance character
        )
        {
            if (unitModel == null || _activeAvatarPoint == null)
            {
                yield break;
            }

            var direction = _activeAvatarPoint.position - unitModel.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                yield break;
            }

            var targetRotation = Quaternion.LookRotation(direction);
            var turnClip = character?.CharacterTemplate?.HubTurnAnimation;

            if (
                turnClip != null
                && unitModel.TryGetComponent<Animator>(out var anim)
                && anim.runtimeAnimatorController != null
            )
            {
                var overrideController = new AnimatorOverrideController(
                    anim.runtimeAnimatorController
                );
                const string TurnState = "Turn";
                overrideController[TurnState] = turnClip;
                anim.runtimeAnimatorController = overrideController;
                anim.Play(Animator.StringToHash(TurnState), 0, 0f);

                yield return new WaitForSeconds(turnClip.length);

                // Restore idle animation after the turn clip finishes.
                _brain?.unitAppearanceBrain?.SetupHubIdleAnimation(unitModel, character);
            }
            else
            {
                const float TurnDuration = 0.4f;
                var startRotation = unitModel.transform.rotation;
                float elapsed = 0f;

                while (elapsed < TurnDuration)
                {
                    elapsed += Time.deltaTime;
                    unitModel.transform.rotation = Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        Mathf.Clamp01(elapsed / TurnDuration)
                    );
                    yield return null;
                }
            }

            // Enforce Y-axis-only rotation — no X or Z tilt.
            unitModel.transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            _turnCoroutine = null;
        }
    }
}
