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

            AnimationClip walkClip;

            if (
                unit.CharacterTemplate != null
                && unit.CharacterTemplate.UseDefaultAnimationsAlways == true
            )
            {
                walkClip = unit.CharacterTemplate.DefaultWalkingAnimation;
            }
            else
            {
                var classData = unit.GetCurrentClass()?.ClassData;
                if (classData == null)
                {
                    $"SetupWalkAnimation: '{unit.CharacterTemplate?.DisplayName}' has no class data — default fallback class was not applied. Check GameSettings.DefaultStartingClass and CharacterTemplate.StartingClass.".LogError(
                        "UnitAppearanceBrain"
                    );
                    walkClip = unit.CharacterTemplate?.DefaultWalkingAnimation;
                }
                else
                {
                    var classWalkClip = classData.WalkAnimation;
                    walkClip =
                        (classWalkClip != null && classWalkClip)
                            ? classWalkClip
                            : unit.CharacterTemplate.DefaultWalkingAnimation;
                }
            }

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

            AnimationClip[] idleClips;
            if (
                unit.CharacterTemplate != null
                && unit.CharacterTemplate.UseDefaultAnimationsAlways == true
            )
            {
                idleClips = unit.CharacterTemplate.DefaultIdleAnimations;
            }
            else
            {
                var classData = unit.GetCurrentClass()?.ClassData;
                var classIdleClips = classData?.IdleAnimations;
                idleClips =
                    (classIdleClips != null && classIdleClips.Length > 0)
                        ? classIdleClips
                        : unit.CharacterTemplate.DefaultIdleAnimations;
            }

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

        private void SetupAnimatorLayers(Animator animator, CharacterInstance unit)
        {
#if UNITY_EDITOR
            if (!unit.CharacterTemplate.HasExtraBoneLayer)
            {
                return;
            }

            if (unit.CharacterTemplate.AdditionalBonesMask == null)
            {
                $"{unit.CharacterTemplate.DisplayName}: HasExtraBoneLayer is true but AdditionalBonesMask is not assigned.".LogWarning(
                    "UnitAppearanceBrain"
                );
                return;
            }

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                $"{unit.CharacterTemplate.DisplayName}: Cannot setup extra bone layer - no animator controller assigned.".LogError(
                    "UnitAppearanceBrain"
                );
                return;
            }

            // Check if controller has at least 2 layers
            var controllerAsset = controller as UnityEditor.Animations.AnimatorController;
            if (controllerAsset == null)
            {
                $"{unit.CharacterTemplate.DisplayName}: Extra bone layers require editor-time setup (assign AvatarMask to Layer 1).".LogWarning(
                    "UnitAppearanceBrain"
                );
                return;
            }

            if (controllerAsset.layers.Length < 2)
            {
                $"{unit.CharacterTemplate.DisplayName}: AnimatorController needs at least 2 layers (Layer 1 missing).".LogError(
                    "UnitAppearanceBrain"
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
                LogWarning(
                    $"{characterName}: Additional bones not found in hierarchy: {string.Join(", ", missingBones)}"
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