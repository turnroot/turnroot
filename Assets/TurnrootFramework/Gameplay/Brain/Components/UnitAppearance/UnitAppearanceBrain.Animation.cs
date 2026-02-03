using System.Collections;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        /// <summary>
        /// Sets up the walk animation for a unit's model.
        /// Called once during model creation in ApplyVisuals.
        /// </summary>
        private void SetupWalkAnimation(GameObject model, CharacterInstance unit)
        {
            var walkClip = unit?.CharacterTemplate?.DefaultWalkingAnimation;
            if (walkClip == null)
            {
                return;
            }

            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                TurnrootLogger.Log(
                    $"No Animator on '{model.name}' - this shouldn't happen",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var baseController = animator.runtimeAnimatorController;
            if (baseController == null)
            {
                TurnrootLogger.Log(
                    $"Animator has no controller on '{model.name}' - check DefaultUnitAnimatorController in settings",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            // Create a simple override controller and set the "Walk" entry directly.
            var overrideController = new AnimatorOverrideController(baseController);
            overrideController["Walk"] = walkClip;
            animator.runtimeAnimatorController = overrideController;

            // Quick check / log in case the controller doesn't contain a clip named exactly "Walk"
            var applied = overrideController.animationClips?.Any(c => c == walkClip) ?? false;
            if (!applied)
            {
                var available = string.Join(
                    ", ",
                    baseController.animationClips?.Select(c => c?.name ?? "(null)") ?? new string[0]
                );
                TurnrootLogger.Log(
                    $"Attempted override by literal state 'Walk' but no clip matched. Available clips: {available}",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            // Ensure the animator is enabled
            animator.enabled = true;

            // Start coroutine to play after animator initializes
            StartCoroutine(
                PlayAnimationNextFrame(animator, unit.CharacterTemplate?.DisplayName ?? "Unknown")
            );
        }

        private IEnumerator PlayAnimationNextFrame(Animator animator, string unitName)
        {
            // Wait for the animator to initialize with the new controller
            yield return null;

            if (animator != null && animator.gameObject.activeInHierarchy)
            {
                // Try to play the Walk state
                var walkHash = Animator.StringToHash("Walk");
                if (animator.HasState(0, walkHash))
                {
                    animator.Play(walkHash, 0, 0f);
                    TurnrootLogger.Log(
                        $"Started walk animation for {unitName}",
                        TurnrootLogger.LogLevel.Info
                    );
                }
                else
                {
                    TurnrootLogger.Log(
                        $"Walk state not found in animator for {unitName} - animation may not play",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
        }
    }
}
