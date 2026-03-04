using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    /// <summary>
    /// Defines how audio sources should behave (Play, FadeIn, FadeOut, Stop, etc.)
    /// </summary>
    public enum AudioActionType
    {
        Play,
        PlayAdditive,
        FadeIn,
        FadeOut,
        Stop,
        StopImmediate,
    }

    /// <summary>
    /// Categorizes audio sources into logical groups for easy management
    /// </summary>
    public enum AudioGroup
    {
        Music,
        SFX,
        Voices,
    }

    /// <summary>
    /// Defines a single audio action to be executed
    /// </summary>
    [Serializable]
    public class AudioAction
    {
        [Tooltip("Which audio group this action targets")]
        public AudioGroup group = AudioGroup.Music;

        [Tooltip("The type of action to perform")]
        public AudioActionType actionType = AudioActionType.Play;

        [Tooltip("The audio clip to play (if applicable)")]
        public AudioClip clip;

        [Tooltip("Which source index within the group (0 = first source)")]
        public int sourceIndex = 0;

        [Tooltip("Duration for fade operations")]
        public float fadeDuration = 1f;

        [Tooltip("Should the clip loop?")]
        public bool loop = false;

        [Tooltip("Play as 3D audio (true) or 2D audio (false)")]
        public bool is3D = false;

        [Tooltip("Delay before executing this action (seconds)")]
        public float delay = 0f;
    }

    /// <summary>
    /// ScriptableObject that defines a sequence of audio actions.
    /// Can be reused across multiple segments and conditionally selected at runtime.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewAudioProfile",
        menuName = "Turnroot/Audio/Audio Segment Profile"
    )]
    public class AudioSegmentProfile : ScriptableObject
    {
        [Tooltip("Human-readable description of what this profile does")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("List of audio actions to execute in sequence")]
        public List<AudioAction> actions = new();
    }
}
