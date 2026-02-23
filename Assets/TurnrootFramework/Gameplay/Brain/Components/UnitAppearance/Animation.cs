using System.Collections;
using Turnroot.Characters;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles unit animation setup, blending, and animator layer configuration.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        private const float ANIMATION_BLEND_DURATION = 0.2f;

        private void SetupWalkAnimation(GameObject model, CharacterInstance unit)
        {
            if (!model.TryGetComponent<Animator>(out var animator))
            {
                LogError($"Missing Animator on '{model.name}'");
                return;
            }

            var baseController = animator.runtimeAnimatorController;
            if (baseController == null)
            {
                LogError($"Animator on '{model.name}' has no controller.");
                return;
            }

            // create a fresh override controller for walk/idle setup
            var overrideController = new AnimatorOverrideController(baseController);

            AnimationClip walkClip;

            if (
                unit.CharacterTemplate != null
                && unit.CharacterTemplate.UseDefaultAnimationsAlways == true
            )
            {
                // always use character defaults, ignore class
                walkClip = unit.CharacterTemplate.DefaultWalkingAnimation;
            }
            else
            {
                // prefer class animation, fall back to character
                var classData = unit.GetCurrentClass()?.ClassData;
                var classWalkClip = classData.WalkAnimation;
                walkClip =
                    (classWalkClip != null && classWalkClip)
                        ? classWalkClip
                        : unit.CharacterTemplate.DefaultWalkingAnimation;
            }

            if (walkClip != null)
            {
                overrideController["Walk"] = walkClip;
            }

            animator.runtimeAnimatorController = overrideController;
            animator.enabled = true;

            // delegate idle logic to its own helper so it can be reused
            SetupIdleAnimation(animator, unit, overrideController);
        }

        private IEnumerator PlayIdleAnimationNextFrame(Animator animator)
        {
            // keep legacy behaviour: immediately push the idle state
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

        /// <summary>
        /// Configure idle animation clips separately from walk.
        /// <paramref name="overrideController"/> may be provided when caller already
        /// created one for walk setup; otherwise a new override controller will be
        /// instantiated from the animator's current controller.
        /// </summary>
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
                    LogError($"Animator on '{animator.gameObject.name}' has no controller.");
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
                overrideController["Idle"] = idleClip;
            }

            animator.runtimeAnimatorController = overrideController;
            animator.enabled = true;

            // start first-frame play and the variation loop
            StartCoroutine(PlayIdleAnimationNextFrame(animator));
            StartCoroutine(IdleVariationRoutine(animator, idleClips));
        }

        private IEnumerator IdleVariationRoutine(Animator animator, AnimationClip[] idleClips)
        {
            // nothing to do if there aren't at least two clips
            if (idleClips == null || idleClips.Length <= 1)
            {
                yield break;
            }

            // pick initial clip index from controller (already applied)
            int currentIndex = Random.Range(0, idleClips.Length);
            AnimationClip currentClip = idleClips[currentIndex];

            // loop indefinitely while animator is alive
            while (animator != null && animator.gameObject.activeInHierarchy)
            {
                // wait until slightly before the clip finishes so blending overlaps
                float clipLength =
                    (currentClip != null && currentClip.length > 0f) ? currentClip.length : 1f;
                float waitTime = Mathf.Max(0f, clipLength - ANIMATION_BLEND_DURATION);
                yield return new WaitForSeconds(waitTime);

                // choose a different clip (allow repeats if random picks same)
                int nextIndex = Random.Range(0, idleClips.Length);
                AnimationClip nextClip = idleClips[nextIndex];
                if (nextClip == null || nextClip == currentClip)
                {
                    continue;
                }

                // perform a manual blend between the two clips using PlayableGraph
                yield return BlendIdleClips(
                    animator,
                    currentClip,
                    nextClip,
                    ANIMATION_BLEND_DURATION
                );

                // update controller to reflect current clip
                if (animator.runtimeAnimatorController is AnimatorOverrideController oc)
                {
                    oc["Idle"] = nextClip;
                }
                animator.Play(Animator.StringToHash("Idle"), 0, 0f);

                currentIndex = nextIndex;
                currentClip = nextClip;
            }
        }

        /// <summary>
        /// Creates a temporary PlayableGraph that crossfades from <paramref name="from"/>
        /// to <paramref name="to"/> over <paramref name="duration"/> seconds, feeding
        /// the result into <paramref name="animator"/>. The graph is destroyed when
        /// the transition completes.
        /// </summary>
        private IEnumerator BlendIdleClips(
            Animator animator,
            AnimationClip from,
            AnimationClip to,
            float duration
        )
        {
            if (animator == null || from == null || to == null || duration <= 0f)
            {
                yield break;
            }

            var graph = PlayableGraph.Create("IdleBlend");
            var output = AnimationPlayableOutput.Create(graph, "IdleBlendOutput", animator);
            var mixer = AnimationMixerPlayable.Create(graph, 2);

            var fromPlayable = AnimationClipPlayable.Create(graph, from);
            var toPlayable = AnimationClipPlayable.Create(graph, to);

            graph.Connect(fromPlayable, 0, mixer, 0);
            graph.Connect(toPlayable, 0, mixer, 1);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);

            output.SetSourcePlayable(mixer);
            graph.Play();

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float w = Mathf.Clamp01(t / duration);
                mixer.SetInputWeight(0, 1f - w);
                mixer.SetInputWeight(1, w);
                yield return null;
            }

            graph.Destroy();
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
#if UNITY_EDITOR
            if (!unit.CharacterTemplate.HasExtraBoneLayer)
            {
                return;
            }

            if (unit.CharacterTemplate.AdditionalBonesMask == null)
            {
                LogWarning(
                    $"{unit.CharacterTemplate.DisplayName}: HasExtraBoneLayer is true but AdditionalBonesMask is not assigned."
                );
                return;
            }

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                LogError(
                    $"{unit.CharacterTemplate.DisplayName}: Cannot setup extra bone layer - no animator controller assigned."
                );
                return;
            }

            // Check if controller has at least 2 layers
            var controllerAsset = controller as UnityEditor.Animations.AnimatorController;
            if (controllerAsset == null)
            {
                LogWarning(
                    $"{unit.CharacterTemplate.DisplayName}: Extra bone layers require editor-time setup (assign AvatarMask to Layer 1)."
                );
                return;
            }

            // Editor-time setup
            if (controllerAsset.layers.Length < 2)
            {
                LogError(
                    $"{unit.CharacterTemplate.DisplayName}: AnimatorController needs at least 2 layers (Layer 1 missing)."
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
