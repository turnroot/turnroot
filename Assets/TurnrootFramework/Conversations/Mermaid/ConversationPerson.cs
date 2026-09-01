using System;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Conversations.Mermaid
{
    /// <summary>
    /// Maps a speaker name as it appears in a Mermaid file to a runtime <see cref="CharacterData"/> asset.
    /// </summary>
    [Serializable]
    public class ConversationPerson
    {
        [Tooltip("Speaker name as written in the Mermaid source (e.g. 'Aubrey', 'WomanA').")]
        public string SpeakerName;

        [Tooltip("Character asset used for portraits, display name, and pronoun substitution.")]
        public CharacterData Character;

        [Tooltip("Optional override for the name shown in the dialogue box.")]
        public string DisplayNameOverride;

        public string ResolvedDisplayName =>
            string.IsNullOrWhiteSpace(DisplayNameOverride)
                ? Character != null && !string.IsNullOrWhiteSpace(Character.DisplayName)
                    ? Character.DisplayName
                    : SpeakerName
                : DisplayNameOverride;
    }
}
