using System;
using NaughtyAttributes;
using Turnroot.Characters;
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
}
