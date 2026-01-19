using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
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
                _brain?.PublishCharacterRecruitableChanged(
                    character,
                    targetCharacter,
                    isRecruitable
                );
                TurnrootLogger.Log(
                    $"Set recruitable for {targetCharacter.DisplayName} to {isRecruitable}"
                );
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
                _brain?.PublishCharacterRecruitmentChanceChanged(
                    character,
                    targetCharacter,
                    chance
                );
                TurnrootLogger.Log(
                    $"Set recruitment chance for {targetCharacter.DisplayName} to {chance}"
                );
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
                _brain?.PublishCharacterRecruitmentChanceIncreaseChanged(
                    character,
                    targetCharacter,
                    increase
                );
                TurnrootLogger.Log(
                    $"Set recruitment increase for {targetCharacter.DisplayName} to {increase}"
                );
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
            _brain?.PublishCharacterRequiresMinSupportLevelChanged(
                character,
                targetCharacter,
                requiresMinSupportLevel
            );
            TurnrootLogger.Log(
                $"Set requires-min-support for {targetCharacter.DisplayName} to {requiresMinSupportLevel}"
            );
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
            _brain?.PublishCharacterRecruitmentOverridesCleared(character, targetCharacter);
            TurnrootLogger.Log($"Cleared recruitment overrides for {targetCharacter.DisplayName}");
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
