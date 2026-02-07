using System;
using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Character Recruitment Events

        public event Action<CharacterInstance, CharacterData, bool> OnCharacterRecruitableChanged;
        public event Action<
            CharacterInstance,
            CharacterData,
            float
        > OnCharacterRecruitmentChanceChanged;
        public event Action<
            CharacterInstance,
            CharacterData,
            float
        > OnCharacterRecruitmentChanceIncreaseChanged;
        public event Action<
            CharacterInstance,
            CharacterData,
            bool
        > OnCharacterRequiresMinSupportLevelChanged;
        public event Action<
            CharacterInstance,
            CharacterData
        > OnCharacterRecruitmentOverridesCleared;

        public void PublishCharacterRecruitableChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            bool isRecruitable
        ) => OnCharacterRecruitableChanged?.Invoke(sourceCharacter, targetCharacter, isRecruitable);

        public void PublishCharacterRecruitmentChanceChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            float chance
        ) => OnCharacterRecruitmentChanceChanged?.Invoke(sourceCharacter, targetCharacter, chance);

        public void PublishCharacterRecruitmentChanceIncreaseChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            float increase
        ) =>
            OnCharacterRecruitmentChanceIncreaseChanged?.Invoke(
                sourceCharacter,
                targetCharacter,
                increase
            );

        public void PublishCharacterRequiresMinSupportLevelChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            bool requiresMinSupportLevel
        ) =>
            OnCharacterRequiresMinSupportLevelChanged?.Invoke(
                sourceCharacter,
                targetCharacter,
                requiresMinSupportLevel
            );

        public void PublishCharacterRecruitmentOverridesCleared(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter
        ) => OnCharacterRecruitmentOverridesCleared?.Invoke(sourceCharacter, targetCharacter);

        #endregion
    }
}
