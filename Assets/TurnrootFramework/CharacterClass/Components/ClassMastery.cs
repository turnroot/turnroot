using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Skills and mastery configuration for a character class.
    /// </summary>
    [Serializable]
    public class ClassMastery
    {
        [Header("Innate Skills")]
        [Tooltip("Skills automatically granted when this class is equipped")]
        public List<Skill> InnateSkills = new();

        [Header("Mastery System")]
        // Mastery is always enabled for every class (per-turn + kill bonus). The inspector
        // exposes the skill granted on mastery.
        [Tooltip("Skill granted when this class is mastered")]
        public Skill MasteredSkill;

        [Header("Weapon Level Bonuses")]
        [Tooltip("Skills granted upon reaching specific weapon level milestones")]
        public List<WeaponLevelBonus> WeaponLevelBonuses = new();

        /// <summary>
        /// Gets all skills granted by this class (innate + weapon level bonuses).
        /// </summary>
        public List<Skill> GetAllSkills()
        {
            var allSkills = new List<Skill>(InnateSkills);

            foreach (
                var bonus in WeaponLevelBonuses.Where(b =>
                    b.GrantedSkill != null && !allSkills.Contains(b.GrantedSkill)
                )
            )
            {
                allSkills.Add(bonus.GrantedSkill);
            }

            return allSkills;
        }

        /// <summary>
        /// Gets the skill granted at a specific weapon level.
        /// </summary>
        public Skill GetSkillAtWeaponLevel(WeaponType weaponType, int weaponLevel)
        {
            foreach (
                var bonus in WeaponLevelBonuses.Where(b =>
                    b.WeaponType == weaponType && b.RequiredWeaponLevel == weaponLevel
                )
            )
            {
                return bonus.GrantedSkill;
            }

            return null;
        }

        /// <summary>
        /// Returns true if mastery is considered complete given a progress value (0-100).
        /// </summary>
        public bool HasMetMasteryCriteria(int progressPercent)
        {
            // Mastery threshold is fixed at 100%.
            return progressPercent >= 100;
        }

        /// <summary>
        /// Validates that mastery configuration is complete.
        /// (MasteredSkill may be empty — mastery will still track progress.)
        /// </summary>
        public OperationResult ValidateMasteryConfiguration()
        {
            return OperationResult.Successful();
        }
    }

    /// <summary>
    /// Weapon level milestone that grants a skill.
    /// </summary>
    [Serializable]
    public class WeaponLevelBonus
    {
        [Tooltip("Weapon type this bonus applies to")]
        public WeaponType WeaponType;

        [Tooltip("Weapon level required to unlock this bonus")]
        [Range(1, 10)]
        public int RequiredWeaponLevel;

        [Tooltip("Skill granted upon reaching the required weapon level")]
        public Skill GrantedSkill;
    }
}
