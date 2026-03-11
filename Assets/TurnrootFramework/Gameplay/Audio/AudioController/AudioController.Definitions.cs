using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    public partial class AudioController : MonoBehaviour
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

        #region Dependencies & State

        private Brain.Brain _brain;

        private Dictionary<string, bool> _runtimeConditions = new();
        private Dictionary<AudioGroup, List<AudioSource>> _groupedSources;

        #endregion
    }
}