using System;
using Turnroot.Characters;
using Turnroot.Skills;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        public event Action<CharacterInstance> OnCharacterBadLevelUp;

        public void PublishBadLevelUp(CharacterInstance character) =>
            OnCharacterBadLevelUp?.Invoke(character);

        public event Action<CharacterInstance> OnCharacterGoodLevelUp;

        public void PublishGoodLevelUp(CharacterInstance character) =>
            OnCharacterGoodLevelUp?.Invoke(character);

        #region Character Progression Events

        public event Action<CharacterInstance> OnCharacterLevelUp;
        public event Action<CharacterInstance> OnCharacterKill;
        public event Action<CharacterInstance, Skill> OnCharacterLearnedSkill;
        public event Action<CharacterInstance, Skill> OnCharacterRemovedSkill;
        public event Action<CharacterInstance> OnCharacterClassChanged;

        // published when a character instance has a birthday during the current week
        public event Action<CharacterInstance, GameDate> OnCharacterBirthdayThisWeek;

        // Published when a character's bounded/unbounded stat current value changes.
        public event Action<
            CharacterInstance,
            Characters.Stats.BoundedStatType,
            float,
            float
        > OnCharacterBoundedStatChanged;
        public event Action<
            CharacterInstance,
            Characters.Stats.UnboundedStatType,
            float,
            float
        > OnCharacterUnboundedStatChanged;

        // Published specifically when class-level persistent bonuses are applied/removed.
        public event Action<
            CharacterInstance,
            Characters.CharacterClass.CharacterClassData
        > OnCharacterClassBonusesApplied;
        public event Action<
            CharacterInstance,
            Characters.CharacterClass.CharacterClassData
        > OnCharacterClassBonusesRemoved;

        // Mastery events: progress updates and unlocked notifications
        // Args for progress: (owner, classData, targetIndex, progress, threshold)
        public event Action<
            CharacterInstance,
            Characters.CharacterClass.CharacterClassData,
            int,
            int,
            int
        > OnCharacterClassMasteryProgressChanged;

        // Args for unlock: (owner, classData, targetIndex, unlockedSkill)
        public event Action<
            CharacterInstance,
            Characters.CharacterClass.CharacterClassData,
            int,
            Skill
        > OnCharacterClassMasteryTargetUnlocked;

        public event Action<CharacterInstance, string, int> OnExperienceGained;
        public event Action<CharacterInstance, CharacterData, float> OnSupportIncreased;

        public void PublishCharacterLevelUp(CharacterInstance character) =>
            OnCharacterLevelUp?.Invoke(character);

        public void PublishCharacterKill(CharacterInstance character) =>
            OnCharacterKill?.Invoke(character);

        public void PublishCharacterLearnedSkill(CharacterInstance character, Skill skill) =>
            OnCharacterLearnedSkill?.Invoke(character, skill);

        public void PublishCharacterRemovedSkill(CharacterInstance character, Skill skill) =>
            OnCharacterRemovedSkill?.Invoke(character, skill);

        public void PublishCharacterClassChanged(CharacterInstance character) =>
            OnCharacterClassChanged?.Invoke(character);

        public void PublishCharacterBirthdayThisWeek(CharacterInstance character, GameDate date) =>
            OnCharacterBirthdayThisWeek?.Invoke(character, date);

        public void PublishCharacterBoundedStatChanged(
            CharacterInstance character,
            Characters.Stats.BoundedStatType statType,
            float oldValue,
            float newValue
        ) => OnCharacterBoundedStatChanged?.Invoke(character, statType, oldValue, newValue);

        public void PublishCharacterUnboundedStatChanged(
            CharacterInstance character,
            Characters.Stats.UnboundedStatType statType,
            float oldValue,
            float newValue
        ) => OnCharacterUnboundedStatChanged?.Invoke(character, statType, oldValue, newValue);

        public void PublishCharacterClassBonusesApplied(
            CharacterInstance character,
            Characters.CharacterClass.CharacterClassData classData
        ) => OnCharacterClassBonusesApplied?.Invoke(character, classData);

        public void PublishCharacterClassBonusesRemoved(
            CharacterInstance character,
            Characters.CharacterClass.CharacterClassData classData
        ) => OnCharacterClassBonusesRemoved?.Invoke(character, classData);

        // Mastery publishing helpers
        public void PublishCharacterClassMasteryProgressChanged(
            CharacterInstance owner,
            Characters.CharacterClass.CharacterClassData classData,
            int targetIndex,
            int progress,
            int threshold
        ) =>
            OnCharacterClassMasteryProgressChanged?.Invoke(
                owner,
                classData,
                targetIndex,
                progress,
                threshold
            );

        public void PublishCharacterClassMasteryTargetUnlocked(
            CharacterInstance owner,
            Characters.CharacterClass.CharacterClassData classData,
            int targetIndex,
            Skill skill
        ) => OnCharacterClassMasteryTargetUnlocked?.Invoke(owner, classData, targetIndex, skill);

        public void PublishExperienceGained(
            CharacterInstance character,
            string experienceTypeId,
            int amount
        ) => OnExperienceGained?.Invoke(character, experienceTypeId, amount);

        public void PublishSupportIncreased(
            CharacterInstance character,
            CharacterData targetCharacter,
            float amount
        ) => OnSupportIncreased?.Invoke(character, targetCharacter, amount);

        #endregion
    }
}
