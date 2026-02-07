using System;
using Turnroot.Characters;
using Turnroot.Skills;

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
        public event Action<CharacterInstance, string, int> OnExperienceGained;
        public event Action<CharacterInstance, CharacterData, int> OnSupportIncreased;

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

        public void PublishExperienceGained(
            CharacterInstance character,
            string experienceTypeId,
            int amount
        ) => OnExperienceGained?.Invoke(character, experienceTypeId, amount);

        public void PublishSupportIncreased(
            CharacterInstance character,
            CharacterData targetCharacter,
            int amount
        ) => OnSupportIncreased?.Invoke(character, targetCharacter, amount);

        #endregion
    }
}
