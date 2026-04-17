using System;
using Turnroot.Characters;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public enum HubCharacterOneShotType
    {
        StartInteraction,
        EndInteraction,
        GetGiftLove,
        GetGiftDislike,
        GetLostItemMine,
        GetLostItemNotMine,
        RecruitFail,
        RecruitSucceed,
    }

    /// <summary>
    /// Maps a character and interaction type to a set of hub one-shot dialogue lines for a specific chapter.
    /// </summary>
    [Serializable]
    public struct HubCharacterOneShotEntry
    {
        [Tooltip("The character this entry belongs to.")]
        public CharacterData Character;

        [Tooltip("The type of interaction this dialogue applies to.")]
        public HubCharacterOneShotType Type;

        [Tooltip(
            "One-shot dialogue lines for this character in this chapter. A random one is chosen at runtime."
        )]
        public OneShotDialogue[] OneShotDialogues;
    }

    /// <summary>
    /// Stores hub one-shot dialogue per character for a specific chapter number.
    /// Add instances directly to the <c>ChapterOneshots</c> list on <c>HubCharacterManager</c>.
    /// </summary>
    [Serializable]
    public struct HubCharacterOneShotChapter
    {
        [Tooltip("The chapter number this data applies to.")]
        public int ChapterNumber;

        [Tooltip("Per-character one-shot dialogue entries for this chapter.")]
        public HubCharacterOneShotEntry[] Entries;

        /// <summary>
        /// Returns the one-shot dialogues assigned to <paramref name="character"/> for this chapter,
        /// or an empty array if none are configured.
        /// </summary>
        public OneShotDialogue[] GetOneShotsForCharacter(
            CharacterData character,
            HubCharacterOneShotType type
        )
        {
            if (character == null || Entries == null)
            {
                return Array.Empty<OneShotDialogue>();
            }

            foreach (var entry in Entries)
            {
                if (entry.Character == character && entry.Type == type)
                {
                    return entry.OneShotDialogues ?? Array.Empty<OneShotDialogue>();
                }
            }

            return Array.Empty<OneShotDialogue>();
        }
    }

    /// <summary>
    /// Maps a character to a set of full chitchat <see cref="Conversation"/> assets for a specific chapter.
    /// A random unplayed conversation is chosen each time the player initiates Talk.
    /// </summary>
    [Serializable]
    public struct HubCharacterChitChatEntry
    {
        [Tooltip("The character this entry belongs to.")]
        public CharacterData Character;

        [Tooltip(
            "Conversations available for chitchat with this character in this chapter. "
                + "A random unplayed one is chosen at runtime. When all are exhausted Talk is disabled."
        )]
        public Conversation[] Conversations;
    }

    /// <summary>
    /// Stores per-chapter chitchat conversation data.
    /// Add instances to the <c>ChapterChitChatConversations</c> list on <see cref="HubCharacterManager"/>.
    /// </summary>
    [Serializable]
    public struct HubCharacterConversationChapter
    {
        [Tooltip("The chapter number this data applies to.")]
        public int ChapterNumber;

        [Tooltip("Per-character chitchat conversation entries for this chapter.")]
        public HubCharacterChitChatEntry[] Entries;

        /// <summary>
        /// Returns all chitchat conversations configured for <paramref name="character"/> in this chapter,
        /// or an empty array if none are configured.
        /// </summary>
        public Conversation[] GetConversationsForCharacter(CharacterData character)
        {
            if (character == null || Entries == null)
            {
                return Array.Empty<Conversation>();
            }

            foreach (var entry in Entries)
            {
                if (entry.Character == character)
                {
                    return entry.Conversations ?? Array.Empty<Conversation>();
                }
            }

            return Array.Empty<Conversation>();
        }
    }
}
