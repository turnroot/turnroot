using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages character skill learning, removal, equipping, and unequipping operations.
    /// </summary>
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
            Brain.PublishCharacterLearnedSkill(character, skill);

            $"{character.CharacterTemplate?.DisplayName} learned {skill.SkillName}"
        .LogInfo();
        }

        public void RemoveSkill(CharacterInstance character, SkillInstance skill)
        {
            if (!Validate(character, skill))
            {
                return;
            }

            character.RemoveSkill(skill);
            Brain.PublishCharacterRemovedSkill(character, skill.SkillTemplate);

            $"{character.CharacterTemplate?.DisplayName} removed {skill.SkillTemplate?.SkillName}"
        .LogInfo();
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
            Brain.PublishSkillEquipped(character, skill);
            $"Equipped {skill.SkillName} on {character.Id}".LogInfo();
            return OperationResult.Successful();
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
            Brain.PublishSkillUnequipped(character, skill);
            $"Unequipped {skill.SkillName} on {character.Id}".LogInfo();
            return OperationResult.Successful();
        }

        #endregion
    }
}

