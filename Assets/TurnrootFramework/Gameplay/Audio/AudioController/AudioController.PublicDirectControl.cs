using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    public partial class AudioController : MonoBehaviour
    {
        #region Public API - Direct Control

        /// <summary>
        /// Plays a voice clip on the first available voice source
        /// </summary>
        public void PlayVoiceClip(AudioClip clip)
        {
            var action = new AudioAction
            {
                group = AudioGroup.Voices,
                actionType = AudioActionType.Play,
                clip = clip,
                sourceIndex = 0,
            };

            _brain?.audioBrain?.ExecuteAudioAction(action, GetSourceForAction(action));
        }

        /// <summary>
        /// Plays an SFX clip on the first available SFX source
        /// </summary>
        public void PlaySfxClip(AudioClip clip)
        {
            var action = new AudioAction
            {
                group = AudioGroup.SFX,
                actionType = AudioActionType.Play,
                clip = clip,
                sourceIndex = 0,
            };

            _brain?.audioBrain?.ExecuteAudioAction(action, GetSourceForAction(action));
        }

        /// <summary>
        /// Fades out all music sources
        /// </summary>
        public void FadeOutMusic() => FadeOutMusic(2f);

        /// <summary>
        /// Fades out all music sources over specified duration
        /// </summary>
        public void FadeOutMusic(float duration)
        {
            if (!_groupedSources.TryGetValue(AudioGroup.Music, out var sources))
            {
                return;
            }

            foreach (var source in sources)
            {
                var action = new AudioAction
                {
                    group = AudioGroup.Music,
                    actionType = AudioActionType.FadeOut,
                    fadeDuration = duration,
                };
                _brain?.audioBrain?.ExecuteAudioAction(action, source);
            }
        }

        /// <summary>
        /// Stops all SFX immediately
        /// </summary>
        public void StopAllSFX()
        {
            if (!_groupedSources.TryGetValue(AudioGroup.SFX, out var sources))
            {
                return;
            }

            foreach (var source in sources)
            {
                var action = new AudioAction
                {
                    group = AudioGroup.SFX,
                    actionType = AudioActionType.StopImmediate,
                };
                _brain?.audioBrain?.ExecuteAudioAction(action, source);
            }
        }

        /// <summary>
        /// Stops all voices immediately
        /// </summary>
        public void StopAllVoices()
        {
            if (!_groupedSources.TryGetValue(AudioGroup.Voices, out var sources))
            {
                return;
            }

            foreach (var source in sources)
            {
                var action = new AudioAction
                {
                    group = AudioGroup.Voices,
                    actionType = AudioActionType.StopImmediate,
                };
                _brain?.audioBrain?.ExecuteAudioAction(action, source);
            }
        }

        #endregion
    }
}
