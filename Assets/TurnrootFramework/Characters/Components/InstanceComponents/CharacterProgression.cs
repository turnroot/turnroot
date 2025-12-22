using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles level up, stat growth, and experience rank progression.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Level Up & Growth

        /// <summary>
        /// Level up the character and apply random stat growth rolls.
        /// Internal method - use CharactersBrain.LevelUpCharacter() to publish events.
        /// </summary>
        internal void LevelUp()
        {
            _currentLevel++;
            // HP always increases by 1 on level up
            var hpStat = GetBoundedStat(BoundedStatType.Health);
            hpStat.SetCurrent(hpStat.GetCurrent() + 1f);

            var growthRates = GetEffectiveGrowthRates();
            // TODO: Ensure there is always a class- character cannot level up without one

            var increasedStats = StatApplicationHelper.ApplyStatGrowths(
                growthRates,
                new List<UnboundedStatModifier>(), // Already combined in GetEffectiveGrowthRates
                this,
                _currentClass.ClassData.Stats.UnboundedStatCaps
            );

            if (increasedStats.Count == UnboundedStats.Count)
            {
                hpStat.SetCurrent(hpStat.GetCurrent() + 1f);
            }
        }

        /// <summary>
        /// Get effective growth rates combining personal and class growth rates.
        /// Personal growth rates from CharacterData are added to class growth rate modifiers.
        /// </summary>
        private List<UnboundedStatModifier> GetEffectiveGrowthRates()
        {
            var effectiveRates = new List<UnboundedStatModifier>();

            // Start with personal growth rates from CharacterData
            if (_characterTemplate?.PersonalGrowthRates != null)
            {
                effectiveRates.AddRange(_characterTemplate.PersonalGrowthRates);
            }

            foreach (var classMod in _currentClass.ClassData.Stats.GrowthRateModifiers)
            {
                int index = effectiveRates.FindIndex(e =>
                    e.unboundedStatType == classMod.unboundedStatType
                );
                if (index != -1)
                {
                    // Combine with existing personal rate
                    var existing = effectiveRates[index];
                    effectiveRates[index] = new UnboundedStatModifier(
                        classMod.unboundedStatType,
                        existing.value + classMod.value
                    );
                }
                else
                {
                    // Add class modifier
                    effectiveRates.Add(classMod);
                }
            }

            return effectiveRates;
        }

        #endregion

        #region Experience Ranks

        /// <summary>
        /// Get experience rank by type ID (e.g., "Swords", "Magic").
        /// </summary>
        public ExperienceRankInstance GetExperienceRank(string experienceTypeId) =>
            _experienceRanks.Find(e => e.ExperienceTypeId == experienceTypeId);

        /// <summary>
        /// Add experience to a specific experience type.
        /// </summary>
        internal void AddExperience(string experienceTypeId, int amount)
        {
            var rank = GetExperienceRank(experienceTypeId);
            if (rank != null)
            {
                rank.AddExperience(amount);
            }
            else
            {
                // Create new experience rank starting at E
                var newRank = new ExperienceRankInstance(
                    experienceTypeId,
                    CommonAncestors.LeveledLetteredField.E
                );
                newRank.AddExperience(amount);
                _experienceRanks.Add(newRank);
            }
        }

        /// <summary>
        /// Check if character meets an experience rank requirement.
        /// </summary>
        public bool MeetsExperienceRequirement(string experienceTypeId, string minRankLetter)
        {
            var rank = GetExperienceRank(experienceTypeId);
            return rank != null && rank.Rank.CompareTo(minRankLetter) >= 0;
        }

        #endregion
    }
}
