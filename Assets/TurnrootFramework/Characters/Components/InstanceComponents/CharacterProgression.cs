using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Handles level up, stat growth, and experience rank progression.
    /// </summary>
    public partial class CharacterInstance
    {
        #region Level Up & Growth

        /// Internal method - use CharactersBrain.LevelUpCharacter() to publish events.
        public OperationResult LevelUp()
        {
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

            // keep the bounded Level stat in sync with our internal level counter.
            var levelStat = GetBoundedStat(BoundedStatType.Level);
            if (levelStat != null)
            {
                // make sure max is at least as high as the new level so it can increase
                if (levelStat.Max < _currentLevel)
                {
                    levelStat.SetMax(_currentLevel);
                }

                levelStat.SetCurrent(_currentLevel);
            }

            var hpStat = GetBoundedStat(BoundedStatType.Health);
            if (hpStat == null)
            {
                return OperationResult.Failure("CharacterInstance.LevelUp: HP stat not found");
            }

            // HP growth roll based on combined personal + class rates
            float hpGrowth = GetEffectiveHpGrowthRate();
            float roll = Random.Range(0f, 100f);
            if (hpGrowth > 100f && GameplayGeneralSettings.Instance.LevelUpExtraGrowthChance)
            {
                // auto +1 then roll for extra; increase both max and current so stat can grow
                hpStat.SetMax(hpStat.Max + 1f);
                hpStat.SetCurrent(hpStat.GetCurrent() + 1f);
                hpGrowth -= 100f; // remaining chance for the extra roll
            }
            if (roll < hpGrowth)
            {
                // grow HP normally (max and current)
                hpStat.SetMax(hpStat.Max + 1f);
                hpStat.SetCurrent(hpStat.GetCurrent() + 1f);
            }

            var growthRates = GetEffectiveGrowthRates();

            var caps =
                _currentClass?.ClassData.Stats?.StatCaps ?? new List<UnboundedStatModifier>();

            var increasedStats = StatApplicationHelper.ApplyStatGrowths(
                growthRates,
                new List<UnboundedStatModifier>(),
                this,
                caps
            );

            // old behaviour of bonus HP when every unbounded stat increased can be preserved
            if (increasedStats.Count == UnboundedStats.Count)
            {
                hpStat.SetMax(hpStat.Max + 1f);
                hpStat.SetCurrent(hpStat.GetCurrent() + 1f);
            }

            // Persist the new stat values immediately so they survive the session.
            PersistStatsToLtm();

            return OperationResult.Successful();
        }

        private List<UnboundedStatModifier> GetEffectiveGrowthRates()
        {
            // combine only the unbounded/stat entries and ignore any bounded (HP) modifiers
            var effectiveRates = new List<UnboundedStatModifier>();

            // First, check LTM for custom growth rates (e.g., from star gifts)
            var customRates = LoadCustomGrowthRatesFromLtm();
            if (customRates != null && customRates.Count > 0)
            {
                effectiveRates.AddRange(customRates);
            }
            else if (_characterTemplate?.PersonalGrowthRates != null)
            {
                // Fall back to template growth rates if no custom rates exist
                effectiveRates.AddRange(
                    _characterTemplate.PersonalGrowthRates.FindAll(r => !r.isBounded)
                );
            }

            var classMods = _currentClass?.ClassData.Stats?.GrowthRateModifiers;
            if (classMods != null)
            {
                foreach (var classMod in classMods)
                {
                    if (classMod.isBounded)
                    {
                        continue; // skip HP entries in this list
                    }

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

        private float GetEffectiveHpGrowthRate()
        {
            float hpRate = 0f;

            // First, check LTM for custom HP growth rate
            var customHpRate = LoadCustomHpGrowthRateFromLtm();
            if (customHpRate.HasValue)
            {
                hpRate = customHpRate.Value;
            }
            else if (_characterTemplate?.PersonalGrowthRates != null)
            {
                // Fall back to template HP growth rate
                var entry = _characterTemplate.PersonalGrowthRates.Find(r =>
                    r.isBounded && r.boundedStatType == BoundedStatType.Health
                );
                if (entry.value != 0f)
                {
                    hpRate += entry.value;
                }
            }

            var classMods = _currentClass?.ClassData.Stats?.GrowthRateModifiers;
            if (classMods != null)
            {
                var centry = classMods.Find(r =>
                    r.isBounded && r.boundedStatType == BoundedStatType.Health
                );
                if (centry.value != 0f)
                {
                    hpRate += centry.value;
                }
            }

            return hpRate;
        }

        private List<UnboundedStatModifier> LoadCustomGrowthRatesFromLtm()
        {
            try
            {
                var brain = Object.FindFirstObjectByType<Gameplay.Brain.Brain>();
                var ltm = brain?.GetComponent<Gameplay.Brain.Components.LongTermMemory>();
                if (ltm == null)
                {
                    return null;
                }

                string key = $"CharacterGrowthRates/{Id}";
                var json = ltm.Recall(key);
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var data = JsonUtility.FromJson<GrowthRatesDto>(json);
                if (data?.growthRates == null)
                {
                    return null;
                }

                var rates = new List<UnboundedStatModifier>();
                foreach (var kvp in data.growthRates)
                {
                    // Skip HP, it's handled separately
                    if (kvp.Key == "Health")
                    {
                        continue;
                    }

                    if (System.Enum.TryParse<UnboundedStatType>(kvp.Key, out var statType))
                    {
                        rates.Add(new UnboundedStatModifier(statType, kvp.Value));
                    }
                }

                return rates.Count > 0 ? rates : null;
            }
            catch (System.Exception ex)
            {
                $"Failed to load custom growth rates from LTM: {ex.Message}".LogWarning(
                    "CharacterInstance"
                );
                return null;
            }
        }

        private float? LoadCustomHpGrowthRateFromLtm()
        {
            try
            {
                var brain = Object.FindFirstObjectByType<Gameplay.Brain.Brain>();
                var ltm = brain?.GetComponent<Gameplay.Brain.Components.LongTermMemory>();
                if (ltm == null)
                {
                    return null;
                }

                string key = $"CharacterGrowthRates/{Id}";
                var json = ltm.Recall(key);
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var data = JsonUtility.FromJson<GrowthRatesDto>(json);
                return data?.growthRates == null || !data.growthRates.ContainsKey("Health")
                    ? null
                    : data.growthRates["Health"];
            }
            catch (System.Exception ex)
            {
                $"Failed to load custom HP growth rate from LTM: {ex.Message}".LogWarning(
                    "CharacterInstance"
                );
                return null;
            }
        }

        [System.Serializable]
        private class GrowthRatesDto
        {
            public Dictionary<string, float> growthRates;
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
            NeedsPersist = true;
        }

        public bool MeetsExperienceRequirement(string experienceTypeId, string minRankLetter)
        {
            var rank = GetExperienceRank(experienceTypeId);
            return rank != null && rank.Rank.CompareTo(minRankLetter) >= 0;
        }

        #endregion
    }
}
