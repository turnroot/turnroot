using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        private IEnumerator IdleVariationRoutine(Animator animator, AnimationClip[] idleClips)
        {
            if (idleClips == null || idleClips.Length == 0)
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

                if (animator == null || !animator.gameObject.activeInHierarchy)
                {
                    yield break;
                }

                // With a single clip, re-use it to keep looping; otherwise pick randomly.
                int nextIndex = idleClips.Length > 1 ? Random.Range(0, idleClips.Length) : 0;
                AnimationClip nextClip = idleClips[nextIndex];
                if (nextClip == null)
                {
                    continue;
                }

                var state = animator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = state.normalizedTime % 1f;

                if (nextClip != currentClip)
                {
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
                else
                {
                    // Same clip (or only one available): replay from the start to keep it looping.
                    animator.Play(IdleHash, 0, 0f);
                }
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
    }
}
