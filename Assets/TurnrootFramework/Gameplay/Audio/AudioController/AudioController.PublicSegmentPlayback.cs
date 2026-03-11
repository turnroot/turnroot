using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    public partial class AudioController : MonoBehaviour
    {
        #region Public API - Segment Playback

        /// <summary>
        /// Plays audio for a segment by index, checking conditions for profile selection
        /// </summary>
        public void PlaySegmentAudio(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= audioSegments.Count)
            {
                $"Invalid segment index {segmentIndex}".LogWarning("AudioController");
                return;
            }

            var segment = audioSegments[segmentIndex];
            var profile = SelectProfile(segment);

            if (profile != null)
            {
                ExecuteProfile(profile);
            }
        }

        /// <summary>
        /// Plays audio for a segment by name, checking conditions for profile selection
        /// </summary>
        public void PlaySegmentAudioByName(string segmentName)
        {
            var segment = audioSegments.Find(s => s.segmentName == segmentName);
            if (segment == null)
            {
                $"No segment found with name '{segmentName}'".LogWarning("AudioController");
                return;
            }

            var profile = SelectProfile(segment);
            if (profile != null)
            {
                ExecuteProfile(profile);
            }
        }

        private AudioSegmentProfile SelectProfile(AudioSegmentConfig segment)
        {
            foreach (var conditional in segment.conditionalProfiles)
            {
                if (
                    _runtimeConditions.TryGetValue(conditional.conditionKey, out bool isActive)
                    && isActive
                    && conditional.profile != null
                )
                {
                    return conditional.profile;
                }
            }

            return segment.defaultProfile;
        }

        private void ExecuteProfile(AudioSegmentProfile profile)
        {
            if (_brain?.audioBrain == null)
            {
                "AudioBrain reference not set!".LogError("AudioController");
                return;
            }

            foreach (var action in profile.actions)
            {
                _brain.audioBrain.ExecuteAudioAction(action, GetSourceForAction(action));
            }
        }

        private AudioSource GetSourceForAction(AudioAction action)
        {
            if (!_groupedSources.TryGetValue(action.group, out var sources))
            {
                $"No sources found for group {action.group}".LogWarning("AudioController");
                return null;
            }

            if (action.sourceIndex < 0 || action.sourceIndex >= sources.Count)
            {
                $"Invalid source index {action.sourceIndex} for group {action.group}".LogWarning(
                    "AudioController"
                );
                return null;
            }

            return sources[action.sourceIndex];
        }

        #endregion
    }
}
