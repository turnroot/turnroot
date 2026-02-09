using System.Collections;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles unit animation setup, blending, and animator layer configuration.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        private const float ANIMATION_BLEND_DURATION = 0.3f;

        private void SetupWalkAnimation(GameObject model, CharacterInstance unit)
        {
            if (!model.TryGetComponent<Animator>(out var animator))
            {
                LogError($"No Animator on '{model.name}'");
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                LogError(
                    $"Animator has no controller on '{model.name}' - check DefaultUnitAnimatorController"
                );
                return;
            }

            var (walkClip, idleClips) = GetAnimationClips(unit);
            var overrideController = new AnimatorOverrideController(
                animator.runtimeAnimatorController
            );

            if (walkClip != null)
            {
                overrideController["Walk"] = walkClip;
            }

            if (idleClips?.Length > 0)
            {
                var idleClip = idleClips[Random.Range(0, idleClips.Length)];
                if (idleClip != null)
                {
                    overrideController["Idle"] = idleClip;
                }
            }

            animator.runtimeAnimatorController = overrideController;
            animator.applyRootMotion = false;
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

        public void BlendToWalkAnimation(Animator animator) => BlendToAnimation(animator, "Walk");

        public void BlendToIdleAnimation(Animator animator) => BlendToAnimation(animator, "Idle");

        private void SetupAnimatorLayers(Animator animator, CharacterInstance unit)
        {
            if (!unit.CharacterTemplate.HasExtraBoneLayer)
            {
                return;
            }

            if (unit.CharacterTemplate.AdditionalBonesMask == null)
            {
                LogWarning(
                    $"{unit.CharacterTemplate.DisplayName}: HasExtraBoneLayer is true but AdditionalBonesMask is not assigned. Extra bones will not animate independently."
                );
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                LogError(
                    $"{unit.CharacterTemplate.DisplayName}: Cannot setup extra bone layer - no animator controller assigned"
                );
                return;
            }

#if UNITY_EDITOR
            ApplyExtraBoneLayerInEditor(animator, unit);
#else
            LogWarning(
                $"{unit.CharacterTemplate.DisplayName}: Extra bone layers require setup in the AnimatorController asset at edit time. Ensure Layer 1 has the AvatarMask assigned in the controller."
            );
#endif
        }

        private void ValidateAdditionalBones(
            Animator animator,
            string[] boneNames,
            string characterName
        )
        {
            if (animator == null || boneNames == null || boneNames.Length == 0)
            {
                return;
            }

            var missingBones = new List<string>();
            foreach (var boneName in boneNames)
            {
                if (
                    !string.IsNullOrEmpty(boneName)
                    && FindBoneRecursive(animator.transform, boneName) == null
                )
                {
                    missingBones.Add(boneName);
                }
            }

            if (missingBones.Count > 0)
            {
                LogWarning(
                    $"{characterName}: Additional bones not found in hierarchy: {string.Join(", ", missingBones)}"
                );
            }
        }

        private Transform FindBoneRecursive(
            Transform parent,
            string boneName,
            int depth = 0,
            HashSet<Transform> visited = null
        )
        {
            const int MAX_DEPTH = 30;
            if (parent == null || string.IsNullOrEmpty(boneName) || depth > MAX_DEPTH)
            {
                return null;
            }

            visited ??= new HashSet<Transform>();
            if (!visited.Add(parent))
            {
                return null;
            }

            if (parent.name == boneName)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                var result =
                    child != null ? FindBoneRecursive(child, boneName, depth + 1, visited) : null;
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        // ===== Helper Methods =====

        private (AnimationClip walkClip, AnimationClip[] idleClips) GetAnimationClips(
            CharacterInstance unit
        )
        {
            if (unit?.CharacterTemplate?.UseDefaultAnimationsAlways == true)
            {
                return (
                    unit.CharacterTemplate.DefaultWalkingAnimation,
                    unit.CharacterTemplate.DefaultIdleAnimations
                );
            }

            var classData = unit?.GetCurrentClass()?.ClassData;
            var classWalkClip = classData?.WalkAnimation;
            var walkClip =
                (classWalkClip != null && classWalkClip)
                    ? classWalkClip
                    : unit?.CharacterTemplate?.DefaultWalkingAnimation;

            var classIdleClips = classData?.IdleAnimations;
            var idleClips =
                (classIdleClips != null && classIdleClips.Length > 0)
                    ? classIdleClips
                    : unit?.CharacterTemplate?.DefaultIdleAnimations;

            return (walkClip, idleClips);
        }

        private void BlendToAnimation(Animator animator, string animationName)
        {
            if (animator == null || !animator.gameObject.activeInHierarchy)
            {
                return;
            }

            var animHash = Animator.StringToHash(animationName);
            if (animator.HasState(0, animHash))
            {
                animator.CrossFade(animHash, ANIMATION_BLEND_DURATION, 0);
            }
        }

#if UNITY_EDITOR
        private void ApplyExtraBoneLayerInEditor(Animator animator, CharacterInstance unit)
        {
            var controllerAsset =
                animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            if (controllerAsset == null || controllerAsset.layers.Length < 2)
            {
                LogError(
                    $"{unit.CharacterTemplate.DisplayName}: AnimatorController needs at least 2 layers for extra bones. Layer 1 is missing."
                );
                return;
            }

            var layers = controllerAsset.layers;
            layers[1].avatarMask = unit.CharacterTemplate.AdditionalBonesMask;
            controllerAsset.layers = layers;

            TurnrootLogger.Log(
                $"Applied AvatarMask '{unit.CharacterTemplate.AdditionalBonesMask.name}' to Layer 1 for {unit.CharacterTemplate.DisplayName}"
            );

            if (unit.CharacterTemplate.AdditionalBoneNames?.Length > 0)
            {
                ValidateAdditionalBones(
                    animator,
                    unit.CharacterTemplate.AdditionalBoneNames,
                    unit.CharacterTemplate.DisplayName
                );
            }
        }
#endif
    }
}
