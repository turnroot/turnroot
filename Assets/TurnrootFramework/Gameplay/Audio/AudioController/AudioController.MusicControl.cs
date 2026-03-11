using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    public partial class AudioController : MonoBehaviour
    {
        #region Music Control

        /// <summary>
        /// Crossfades from one music clip to another.
        /// Fades out the currently playing music while simultaneously fading in the new clip.
        /// </summary>
        public void CrossfadeMusic(AudioClip newClip, float duration = 2f)
        {
            if (newClip == null)
            {
                "Cannot crossfade to null clip".LogWarning("AudioController");
                return;
            }

            if (
                !_groupedSources.TryGetValue(AudioGroup.Music, out var sources)
                || sources.Count == 0
            )
            {
                "No music sources available for crossfade".LogWarning("AudioController");
                return;
            }

            // Find an available source for the new clip
            // Use second source if available, otherwise reuse first
            AudioSource fadeInSource = sources.Count > 1 ? sources[1] : sources[0];
            AudioSource fadeOutSource = sources[0];

            // If using same source, just fade out and fade in
            if (fadeInSource == fadeOutSource)
            {
                // Fade out current
                var fadeOutAction = new AudioAction
                {
                    group = AudioGroup.Music,
                    actionType = AudioActionType.FadeOut,
                    fadeDuration = duration / 2f,
                };
                _brain?.audioBrain?.ExecuteAudioAction(fadeOutAction, fadeOutSource);

                // Fade in new after half duration
                var fadeInAction = new AudioAction
                {
                    group = AudioGroup.Music,
                    actionType = AudioActionType.FadeIn,
                    clip = newClip,
                    loop = true,
                    fadeDuration = duration / 2f,
                    delay = duration / 2f,
                };
                _brain?.audioBrain?.ExecuteAudioAction(fadeInAction, fadeInSource);
            }
            else
            {
                // Simultaneous crossfade with two sources
                var fadeOutAction = new AudioAction
                {
                    group = AudioGroup.Music,
                    actionType = AudioActionType.FadeOut,
                    fadeDuration = duration,
                };
                _brain?.audioBrain?.ExecuteAudioAction(fadeOutAction, fadeOutSource);

                var fadeInAction = new AudioAction
                {
                    group = AudioGroup.Music,
                    actionType = AudioActionType.FadeIn,
                    clip = newClip,
                    loop = true,
                    fadeDuration = duration,
                };
                _brain?.audioBrain?.ExecuteAudioAction(fadeInAction, fadeInSource);
            }
        }

        /// <summary>
        /// Stops all music immediately without fade
        /// </summary>
        public void StopAllMusic()
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
                    actionType = AudioActionType.StopImmediate,
                };
                _brain?.audioBrain?.ExecuteAudioAction(action, source);
            }
        }

        #endregion
    }
}
