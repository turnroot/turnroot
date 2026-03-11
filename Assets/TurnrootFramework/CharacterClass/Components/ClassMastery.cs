using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.CommonAncestors;
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
        /// Gets the skill granted for a specific weapon proficiency rank (E..S).
        /// </summary>
        public Skill GetSkillAtWeaponRank(WeaponType weaponType, LeveledLetteredField weaponRank)
        {
            if (!ValidationHelper.ValidateNotNull(weaponRank, nameof(weaponRank)))
            {
                return null;
            }

            foreach (
                var bonus in WeaponLevelBonuses.Where(b =>
                    b.WeaponType == weaponType
                    && b.RequiredWeaponRank != null
                    && b.RequiredWeaponRank.Value == weaponRank.Value
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
        public bool HasMetMasteryCriteria(int progressPercent) =>
            // Mastery threshold is fixed at 100%.
            progressPercent >= 100;

        /// <summary>
        /// Validates that mastery configuration is complete.
        /// (MasteredSkill may be empty — mastery will still track progress.)
        /// </summary>
        public OperationResult ValidateMasteryConfiguration() => OperationResult.Successful();
    }

    /// <summary>
    /// Weapon level milestone that grants a skill.
    /// </summary>
    [Serializable]
    public class WeaponLevelBonus
    {
        [Tooltip("Weapon type this bonus applies to")]
        public WeaponType WeaponType;

        [Tooltip("Weapon proficiency rank required to unlock this bonus (E..S)")]
        public LeveledLetteredField RequiredWeaponRank = new(LeveledLetteredField.E);

        [Tooltip("Skill granted upon reaching the required weapon rank")]
        public Skill GrantedSkill;
    }
}
