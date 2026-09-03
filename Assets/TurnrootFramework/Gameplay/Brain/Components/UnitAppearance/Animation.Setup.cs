using System.Collections;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        private void SetupWalkAnimation(GameObject model, CharacterInstance unit)
        {
            if (!model.TryGetComponent<Animator>(out var animator))
            {
                $"Missing Animator on '{model.name}'".LogError("UnitAppearanceBrain");
                return;
            }

            var baseController = animator.runtimeAnimatorController;
            if (baseController == null)
            {
                $"Animator on '{model.name}' has no controller.".LogError("UnitAppearanceBrain");
                return;
            }

            var overrideController = new AnimatorOverrideController(baseController);

            var classData = unit.GetCurrentClass()?.ClassData;
            if (classData == null)
            {
                $"SetupWalkAnimation: '{unit.CharacterTemplate?.DisplayName}' has no class data — default fallback class was not applied. Check GameSettings.DefaultStartingClass and CharacterTemplate.StartingClass.".LogError(
                    "UnitAppearanceBrain"
                );
                return;
            }

            AnimationClip walkClip = classData.WalkAnimation;

            if (walkClip != null)
            {
                // Ensure the walk animation loops at runtime; some imported clips
                // forget to enable Loop Time which causes characters to freeze
                // after a single frame.  We modify the wrap mode here rather than
                // relying on the import settings so prototypes don't break.
                walkClip.wrapMode = WrapMode.Loop;
                overrideController[WalkState] = walkClip;

#if UNITY_EDITOR
                if (!walkClip.isLooping)
                {
                    $"[UnitAppearance] Walk clip '{walkClip.name}' is not set to loop, characters may stop animating during movement.".LogWarning();
                }
#endif
            }

            animator.runtimeAnimatorController = overrideController;
            animator.enabled = true;

            SetupIdleAnimation(animator, unit, overrideController);
        }

        private IEnumerator PlayIdleAnimationNextFrame(Animator animator)
        {
            yield return null;
            if (animator != null && animator.gameObject.activeInHierarchy)
            {
                var idleHash = IdleHash;
                if (animator.HasState(0, idleHash))
                {
                    animator.Play(idleHash, 0, 0f);
                }
            }
        }

        private void SetupIdleAnimation(
            Animator animator,
            CharacterInstance unit,
            AnimatorOverrideController overrideController = null
        )
        {
            if (animator == null)
            {
                return;
            }

            if (overrideController == null)
            {
                var baseController = animator.runtimeAnimatorController;
                if (baseController == null)
                {
                    $"Animator on '{animator.gameObject.name}' has no controller.".LogError(
                        "UnitAppearanceBrain"
                    );
                    return;
                }
                overrideController = new AnimatorOverrideController(baseController);
            }

            var classData = unit.GetCurrentClass()?.ClassData;
            AnimationClip[] idleClips = classData?.IdleAnimations;

            var idleClip =
                idleClips != null && idleClips.Length > 0
                    ? idleClips[Random.Range(0, idleClips.Length)]
                    : null;

            if (idleClip != null)
            {
                overrideController[IdleState] = idleClip;
            }

            animator.runtimeAnimatorController = overrideController;
            animator.enabled = true;

            StartCoroutine(PlayIdleAnimationNextFrame(animator));
            StartCoroutine(IdleVariationRoutine(animator, idleClips));
        }
    }
}
