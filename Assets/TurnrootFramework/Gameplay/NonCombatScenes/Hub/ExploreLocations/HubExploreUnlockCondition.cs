using System;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public enum ExploreUnlockConditionType
    {
        DateBased,
        ChapterBased,
        CharacterSupport,
    }

    public enum SupportCheckMode
    {
        All,

        Any,
    }

    [Serializable]
    public struct CharacterSupportEntry
    {
        [Tooltip("The character the avatar must have a support relationship with.")]
        public CharacterData Character;

        [Tooltip("The minimum support level the avatar must have reached with this character.")]
        public SupportLevels MinimumLevel;
    }

    [Serializable]
    public struct HubExploreUnlockCondition
    {
        [Tooltip("What kind of condition must be met to unlock this explore location.")]
        public ExploreUnlockConditionType Type;

        [ShowIf("IsDateBased")]
        [Tooltip("The location becomes available on or after this in-game date.")]
        public GameDate UnlockDate;

        [ShowIf("IsChapterBased")]
        [Tooltip("The location becomes available once this chapter number has been reached.")]
        public int UnlockAfterChapter;

        [ShowIf("IsCharacterSupport")]
        [InfoBox(
            "All: every listed support must reach its required level.\n"
                + "Any: at least one listed support must reach its required level."
        )]
        [Tooltip(
            "Whether all listed supports must be met (AND) or any one of them is enough (OR)."
        )]
        public SupportCheckMode SupportMode;

        [ShowIf("IsCharacterSupport")]
        [Tooltip("The avatar's support relationships to check. Add one entry per character.")]
        public CharacterSupportEntry[] SupportEntries;

        private bool IsDateBased() => Type == ExploreUnlockConditionType.DateBased;

        private bool IsChapterBased() => Type == ExploreUnlockConditionType.ChapterBased;

        private bool IsCharacterSupport() => Type == ExploreUnlockConditionType.CharacterSupport;

        public bool IsUnlocked(Brain.Brain brain, GameDate currentDate)
        {
            return Type switch
            {
                ExploreUnlockConditionType.DateBased => IsDateUnlocked(currentDate),
                ExploreUnlockConditionType.ChapterBased => IsChapterUnlocked(brain),
                ExploreUnlockConditionType.CharacterSupport => IsCharacterSupportUnlocked(brain),
                _ => true,
            };
        }

        private bool IsDateUnlocked(GameDate currentDate)
        {
            if (UnlockDate == GameDate.Default)
            {
                return true;
            }

            var unlock = new DateTime(UnlockDate.year, UnlockDate.month, UnlockDate.day);
            var current = new DateTime(currentDate.year, currentDate.month, currentDate.day);
            return current >= unlock;
        }

        private bool IsChapterUnlocked(Brain.Brain brain)
        {
            if (brain?.saveFileBrain?.ActiveSaveFile == null)
            {
                return false;
            }

            return brain.saveFileBrain.ActiveSaveFile.ChapterNumber >= UnlockAfterChapter;
        }

        private bool IsCharacterSupportUnlocked(Brain.Brain brain)
        {
            if (brain == null || SupportEntries == null || SupportEntries.Length == 0)
            {
                return false;
            }

            var avatar = brain.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null)
            {
                "HubExploreUnlockCondition: No avatar instance available.".LogWarning();
                return false;
            }

            if (SupportMode == SupportCheckMode.All)
            {
                foreach (var entry in SupportEntries)
                {
                    if (!CheckSupportEntry(avatar, entry))
                    {
                        return false;
                    }
                }
                return true;
            }
            else // Any
            {
                foreach (var entry in SupportEntries)
                {
                    if (CheckSupportEntry(avatar, entry))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static bool CheckSupportEntry(CharacterInstance avatar, CharacterSupportEntry entry)
        {
            if (entry.Character == null || entry.MinimumLevel == null)
            {
                return false;
            }

            var rel = avatar.GetSupportRelationship(entry.Character);
            if (rel == null)
            {
                return false;
            }

            return new SupportLevels { Value = rel.CurrentLevel }.CompareTo(
                    entry.MinimumLevel.Value
                ) >= 0;
        }
    }
}
