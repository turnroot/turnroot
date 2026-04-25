using System;
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

    [Serializable]
    public struct HubExploreUnlockCondition
    {
        [Tooltip("What kind of condition must be met to unlock this explore location.")]
        public ExploreUnlockConditionType Type;

        [Tooltip("(DateBased) The location becomes available on or after this date.")]
        public GameDate UnlockDate;

        [Tooltip("(ChapterBased) The location becomes available on or after this chapter number.")]
        public int UnlockAfterChapter;

        [Tooltip(
            "(CharacterSupport) Any character in the player roster must have at least the "
                + "required support level with this character for the condition to pass."
        )]
        public CharacterData RequiredCharacter;

        [Tooltip(
            "(CharacterSupport) Minimum support level required (E, D, C, B, A, or S). "
                + "At least one party member must be at or above this level with RequiredCharacter."
        )]
        public string RequiredSupportLevel;

        /// <summary>Returns true if this single condition is satisfied. All conditions on an
        /// ExploreLocation use AND logic — the location is locked if any condition fails.</summary>
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

            var unlock = new System.DateTime(UnlockDate.year, UnlockDate.month, UnlockDate.day);
            var current = new System.DateTime(currentDate.year, currentDate.month, currentDate.day);
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
            if (brain == null || RequiredCharacter == null)
            {
                return false;
            }

            if (
                string.IsNullOrEmpty(RequiredSupportLevel)
                || !SupportLevels.IsValid(RequiredSupportLevel)
            )
            {
                $"HubExploreUnlockCondition: RequiredSupportLevel '{RequiredSupportLevel}' is invalid. Must be E, D, C, B, A, or S.".LogWarning();
                return false;
            }

            var avatar = brain.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null)
            {
                "HubExploreUnlockCondition: No avatar instance available.".LogWarning();
                return false;
            }

            var rel = avatar.GetSupportRelationship(RequiredCharacter);
            if (rel == null)
            {
                return false;
            }

            var currentLevel = new SupportLevels { Value = rel.CurrentLevel };
            return currentLevel.CompareTo(RequiredSupportLevel) >= 0;
        }
    }
}
