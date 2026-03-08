using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    /// <summary>
    /// Scene-based audio manager that bridges DynamicSceneFlow and AudioBrain.
    /// Manages audio source groups, conditional profile selection, and runtime audio state.
    /// Similar to ConversationController in architecture.
    /// </summary>
    public class AudioController : MonoBehaviour
    {
        #region Audio Group Configuration

        [Serializable]
        public class AudioSourceGroup
        {
            [Tooltip("The group this collection belongs to")]
            public AudioGroup group;

            [Tooltip("Audio sources for this group")]
            public AudioSource[] sources;
        }

        [Header("Audio Source Groups")]
        [Tooltip("Organize AudioSources by type (Music, SFX, Voices)")]
        [SerializeField]
        private List<AudioSourceGroup> musicSources = new();

        [SerializeField]
        private List<AudioSourceGroup> sfxSources = new();

        [SerializeField]
        private List<AudioSourceGroup> voiceSources = new();

        #endregion

        #region Segment Configuration

        [Serializable]
        public class ConditionalProfile
        {
            [Tooltip("Runtime condition key to check")]
            public string conditionKey;

            [Tooltip("Profile to use when condition is true")]
            public AudioSegmentProfile profile;
        }

        [Serializable]
        public class AudioSegmentConfig
        {
            [Tooltip("Human-readable name for this segment")]
            public string segmentName;

            [Tooltip("Default profile if no conditions match")]
            public AudioSegmentProfile defaultProfile;

            [Tooltip("Conditional profiles checked in order")]
            public List<ConditionalProfile> conditionalProfiles = new();
        }

        [Header("Segment Profiles")]
        [Tooltip("Audio configurations mapped to scene flow segments")]
        [SerializeField]
        private List<AudioSegmentConfig> audioSegments = new();

        #endregion

        #region Dependencies

        private Brain.Brain _brain;

        public void Initialize(Brain.Brain brain) => _brain = brain;

        #endregion

        #region Runtime State

        private Dictionary<string, bool> _runtimeConditions = new();
        private Dictionary<AudioGroup, List<AudioSource>> _groupedSources;

        #endregion

        #region Initialization

        private void Awake() => InitializeAudioGroups();

        private void InitializeAudioGroups()
        {
            _groupedSources = new Dictionary<AudioGroup, List<AudioSource>>();

            // Flatten all sources by group
            AddGroupSources(AudioGroup.Music, musicSources);
            AddGroupSources(AudioGroup.SFX, sfxSources);
            AddGroupSources(AudioGroup.Voices, voiceSources);
        }

        private void AddGroupSources(AudioGroup group, List<AudioSourceGroup> sourceGroups)
        {
            if (!_groupedSources.ContainsKey(group))
            {
                _groupedSources[group] = new List<AudioSource>();
            }

            foreach (var sourceGroup in sourceGroups)
            {
                if (sourceGroup.sources != null)
                {
                    _groupedSources[group].AddRange(sourceGroup.sources);
                }
            }
        }

        #endregion

        #region Help & Documentation

#if UNITY_EDITOR
        [Button("📖 Show Audio System Help", EButtonEnableMode.Always)]
        private void ShowHelp()
        {
            // Use reflection to call the editor window since it's in a separate Editor assembly
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            System.Type windowType = null;

            foreach (var assembly in assemblies)
            {
                windowType = assembly.GetType(
                    "Turnroot.Gameplay.Audio.Editor.AudioControllerHelpWindow"
                );
                if (windowType != null)
                {
                    break;
                }
            }

            if (windowType != null)
            {
                var showMethod = windowType.GetMethod(
                    "ShowWindowFromButton",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                showMethod?.Invoke(null, null);
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Help",
                    "Could not find AudioControllerHelpWindow editor script",
                    "OK"
                );
            }
        }
#endif

        #endregion

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

        #region Runtime Conditions


        public void SetCondition(string key, bool value) => _runtimeConditions[key] = value;

        public void ClearCondition(string key) => _runtimeConditions.Remove(key);

        public void ClearAllConditions() => _runtimeConditions.Clear();

        #endregion

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
