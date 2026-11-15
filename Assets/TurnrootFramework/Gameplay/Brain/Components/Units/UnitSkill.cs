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
            Brain.battleBrain?.BattleObject?.Context?.RefreshActiveSkillsForUnit(character);
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

            if (character.IsPersonalSkill(skill.SkillTemplate))
            {
                "The personal skill cannot be removed.".LogWarning();
                return;
            }

            character.RemoveSkill(skill);
            Brain.battleBrain?.BattleObject?.Context?.RefreshActiveSkillsForUnit(character);
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

            if (!character.CanEquipSkill(skill))
            {
                return OperationResult.Failure("No available skill slots.");
            }

            instance.SetEquipped(true, character);
            Brain.battleBrain?.BattleObject?.Context?.RefreshActiveSkillsForUnit(character);
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

            if (character.IsPersonalSkill(skill))
            {
                return OperationResult.Failure("The personal skill cannot be unequipped.");
            }

            instance.SetEquipped(false, character);
            Brain.battleBrain?.BattleObject?.Context?.RefreshActiveSkillsForUnit(character);
            Brain.PublishSkillUnequipped(character, skill);
            $"Unequipped {skill.SkillName} on {character.Id}".LogInfo();
            return OperationResult.Successful();
        }

        #endregion
    }
}
