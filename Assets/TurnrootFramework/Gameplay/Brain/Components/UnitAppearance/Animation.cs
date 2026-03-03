using System.Collections;
using Turnroot.Characters;
using Turnroot.Utilities;
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

        // animation state names used when overriding clips
        private const string WalkState = "Walk";
        private const string IdleState = "Idle";

        // cached hash for the idle state (used when playing directly)
        private static readonly int IdleHash = Animator.StringToHash(IdleState);

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

        private IEnumerator IdleVariationRoutine(Animator animator, AnimationClip[] idleClips)
        {
            if (idleClips == null || idleClips.Length <= 1)
            {
                yield break;
            }

            int currentIndex = Random.Range(0, idleClips.Length);
            AnimationClip currentClip = idleClips[currentIndex];

            while (animator != null && animator.gameObject.activeInHierarchy)
            {
                float clipLength =
                    (currentClip != null && currentClip.length > 0f) ? currentClip.length : 1f;
                float waitTime = Mathf.Max(0f, clipLength - (ANIMATION_BLEND_DURATION * 2f));
                yield return new WaitForSeconds(waitTime);

                // choose a different clip (allow repeats if random picks same)
                int nextIndex = Random.Range(0, idleClips.Length);
                AnimationClip nextClip = idleClips[nextIndex];
                if (nextClip == null || nextClip == currentClip)
                {
                    continue;
                }

                float normalizedTime = 0f;
                if (animator != null && animator.gameObject.activeInHierarchy)
                {
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    normalizedTime = state.normalizedTime % 1f;
                }

                yield return BlendClips(
                    animator,
                    currentClip,
                    nextClip,
                    ANIMATION_BLEND_DURATION,
                    normalizedTime
                );

                if (animator.runtimeAnimatorController is AnimatorOverrideController oc)
                {
                    oc[IdleState] = nextClip;
                }
                animator.Play(IdleHash, 0, normalizedTime);

                currentIndex = nextIndex;
                currentClip = nextClip;
            }
        }

        private IEnumerator BlendClips(
            Animator animator,
            AnimationClip from,
            AnimationClip to,
            float duration,
            float startNormalizedTime = 0f
        )
        {
            if (animator == null || from == null || to == null || duration <= 0f)
            {
                yield break;
            }

            var graph = PlayableGraph.Create("IdleBlend");
            try
            {
                var output = AnimationPlayableOutput.Create(graph, "IdleBlendOutput", animator);
                var mixer = AnimationMixerPlayable.Create(graph, 2);

                var fromPlayable = AnimationClipPlayable.Create(graph, from);
                var toPlayable = AnimationClipPlayable.Create(graph, to);

                double fromTime = Mathf.Repeat(startNormalizedTime, 1f) * from.length;
                double toTime = Mathf.Repeat(startNormalizedTime, 1f) * to.length;
                fromPlayable.SetTime(fromTime);
                toPlayable.SetTime(toTime);

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
            }
            finally
            {
                if (graph.IsValid())
                {
                    graph.Destroy();
                }
            }
        }

        public void BlendToWalkAnimation(Animator animator) => BlendToNamedClip(animator, "Walk");

        public void BlendToIdleAnimation(Animator animator) => BlendToNamedClip(animator, "Idle");

        private void BlendToNamedClip(Animator animator, string clipName)
        {
            if (animator == null || !animator.gameObject.activeInHierarchy)
            {
                return;
            }

            var toClip = GetClipByName(animator, clipName);
            var stateHash = Animator.StringToHash(clipName);
            if (toClip == null)
            {
                // fallback to simple crossfade if we can't resolve a clip
                if (animator.HasState(0, stateHash))
                {
                    animator.CrossFade(stateHash, ANIMATION_BLEND_DURATION, 0);
                }
                return;
            }

            AnimationClip fromClip = toClip;
            var currentInfos = animator.GetCurrentAnimatorClipInfo(0);
            if (currentInfos.Length > 0 && currentInfos[0].clip != null)
            {
                fromClip = currentInfos[0].clip;
            }

            float normalizedTime = 0f;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            normalizedTime = state.normalizedTime % 1f;

            // begin blending via PlayableGraph; also start a crossfade so the controller
            // will continue to the requested state once the graph is torn down.
            StartCoroutine(
                BlendClips(animator, fromClip, toClip, ANIMATION_BLEND_DURATION, normalizedTime)
            );

            if (animator.HasState(0, stateHash))
            {
                animator.CrossFade(stateHash, ANIMATION_BLEND_DURATION, 0);
            }
        }

        /// <summary>
        /// Retrieves an animation clip by name from the given animator's controller
        /// (checking override controller first).
        /// </summary>
        private static AnimationClip GetClipByName(Animator animator, string clipName)
        {
            if (animator == null || string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                return null;
            }

            if (controller is AnimatorOverrideController oc)
            {
                var clip = oc[clipName];
                if (clip != null)
                {
                    return clip;
                }
            }

            var clips = controller.animationClips;
            if (clips != null)
            {
                foreach (var c in clips)
                {
                    if (c != null && c.name == clipName)
                    {
                        return c;
                    }
                }
            }

            return null;
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
