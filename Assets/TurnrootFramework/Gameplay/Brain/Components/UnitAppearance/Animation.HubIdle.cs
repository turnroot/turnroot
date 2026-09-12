using System.Collections;
using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        private readonly Dictionary<int, Coroutine> _hubIdleCoroutines = new();
        private readonly Dictionary<int, PlayableGraph> _hubIdleGraphs = new();

        /// <summary>
        /// Configures a hub unit model to play the current class's idle animations
        /// on loop, blending randomly through them
        ///
        /// Call this after <see cref="CreateModelForUnit"/> both when HubTeamLocations spawns hub
        /// unit models and when HubCharacter spawns its avatar model
        /// </summary>
        public void SetupHubIdleAnimation(GameObject model, CharacterInstance unit)
        {
            if (model == null || unit == null)
            {
                return;
            }

            if (!model.TryGetComponent<Animator>(out var animator))
            {
                return;
            }

            var idleClips = ResolveHubIdleClips(unit);
            if (idleClips == null || idleClips.Length == 0)
            {
                return;
            }

            // Stop any existing idle loop for this model before starting a new one
            int modelId = model.GetInstanceID();
            if (
                _hubIdleCoroutines.TryGetValue(modelId, out var existingRoutine)
                && existingRoutine != null
            )
            {
                StopCoroutine(existingRoutine);
            }

            if (
                _hubIdleGraphs.TryGetValue(modelId, out var existingGraph)
                && existingGraph.IsValid()
            )
            {
                existingGraph.Destroy();
            }

            _hubIdleCoroutines.Remove(modelId);
            _hubIdleGraphs.Remove(modelId);

            var baseController = animator.runtimeAnimatorController;
            if (baseController != null)
            {
                var overrideController = new AnimatorOverrideController(baseController);

                var firstClip = idleClips[Random.Range(0, idleClips.Length)];
                if (firstClip != null)
                {
                    overrideController[IdleState] = firstClip;
                }

                animator.runtimeAnimatorController = overrideController;
                animator.enabled = true;

                StartCoroutine(PlayIdleAnimationNextFrame(animator));
                _hubIdleCoroutines[modelId] = StartCoroutine(
                    IdleVariationRoutine(animator, idleClips)
                );
                return;
            }

            animator.enabled = true;
            _hubIdleCoroutines[modelId] = StartCoroutine(
                HubIdlePlayableRoutine(modelId, animator, idleClips)
            );
        }

        private IEnumerator HubIdlePlayableRoutine(
            int modelId,
            Animator animator,
            AnimationClip[] idleClips
        )
        {
            if (animator == null || idleClips == null || idleClips.Length == 0)
            {
                yield break;
            }

            var firstClip = idleClips[Random.Range(0, idleClips.Length)];
            if (firstClip == null)
            {
                yield break;
            }

            var graph = PlayableGraph.Create($"HubIdle_{modelId}");
            _hubIdleGraphs[modelId] = graph;

            var output = AnimationPlayableOutput.Create(graph, "HubIdleOutput", animator);
            var mixer = AnimationMixerPlayable.Create(graph, 2);
            output.SetSourcePlayable(mixer);

            var currentPlayable = AnimationClipPlayable.Create(graph, firstClip);
            currentPlayable.SetApplyFootIK(false);
            currentPlayable.SetApplyPlayableIK(false);
            currentPlayable.SetDuration(firstClip.length);

            graph.Connect(currentPlayable, 0, mixer, 0);
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            graph.Play();

            var currentClip = firstClip;
            int currentInput = 0;

            try
            {
                while (animator != null && animator.gameObject.activeInHierarchy)
                {
                    float clipLength =
                        (currentClip != null && currentClip.length > 0f) ? currentClip.length : 1f;
                    float waitTime = Mathf.Max(0f, clipLength - ANIMATION_BLEND_DURATION);
                    yield return new WaitForSeconds(waitTime);

                    if (animator == null || !animator.gameObject.activeInHierarchy)
                    {
                        yield break;
                    }

                    int nextIndex = idleClips.Length > 1 ? Random.Range(0, idleClips.Length) : 0;
                    var nextClip = idleClips[nextIndex];
                    if (nextClip == null)
                    {
                        continue;
                    }

                    if (nextClip == currentClip)
                    {
                        currentPlayable.SetTime(0d);
                        continue;
                    }

                    int nextInput = 1 - currentInput;
                    var nextPlayable = AnimationClipPlayable.Create(graph, nextClip);
                    nextPlayable.SetApplyFootIK(false);
                    nextPlayable.SetApplyPlayableIK(false);
                    nextPlayable.SetDuration(nextClip.length);

                    if (mixer.GetInput(nextInput).IsValid())
                    {
                        graph.Disconnect(mixer, nextInput);
                        graph.DestroyPlayable(mixer.GetInput(nextInput));
                    }

                    graph.Connect(nextPlayable, 0, mixer, nextInput);
                    mixer.SetInputWeight(nextInput, 0f);

                    float t = 0f;
                    while (t < ANIMATION_BLEND_DURATION)
                    {
                        t += Time.deltaTime;
                        float w = Mathf.Clamp01(t / ANIMATION_BLEND_DURATION);
                        mixer.SetInputWeight(currentInput, 1f - w);
                        mixer.SetInputWeight(nextInput, w);
                        yield return null;
                    }

                    if (mixer.GetInput(currentInput).IsValid())
                    {
                        graph.Disconnect(mixer, currentInput);
                        graph.DestroyPlayable(mixer.GetInput(currentInput));
                    }

                    currentInput = nextInput;
                    currentPlayable = nextPlayable;
                    currentClip = nextClip;
                }
            }
            finally
            {
                if (graph.IsValid())
                {
                    graph.Destroy();
                }

                _hubIdleGraphs.Remove(modelId);
                _hubIdleCoroutines.Remove(modelId);
            }
        }

        private AnimationClip[] ResolveHubIdleClips(CharacterInstance unit)
        {
            var classData = unit.GetCurrentClass()?.ClassData;
            return classData?.IdleAnimations;
        }
    }
}
