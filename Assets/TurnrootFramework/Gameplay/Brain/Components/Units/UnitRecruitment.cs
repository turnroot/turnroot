using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles character recruitment system, including recruitability overrides, recruitment chances, and support requirements.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        #region Recruitment System API

        public void SetCharacterRecruitableOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            bool isRecruitable
        )
        {
            if (!Validate(character, targetCharacter))
            {
                return;
            }

            var res = character.SetCharacterRecruitable(targetCharacter, isRecruitable);
            if (res.Success)
            {
                Brain.PublishCharacterRecruitableChanged(character, targetCharacter, isRecruitable);

                $"Set recruitable for {targetCharacter.DisplayName} to {isRecruitable}"
            .LogInfo();
            }
        }

        public void SetCharacterRecruitmentChanceOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            float chance
        )
        {
            if (!Validate(character, targetCharacter))
            {
                return;
            }

            var res = character.SetCharacterRecruitmentChance(targetCharacter, chance);
            if (res.Success)
            {
                Brain.PublishCharacterRecruitmentChanceChanged(character, targetCharacter, chance);

                $"Set recruitment chance for {targetCharacter.DisplayName} to {chance}"
            .LogInfo();
            }
        }

        public void SetCharacterRecruitmentChanceIncreaseOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            float increase
        )
        {
            if (!Validate(character, targetCharacter))
            {
                return;
            }

            var res = character.SetCharacterRecruitmentChanceIncreasePerConversation(
                targetCharacter,
                increase
            );
            if (res.Success)
            {
                Brain.PublishCharacterRecruitmentChanceIncreaseChanged(
                    character,
                    targetCharacter,
                    increase
                );

                $"Set recruitment increase for {targetCharacter.DisplayName} to {increase}"
            .LogInfo();
            }
        }

        public void SetCharacterRequiresMinSupportLevelOverride(
            CharacterInstance character,
            CharacterData targetCharacter,
            bool requiresMinSupportLevel
        )
        {
            if (!Validate(character, targetCharacter))
            {
                return;
            }

            character.SetCharacterRequiresMinSupportLevel(targetCharacter, requiresMinSupportLevel);
            Brain.PublishCharacterRequiresMinSupportLevelChanged(
                character,
                targetCharacter,
                requiresMinSupportLevel
            );

            $"Set requires-min-support for {targetCharacter.DisplayName} to {requiresMinSupportLevel}"
        .LogInfo();
        }

        public void ClearCharacterRecruitmentOverrides(
            CharacterInstance character,
            CharacterData targetCharacter
        )
        {
            if (!Validate(character, targetCharacter))
            {
                return;
            }

            character.ClearRecruitmentOverrides(targetCharacter);
            Brain.PublishCharacterRecruitmentOverridesCleared(character, targetCharacter);
            $"Cleared recruitment overrides for {targetCharacter.DisplayName}".LogInfo();
        }

        public bool IsCharacterRecruitable(
            CharacterInstance character,
            CharacterData targetCharacter
        ) =>
            Validate(character, targetCharacter)
            && character.IsCharacterRecruitable(targetCharacter);

        public float GetCharacterRecruitmentChance(
            CharacterInstance character,
            CharacterData targetCharacter
        ) =>
            Validate(character, targetCharacter)
                ? character.GetCharacterRecruitmentChance(targetCharacter)
                : 0f;

        public float GetCharacterRecruitmentChanceIncreasePerConversation(
            CharacterInstance character,
            CharacterData targetCharacter
        ) =>
            Validate(character, targetCharacter)
                ? character.GetCharacterRecruitmentChanceIncreasePerConversation(targetCharacter)
                : 0f;

        public bool GetCharacterRequiresMinSupportLevel(
            CharacterInstance character,
            CharacterData targetCharacter
        ) =>
            Validate(character, targetCharacter)
            && character.GetCharacterRequiresMinSupportLevel(targetCharacter);

        #endregion
    }
}

