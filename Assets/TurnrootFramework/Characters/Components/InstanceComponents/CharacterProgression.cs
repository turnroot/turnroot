using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;
using Turnroot.Utilities;

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
        internal OperationResult LevelUp()
        {
            // Ensure character has a class equipped before leveling up — validate and return clear diagnostics
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.LevelUp",
                out var missing,
                (_currentClass, nameof(_currentClass)),
                (_currentClass?.ClassData, "classData")
            );

            if (!ok)
            {
                var msg = $"CharacterInstance.LevelUp failed: missing {string.Join(", ", missing)}";
                return OperationResult.Failure(msg);
            }

            _currentLevel++;
            // HP always increases by 1 on level up
            var hpStat = GetBoundedStat(BoundedStatType.Health);
            hpStat.SetCurrent(hpStat.GetCurrent() + 1f);

            var growthRates = GetEffectiveGrowthRates();

            var caps =
                _currentClass?.ClassData?.Stats?.UnboundedStatCaps
                ?? new List<UnboundedStatModifier>();

            var increasedStats = StatApplicationHelper.ApplyStatGrowths(
                growthRates,
                new List<UnboundedStatModifier>(),
                this,
                caps
            );

            if (increasedStats.Count == UnboundedStats.Count)
            {
                hpStat.SetCurrent(hpStat.GetCurrent() + 1f);
            }

            return OperationResult.SuccessResult();
        }

        private List<UnboundedStatModifier> GetEffectiveGrowthRates()
        {
            var effectiveRates = new List<UnboundedStatModifier>();

            if (_characterTemplate?.PersonalGrowthRates != null)
            {
                effectiveRates.AddRange(_characterTemplate.PersonalGrowthRates);
            }

            var classMods = _currentClass?.ClassData?.Stats?.GrowthRateModifiers;
            if (classMods != null)
            {
                foreach (var classMod in classMods)
                {
                    int index = effectiveRates.FindIndex(e =>
                        e.unboundedStatType == classMod.unboundedStatType
                    );
                    if (index != -1)
                    {
                        var existing = effectiveRates[index];
                        effectiveRates[index] = new UnboundedStatModifier(
                            classMod.unboundedStatType,
                            existing.value + classMod.value
                        );
                    }
                    else
                    {
                        effectiveRates.Add(classMod);
                    }
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

        internal void AddExperience(string experienceTypeId, int amount)
        {
            var rank = GetExperienceRank(experienceTypeId);
            if (rank != null)
            {
                rank.AddExperience(amount);
            }
            else
            {
                var newRank = new ExperienceRankInstance(
                    experienceTypeId,
                    CommonAncestors.LeveledLetteredField.E
                );
                newRank.AddExperience(amount);
                _experienceRanks.Add(newRank);
            }
        }

        public bool MeetsExperienceRequirement(string experienceTypeId, string minRankLetter)
        {
            var rank = GetExperienceRank(experienceTypeId);
            return rank != null && rank.Rank.CompareTo(minRankLetter) >= 0;
        }

        #endregion
    }
}
