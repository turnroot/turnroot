using System.Collections;
using System.Linq;
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
    [RequireComponent(typeof(HubCharacterInteraction))]
    public partial class HubCharacterManager : MonoBehaviour
    {
        [BoxGroup("Dialogue")]
        [InfoBox(
            "Per-chapter one-shot dialogue data. At runtime the entry matching the current chapter is used."
        )]
        public HubCharacterOneShotChapter[] ChapterOneshots;

        [BoxGroup("Dialogue")]
        [InfoBox(
            "Per-chapter chitchat conversation data for the Talk interaction. "
                + "A random unplayed conversation is chosen each time the player talks. "
                + "When all conversations for a chapter are exhausted, Talk is disabled until the next chapter."
        )]
        public HubCharacterConversationChapter[] ChapterChitChatConversations;

        [BoxGroup("Interaction")]
        [Tooltip(
            "Reference to the HubCharacterInteraction component. Can be on this GameObject or elsewhere in the scene."
        )]
        public HubCharacterInteraction CharacterInteraction;

        [BoxGroup("Interaction")]
        [Tooltip("When enabled, the avatar model is spawned automatically when traversal starts.")]
        public bool SpawnAvatarOnTraversalStart = true;

        // ── Runtime ──────────────────────────────────────────────────────────

        private CharacterInstance _activeCharacter;
        private GameObject _avatarModel;
        private Transform _activeAvatarPoint;
        private HubManager _hubManager;
        private Coroutine _spawnOnLoadRoutine;

        [HideInInspector]
        public Brain.Brain _brain;
        private AudioBrain _audioBrain;

        private Coroutine _turnCoroutine;

        /// <summary>The character currently being interacted with, or null if none.</summary>
        public CharacterInstance ActiveCharacter => _activeCharacter;

        private void Awake()
        {
            _brain = GetAndCacheBrain.GetBrain();
            _audioBrain = _brain.audioBrain;
            _hubManager = HubManager.GetCurrent();

            if (CharacterInteraction == null)
            {
                CharacterInteraction = GetComponent<HubCharacterInteraction>();
            }
        }

        private void Start()
        {
            if (!SpawnAvatarOnTraversalStart)
            {
                return;
            }

            if (_spawnOnLoadRoutine != null)
            {
                StopCoroutine(_spawnOnLoadRoutine);
            }

            _spawnOnLoadRoutine = StartCoroutine(SpawnAvatarOnTraversalStartRoutine());
        }

        private IEnumerator SpawnAvatarOnTraversalStartRoutine()
        {
            // Wait for brain sub-systems that avatar model creation depends on.
            while (
                _brain == null
                || _brain.gamewideContextBrain == null
                || _brain.unitAppearanceBrain == null
            )
            {
                _brain ??= FindFirstObjectByType<Brain.Brain>();
                yield return null;
            }

            EnsureHubTraversalAvatarSpawned();
            _spawnOnLoadRoutine = null;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns a random chitchat <see cref="Conversation"/> for <paramref name="character"/> in the given
        /// chapter that has not yet been completed, or <c>null</c> if all have been played (or none configured).
        /// </summary>
        public Conversation GetRandomUnplayedChitChatConversation(
            CharacterInstance character,
            int chapterNumber
        )
        {
            if (character?.CharacterTemplate == null || _brain?.conversationalBrain == null)
            {
                return null;
            }

            if (ChapterChitChatConversations == null)
            {
                return null;
            }

            foreach (var chapter in ChapterChitChatConversations)
            {
                if (chapter.ChapterNumber != chapterNumber)
                {
                    continue;
                }

                var all = chapter.GetConversationsForCharacter(character.CharacterTemplate);
                if (all == null || all.Length == 0)
                {
                    return null;
                }

                var unplayed = all.Where(c =>
                        c != null && !_brain.conversationalBrain.HasCompletedConversation(c)
                    )
                    .ToArray();

                return unplayed.Length == 0 ? null : unplayed[Random.Range(0, unplayed.Length)];
            }

            return null;
        }

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

        /// <summary>
        /// Like <see cref="GetRandomOneShotForType"/> but uses <see cref="HubDayRandom"/> so the
        /// choice is deterministic for a given hub day.
        /// </summary>
        public OneShot GetDailyOneShotForType(
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
            return oneShots == null || oneShots.Length == 0
                ? default
                : oneShots[HubDayRandom.Range(0, oneShots.Length)];
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
            SpawnAvatarModel();
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
            DestroyCurrentAvatarModel();

            if (_turnCoroutine != null)
            {
                StopCoroutine(_turnCoroutine);
                _turnCoroutine = null;
            }

            CharacterInteraction?.HideActionsMenu();
            _activeCharacter = null;
            _activeAvatarPoint = null;

            EnsureHubTraversalAvatarSpawned();
        }

        // ── Private helpers ──────────────────────────────────────────────────
    }
}
