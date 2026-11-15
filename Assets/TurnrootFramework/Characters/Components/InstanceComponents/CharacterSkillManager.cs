using Turnroot.Skills;
using Turnroot.GameSettings;
using Turnroot.Utilities;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles skill management for a character instance.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Skills

        /// <summary>
        /// Add a skill from a template.
        /// </summary>
        internal void AddSkill(Skill skillTemplate)
        {
            var skillInstance = new SkillInstance(skillTemplate);
            _skillInstances.Add(skillInstance);
            AutoEquipSkillsWithinLimit();
        }

        /// <summary>
        /// Remove a skill instance.
        /// </summary>
        internal void RemoveSkill(SkillInstance skillInstance)
        {
            if (IsPersonalSkill(skillInstance?.SkillTemplate))
            {
                "The personal skill cannot be removed.".LogWarning();
                return;
            }

            _skillInstances.Remove(skillInstance);
            AutoEquipSkillsWithinLimit();
        }

        public bool IsPersonalSkill(Skill skill) =>
            skill != null && skill == _characterTemplate?.PersonalSkill;

        public int CountEquippedRegularSkills()
        {
            int count = 0;
            foreach (var skillInstance in _skillInstances)
            {
                if (
                    skillInstance?.Equipped == true
                    && !IsPersonalSkill(skillInstance.SkillTemplate)
                )
                {
                    count++;
                }
            }
            return count;
        }

        public bool CanEquipSkill(Skill skill)
        {
            if (IsPersonalSkill(skill))
            {
                return true;
            }

            var instance = _skillInstances?.Find(s => s?.SkillTemplate == skill);
            if (instance == null || instance.Equipped)
            {
                return instance != null;
            }

            int maximum = GameplayGeneralSettings.Instance?.GetMaxEquippedSkills() ?? 0;
            return CountEquippedRegularSkills() < maximum;
        }

        public void AutoEquipSkillsWithinLimit()
        {
            if (_skillInstances == null)
            {
                return;
            }

            var personalSkill = _characterTemplate?.PersonalSkill;
            bool changed = false;
            foreach (var skillInstance in _skillInstances)
            {
                if (skillInstance?.SkillTemplate == personalSkill && personalSkill != null)
                {
                    changed |= !skillInstance.Equipped;
                    skillInstance.SetEquipped(true, this);
                }
            }

            var settings = GameplayGeneralSettings.Instance;
            if (settings == null)
            {
                if (changed)
                {
                    NeedsPersist = true;
                }
                return;
            }

            int maximum = System.Math.Max(
                0,
                settings.GetMaxEquippedSkills()
            );
            int equippedCount = 0;

            foreach (var skillInstance in _skillInstances)
            {
                if (
                    skillInstance == null
                    || skillInstance.SkillTemplate == personalSkill
                    || !skillInstance.Equipped
                )
                {
                    continue;
                }

                if (equippedCount < maximum)
                {
                    equippedCount++;
                }
                else
                {
                    skillInstance.SetEquipped(false, this);
                    changed = true;
                }
            }

            foreach (var skillInstance in _skillInstances)
            {
                if (
                    equippedCount >= maximum
                    || skillInstance == null
                    || skillInstance.Equipped
                    || skillInstance.SkillTemplate == personalSkill
                )
                {
                    continue;
                }

                skillInstance.SetEquipped(true, this);
                changed = true;
                equippedCount++;
            }

            if (changed)
            {
                NeedsPersist = true;
            }
        }

        #endregion
    }
}
