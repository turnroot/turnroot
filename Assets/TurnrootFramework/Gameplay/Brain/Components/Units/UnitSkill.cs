using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        #region Skill Management API

        public void LearnSkill(CharacterInstance character, Skill skill)
        {
            if (!Validate(character, skill))
            {
                return;
            }

            character.AddSkill(skill);
            _brain?.PublishCharacterLearnedSkill(character, skill);
            TurnrootLogger.Log(
                $"{character.CharacterTemplate?.DisplayName} learned {skill.SkillName}"
            );
        }

        public void RemoveSkill(CharacterInstance character, SkillInstance skill)
        {
            if (!Validate(character, skill))
            {
                return;
            }

            character.RemoveSkill(skill);
            _brain?.PublishCharacterRemovedSkill(character, skill.SkillTemplate);
            TurnrootLogger.Log(
                $"{character.CharacterTemplate?.DisplayName} removed {skill.SkillTemplate?.SkillName}"
            );
        }

        public OperationResult EquipSkill(CharacterInstance character, Skill skill)
        {
            if (!Validate(character, skill))
            {
                return OperationResult.Failure("Invalid parameters");
            }

            var instance = character.SkillInstances?.Find(s => s.SkillTemplate == skill);
            if (instance == null)
            {
                return OperationResult.Failure("Skill not found on character");
            }

            instance.SetEquipped(true, character);
            _brain?.PublishSkillEquipped(character, skill);
            TurnrootLogger.Log($"Equipped {skill.SkillName} on {character.Id}");
            return OperationResult.SuccessResult();
        }

        public OperationResult UnequipSkill(CharacterInstance character, Skill skill)
        {
            if (!Validate(character, skill))
            {
                return OperationResult.Failure("Invalid parameters");
            }

            var instance = character.SkillInstances?.Find(s => s.SkillTemplate == skill);
            if (instance == null)
            {
                return OperationResult.Failure("Skill not found on character");
            }

            instance.SetEquipped(false, character);
            _brain?.PublishSkillUnequipped(character, skill);
            TurnrootLogger.Log($"Unequipped {skill.SkillName} on {character.Id}");
            return OperationResult.SuccessResult();
        }

        #endregion
    }
}
