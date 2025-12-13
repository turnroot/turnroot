using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Objects.Components;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Skills and mastery configuration for a character class.
    /// Extracted from CharacterClassData for cleaner organization.
    /// </summary>
    [Serializable]
    public class ClassMastery
    {
        [Header("Innate Skills")]
        [Tooltip("Skills automatically granted when this class is equipped")]
        public List<Skill> InnateSkills = new();

        [Header("Mastery System")]
        [Tooltip("If true, this class uses a mastery system for skill progression")]
        public bool UsesMasterySystem = false;

        [Tooltip("Mastery criteria for this class (what the character must do to gain mastery)")]
        public MasteryCriteria MasteryCriteria;

        [Tooltip("Mastery targets (specific skills or stat thresholds required for mastery)")]
        public List<MasteryTarget> MasteryTargets = new();

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
        /// Checks whether a character has met the mastery criteria.
        /// </summary>
        public bool HasMetMasteryCriteria(int achievedMasteryCount) => UsesMasterySystem && MasteryCriteria != null && achievedMasteryCount >= MasteryTargets.Count;

        /// <summary>
        /// Validates that mastery configuration is complete.
        /// </summary>
        public bool ValidateMasteryConfiguration()
        {
            if (!UsesMasterySystem)
            {
                return true; // No validation needed if not using mastery
            }

            if (MasteryTargets.Count == 0)
            {
                Debug.LogWarning("Class uses mastery system but has no MasteryTargets defined.");
                return false;
            }

            return true;
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

    /// <summary>
    /// Specific target for mastery progression.
    /// </summary>
    [Serializable]
    public class MasteryTarget
    {
        [Tooltip("Description of what must be achieved")]
        public string TargetDescription;

        [Tooltip("Skill that must be learned or used (if applicable)")]
        public Skill RequiredSkill;

        [Tooltip("Threshold value (weapon level, usage count, etc.)")]
        public int ThresholdValue;
    }
}
