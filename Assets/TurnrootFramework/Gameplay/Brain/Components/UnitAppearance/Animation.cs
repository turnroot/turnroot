using System.Collections;
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
                TurnrootLogger.Log(
                    $"Missing Animator on '{model.name}'",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var baseController = animator.runtimeAnimatorController;
            if (baseController == null)
            {
                TurnrootLogger.Log(
                    $"Animator on '{model.name}' has no controller.",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var overrideController = new AnimatorOverrideController(baseController);

            AnimationClip walkClip;
            AnimationClip[] idleClips;

            if (unit?.CharacterTemplate?.UseDefaultAnimationsAlways == true)
            {
                // Always use character's default animations, ignore class animations
                walkClip = unit.CharacterTemplate.DefaultWalkingAnimation;
                idleClips = unit.CharacterTemplate.DefaultIdleAnimations;
            }
            else
            {
                // Prefer class animations, fall back to character defaults
                var classData = unit?.GetCurrentClass()?.ClassData;

                // Unity objects can be "null" but not C# null, so we need explicit checks
                var classWalkClip = classData?.WalkAnimation;
                walkClip =
                    (classWalkClip != null && classWalkClip)
                        ? classWalkClip
                        : unit?.CharacterTemplate?.DefaultWalkingAnimation;

                var classIdleClips = classData?.IdleAnimations;
                idleClips =
                    (classIdleClips != null && classIdleClips.Length > 0)
                        ? classIdleClips
                        : unit?.CharacterTemplate?.DefaultIdleAnimations;
            }

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

        /// <summary>
        /// Sets up animator layers for characters with extra bones (tails, wings, etc.).
        /// Applies AvatarMask to layer 1 for independent animation of extra bones.
        /// </summary>
        private void SetupAnimatorLayers(Animator animator, CharacterInstance unit)
        {
            if (!unit.CharacterTemplate.HasExtraBoneLayer)
            {
                return;
            }

            // Validate that mask exists when HasExtraBoneLayer is true
            if (unit.CharacterTemplate.AdditionalBonesMask == null)
            {
                TurnrootLogger.Log(
                    $"{unit.CharacterTemplate.DisplayName}: HasExtraBoneLayer is true but AdditionalBonesMask is not assigned.",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                TurnrootLogger.Log(
                    $"{unit.CharacterTemplate.DisplayName}: Cannot setup extra bone layer - no animator controller assigned.",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            // Check if controller has at least 2 layers
            var controllerAsset = controller as UnityEditor.Animations.AnimatorController;
            if (controllerAsset == null)
            {
                // Runtime - can't modify layers at runtime easily without editor API
                TurnrootLogger.Log(
                    $"{unit.CharacterTemplate.DisplayName}: Extra bone layers require editor-time setup (assign AvatarMask to Layer 1).",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

#if UNITY_EDITOR
            // Editor-time setup
            if (controllerAsset.layers.Length < 2)
            {
                TurnrootLogger.Log(
                    $"{unit.CharacterTemplate.DisplayName}: AnimatorController needs at least 2 layers (Layer 1 missing).",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            // Apply mask to Layer 1
            var layers = controllerAsset.layers;
            layers[1].avatarMask = unit.CharacterTemplate.AdditionalBonesMask;
            controllerAsset.layers = layers;

            // Validate additional bone names if provided
            if (
                unit.CharacterTemplate.AdditionalBoneNames != null
                && unit.CharacterTemplate.AdditionalBoneNames.Length > 0
            )
            {
                ValidateAdditionalBones(
                    animator,
                    unit.CharacterTemplate.AdditionalBoneNames,
                    unit.CharacterTemplate.DisplayName
                );
            }
#endif
        }

        /// <summary>
        /// Validates that all specified additional bones exist in the animator's hierarchy.
        /// </summary>
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

            var root = animator.transform;
            var missingBones = new System.Collections.Generic.List<string>();

            foreach (var boneName in boneNames)
            {
                if (string.IsNullOrEmpty(boneName))
                {
                    continue;
                }

                var bone = FindBoneRecursive(root, boneName);
                if (bone == null)
                {
                    missingBones.Add(boneName);
                }
            }

            if (missingBones.Count > 0)
            {
                TurnrootLogger.Log(
                    $"{characterName}: Additional bones not found in hierarchy: {string.Join(", ", missingBones)}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        private Transform FindBoneRecursive(
            Transform parent,
            string boneName,
            int depth = 0,
            System.Collections.Generic.HashSet<Transform> visited = null
        )
        {
            const int MAX_DEPTH = 30;

            if (parent == null || string.IsNullOrEmpty(boneName) || depth > MAX_DEPTH)
            {
                return null;
            }

            visited ??= new System.Collections.Generic.HashSet<Transform>();

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
                if (child != null)
                {
                    var result = FindBoneRecursive(child, boneName, depth + 1, visited);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
