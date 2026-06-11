using System.Collections;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public partial class HubCharacterManager
    {
        private void BeginUnitTurn(CharacterInstance character)
        {
            if (_activeAvatarPoint == null || character == null || _brain == null)
            {
                return;
            }

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

            unitModel.transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            _turnCoroutine = null;
        }
    }
}
