using System.Collections;
using System.Collections.Generic;
using Turnroot.Gameplay.Audio;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages audio systems and sound playback within the brain framework.
    /// Executes AudioActions from AudioController, handling fades, transitions, and playback modes.
    /// </summary>
    public class AudioBrain : BrainComponent
    {
        private Dictionary<AudioSource, Coroutine> _activeFades = new();

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

        protected override void Awake() => base.Awake();

        /// <summary>
        /// Executes an audio action on the specified source
        /// </summary>
        public void ExecuteAudioAction(AudioAction action, AudioSource source)
        {
            if (source == null)
            {
                "Cannot execute action on null AudioSource".LogWarning("AudioBrain");
                return;
            }

            if (action.delay > 0)
            {
                StartCoroutine(ExecuteDelayedAction(action, source));
                return;
            }

            ExecuteActionImmediate(action, source);
        }

        private IEnumerator ExecuteDelayedAction(AudioAction action, AudioSource source)
        {
            yield return new WaitForSeconds(action.delay);
            ExecuteActionImmediate(action, source);
        }

        private void ExecuteActionImmediate(AudioAction action, AudioSource source)
        {
            switch (action.actionType)
            {
                case AudioActionType.Play:
                    PlayClip(source, action);
                    break;

                case AudioActionType.PlayAdditive:
                    PlayClipAdditive(source, action);
                    break;

                case AudioActionType.FadeIn:
                    FadeIn(source, action);
                    break;

                case AudioActionType.FadeOut:
                    FadeOut(source, action);
                    break;

                case AudioActionType.Stop:
                    StopClip(source, action);
                    break;

                case AudioActionType.StopImmediate:
                    StopClipImmediate(source);
                    break;
            }
        }

        private void PlayClip(AudioSource source, AudioAction action)
        {
            if (action.clip == null)
            {
                "Cannot play null clip".LogWarning("AudioBrain");
                return;
            }

            // Stop any active fade
            StopFade(source);

            source.clip = action.clip;
            source.loop = action.loop;
            source.spatialBlend = action.is3D ? 1f : 0f;
            source.volume = 1f;
            source.Play();
        }

        private void PlayClipAdditive(AudioSource source, AudioAction action)
        {
            if (action.clip == null)
            {
                "Cannot play null clip".LogWarning("AudioBrain");
                return;
            }

            // Play one-shot doesn't interrupt current clip
            source.spatialBlend = action.is3D ? 1f : 0f;
            source.PlayOneShot(action.clip);
        }

        private void FadeIn(AudioSource source, AudioAction action)
        {
            if (action.clip == null)
            {
                "Cannot fade in null clip".LogWarning("AudioBrain");
                return;
            }

            StopFade(source);

            source.clip = action.clip;
            source.loop = action.loop;
            source.spatialBlend = action.is3D ? 1f : 0f;
            source.volume = 0f;
            source.Play();

            var fadeCoroutine = StartCoroutine(FadeVolume(source, 0f, 1f, action.fadeDuration));
            _activeFades[source] = fadeCoroutine;
        }

        private void FadeOut(AudioSource source, AudioAction action)
        {
            StopFade(source);

            float startVolume = source.volume;
            var fadeCoroutine = StartCoroutine(
                FadeVolume(source, startVolume, 0f, action.fadeDuration, stopAfter: true)
            );
            _activeFades[source] = fadeCoroutine;
        }

        private void StopClip(AudioSource source, AudioAction action)
        {
            FadeOut(source, action);
        }

        private void StopClipImmediate(AudioSource source)
        {
            StopFade(source);
            source.Stop();
            source.volume = 1f;
        }

        private void StopFade(AudioSource source)
        {
            if (_activeFades.TryGetValue(source, out var fadeCoroutine))
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                _activeFades.Remove(source);
            }
        }

        private IEnumerator FadeVolume(
            AudioSource source,
            float startVolume,
            float endVolume,
            float duration,
            bool stopAfter = false
        )
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVolume, endVolume, t);
                yield return null;
            }

            source.volume = endVolume;

            if (stopAfter)
            {
                source.Stop();
            }

            _activeFades.Remove(source);
        }

        private void Start()
        {
            // Find and initialize AudioController in scene
            var audioController = FindAnyObjectByType<AudioController>();
            if (audioController != null)
            {
                audioController.Initialize(_brain);
            }
        }

        protected override void OnDestroy()
        {
            // Clean up any active fades
            foreach (var fade in _activeFades.Values)
            {
                if (fade != null)
                {
                    StopCoroutine(fade);
                }
            }
            _activeFades.Clear();

            base.OnDestroy();
        }
    }
}
