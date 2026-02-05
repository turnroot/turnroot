using System.Collections;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        private const float ANIMATION_BLEND_DURATION = 0.2f;

        /// <summary>
        /// Sets up animations for a unit's model. Called once during model creation in ApplyVisuals.
        /// Configures idle and walk animations, defaulting to idle.
        /// </summary>
        private void SetupWalkAnimation(GameObject model, CharacterInstance unit)
        {
            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                TurnrootLogger.Log($"No Animator on '{model.name}'", TurnrootLogger.LogLevel.Error);
                return;
            }

            var baseController = animator.runtimeAnimatorController;
            if (baseController == null)
            {
                TurnrootLogger.Log(
                    $"Animator has no controller on '{model.name}' - check DefaultUnitAnimatorController",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var overrideController = new AnimatorOverrideController(baseController);
            var walkClip = unit?.CharacterTemplate?.DefaultWalkingAnimation;
            var idleClips = unit?.CharacterTemplate?.DefaultIdleAnimations;
            var idleClip =
                idleClips?.Length > 0 ? idleClips[Random.Range(0, idleClips.Length)] : null;

            if (walkClip != null)
            {
                overrideController["Walk"] = walkClip;
            }

            if (idleClip != null)
            {
                overrideController["Idle"] = idleClip;
            }

            animator.runtimeAnimatorController = overrideController;
            animator.enabled = true;

            StartCoroutine(PlayIdleAnimationNextFrame(animator));
        }

        private IEnumerator PlayIdleAnimationNextFrame(Animator animator)
        {
            yield return null;
            if (animator != null && animator.gameObject.activeInHierarchy)
            {
                var idleHash = Animator.StringToHash("Idle");
                if (animator.HasState(0, idleHash))
                {
                    animator.Play(idleHash, 0, 0f);
                }
            }
        }

        public void BlendToWalkAnimation(Animator animator)
        {
            if (animator == null || !animator.gameObject.activeInHierarchy)
            {
                return;
            }

            var walkHash = Animator.StringToHash("Walk");
            if (animator.HasState(0, walkHash))
            {
                animator.CrossFade(walkHash, ANIMATION_BLEND_DURATION, 0);
            }
        }

        public void BlendToIdleAnimation(Animator animator)
        {
            if (animator == null || !animator.gameObject.activeInHierarchy)
            {
                return;
            }

            var idleHash = Animator.StringToHash("Idle");
            if (animator.HasState(0, idleHash))
            {
                animator.CrossFade(idleHash, ANIMATION_BLEND_DURATION, 0);
            }
        }
    }
}
