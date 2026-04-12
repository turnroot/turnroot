using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [Serializable]
    public struct OneShotDialogue
    {
        public string Dialogue;
        public Sprite Portrait;
        public AudioClip Audio;
    }

    /// <summary>
    /// Manages audio systems and sound playback within the brain framework.
    /// </summary>
    public class AudioBrain : BrainComponent
    {
        private Dictionary<AudioSource, Coroutine> _activeFades = new();

        private readonly AudioClip currentMusic;

        public void SetMusic(AudioClip newMusic, float fadeDuration = 1f)
        {
            if (currentMusic == newMusic)
            {
                return;
            }

            var musicPlayer = GetOrCreateMusicPlayer();
            if (musicPlayer == null)
            {
                $"AudioBrain: Failed to get or create music player.".LogError();
                return;
            }

            var audioSource = musicPlayer.Player;

            if (_activeFades.TryGetValue(audioSource, out var existingFade) && existingFade != null)
            {
                StopCoroutine(existingFade);
                _activeFades.Remove(audioSource);
            }

            _activeFades[audioSource] = StartCoroutine(
                FadeToNewMusicCoroutine(audioSource, newMusic, fadeDuration)
            );
        }

        public IEnumerator FadeToNewMusicCoroutine(
            AudioSource musicPlayer,
            AudioClip newMusic,
            float fadeDuration
        )
        {
            var initialVolume = musicPlayer.volume;
            var time = 0f;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                musicPlayer.volume = Mathf.Lerp(initialVolume, 0f, time / fadeDuration);
                yield return null;
            }

            musicPlayer.Stop();
            musicPlayer.clip = newMusic;
            musicPlayer.Play();

            time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                musicPlayer.volume = Mathf.Lerp(0f, initialVolume, time / fadeDuration);
                yield return null;
            }

            musicPlayer.volume = initialVolume;
            _activeFades.Remove(musicPlayer);
        }

        public MusicPlayer GetOrCreateMusicPlayer()
        {
            musicPlayer = musicPlayer != null ? musicPlayer : FindFirstObjectByType<MusicPlayer>();
            if (musicPlayer == null)
            {
                $"AudioBrain: No MusicPlayer found in scene.".LogError();
                return null;
            }

            return musicPlayer;
        }

        private MusicPlayer musicPlayer;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

        protected override void Awake() => base.Awake();

        public OneShot[] ConvertToOneShots(OneShotDialogue[] dialogues, string speakerName)
        {
            if (dialogues == null)
            {
                return System.Array.Empty<OneShot>();
            }

            var result = new OneShot[dialogues.Length];
            for (var i = 0; i < dialogues.Length; i++)
            {
                result[i] = new OneShot
                {
                    Dialogue = dialogues[i].Dialogue,
                    Portrait = dialogues[i].Portrait,
                    Audio = dialogues[i].Audio,
                    SpeakerName = speakerName,
                };
            }

            return result;
        }

        public OneShot GetRandomOneShot(OneShot[] candidates)
        {
            if (candidates == null || candidates.Length == 0)
            {
                return default;
            }

            return candidates[UnityEngine.Random.Range(0, candidates.Length)];
        }

        public OneShot GetRandomWelcomeOneShot(OneShot[] welcomeDialogues) =>
            GetRandomOneShot(welcomeDialogues);

        public OneShotPlayer GetOrCreateOneShotPlayer()
        {
            if (!TryGetComponent(out OneShotPlayer player))
            {
                player = gameObject.AddComponent<OneShotPlayer>();
            }

            if (TryGetComponent<AudioSource>(out var audioSource))
            {
                player.SetAudioSource(audioSource);
            }

            return player;
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
