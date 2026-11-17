using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain;
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
            var validationResult = ValidateLevelUp();
            if (!validationResult.Success)
            {
                return validationResult;
            }

            _currentLevel++;
            UpdateLevelStat();

            var hpStatResult = GetHealthStatForLevelUp(out var hpStat);
            if (!hpStatResult.Success)
            {
                return hpStatResult;
            }

            ApplyHpGrowth(hpStat);

            var growthRates = GetEffectiveGrowthRates();
            var increasedStats = ApplyUnboundedGrowth(growthRates);
            ApplyBonusHpForFullGrowth(hpStat, increasedStats);

            // Persist the new stat values immediately so they survive the session.
            PersistStatsToLtm();

            return OperationResult.Successful();
        }

        private OperationResult ValidateLevelUp()
        {
            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterInstance.LevelUp",
                out var missing,
                (_currentClass, nameof(_currentClass)),
                (_currentClass?.ClassData, "classData")
            );

            return ok
                ? OperationResult.Successful()
                : OperationResult.Failure(
                    $"CharacterInstance.LevelUp failed: missing {string.Join(", ", missing)}"
                );
        }

        private OperationResult GetHealthStatForLevelUp(out BoundedCharacterStat hpStat)
        {
            hpStat = GetBoundedStat(BoundedStatType.Health);
            return hpStat != null
                ? OperationResult.Successful()
                : OperationResult.Failure("CharacterInstance.LevelUp: HP stat not found");
        }

        private void UpdateLevelStat()
        {
            var levelStat = GetBoundedStat(BoundedStatType.Level);
            if (levelStat == null)
            {
                return;
            }

            if (levelStat.Max < _currentLevel)
            {
                levelStat.SetMax(_currentLevel);
            }

            levelStat.SetCurrent(_currentLevel);
        }

        private void ApplyHpGrowth(BoundedCharacterStat hpStat)
        {
            float hpGrowth = GetEffectiveHpGrowthRate();
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (hpGrowth > 100f && GameplayGeneralSettings.Instance.LevelUpExtraGrowthChance)
            {
                IncreaseHp(hpStat);
                hpGrowth -= 100f;
            }

            if (roll < hpGrowth)
            {
                IncreaseHp(hpStat);
            }
        }

        private List<UnboundedStatType> ApplyUnboundedGrowth(
            List<UnboundedStatModifier> growthRates
        )
        {
            var caps =
                _currentClass?.ClassData.Stats?.StatCaps ?? new List<UnboundedStatModifier>();
            return StatApplicationHelper.ApplyStatGrowths(
                growthRates,
                new List<UnboundedStatModifier>(),
                this,
                caps
            );
        }

        private void ApplyBonusHpForFullGrowth(
            BoundedCharacterStat hpStat,
            List<UnboundedStatType> increasedStats
        )
        {
            if (
                Enum.GetValues(typeof(UnboundedStatType))
                    .Cast<UnboundedStatType>()
                    .Where(statType => statType != UnboundedStatType.Movement)
                    .All(increasedStats.Contains)
            )
            {
                IncreaseHp(hpStat);
            }
        }

        private static void IncreaseHp(BoundedCharacterStat hpStat)
        {
            hpStat.SetMax(hpStat.Max + 1f);
            hpStat.SetCurrent(hpStat.GetCurrent() + 1f);
        }

        private List<UnboundedStatModifier> GetEffectiveGrowthRates()
        {
            var effectiveRates = GetPersonalGrowthRates();
            AddMissingGrowthRates(effectiveRates);
            ApplyCustomGrowthRates(effectiveRates, LoadCustomGrowthRatesFromLtm());
            ApplyClassGrowthRateModifiers(effectiveRates);
            return effectiveRates;
        }

        private List<UnboundedStatModifier> GetPersonalGrowthRates()
        {
            var effectiveRates = new List<UnboundedStatModifier>();
            if (_characterTemplate?.PersonalGrowthRates == null)
            {
                return effectiveRates;
            }

            effectiveRates.AddRange(
                _characterTemplate.PersonalGrowthRates.FindAll(r =>
                    !r.isBounded
                    && Enum.IsDefined(typeof(UnboundedStatType), r.unboundedStatType)
                    && r.unboundedStatType != UnboundedStatType.Movement
                )
            );
            return effectiveRates;
        }

        private void AddMissingGrowthRates(List<UnboundedStatModifier> effectiveRates)
        {
            var growthStatTypes = Enum
                .GetValues(typeof(UnboundedStatType))
                .Cast<UnboundedStatType>()
                .Where(statType => statType != UnboundedStatType.Movement)
                .ToArray();

            foreach (var statType in growthStatTypes)
            {
                if (!effectiveRates.Exists(rate => rate.unboundedStatType == statType))
                {
                    effectiveRates.Add(new UnboundedStatModifier(statType, 0f));
                }
            }
        }

        private static void ApplyCustomGrowthRates(
            List<UnboundedStatModifier> effectiveRates,
            List<UnboundedStatModifier> customRates
        )
        {
            if (customRates == null)
            {
                return;
            }

            foreach (var customRate in customRates)
            {
                int index = effectiveRates.FindIndex(e =>
                    e.unboundedStatType == customRate.unboundedStatType
                );
                if (index >= 0)
                {
                    effectiveRates[index] = customRate;
                }
                else
                {
                    effectiveRates.Add(customRate);
                }
            }
        }

        private void ApplyClassGrowthRateModifiers(List<UnboundedStatModifier> effectiveRates)
        {
            var classMods = _currentClass?.ClassData.Stats?.GrowthRateModifiers;
            if (classMods == null)
            {
                return;
            }

            foreach (var classMod in classMods)
            {
                if (
                    classMod.isBounded
                    || !Enum.IsDefined(typeof(UnboundedStatType), classMod.unboundedStatType)
                    || classMod.unboundedStatType == UnboundedStatType.Movement
                )
                {
                    continue;
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

        private float GetEffectiveHpGrowthRate()
        {
            var customHpRate = LoadCustomHpGrowthRateFromLtm();
            float hpRate = customHpRate ?? GetPersonalHpGrowthRate();
            return hpRate + GetClassHpGrowthRate();
        }

        private float GetPersonalHpGrowthRate()
        {
            if (_characterTemplate?.PersonalGrowthRates == null)
            {
                return 0f;
            }

            var entry = _characterTemplate.PersonalGrowthRates.Find(r =>
                r.isBounded && r.boundedStatType == BoundedStatType.Health
            );
            return entry.value != 0f ? entry.value : 0f;
        }

        private float GetClassHpGrowthRate()
        {
            var classMods = _currentClass?.ClassData.Stats?.GrowthRateModifiers;
            if (classMods == null)
            {
                return 0f;
            }

            var entry = classMods.Find(r =>
                r.isBounded && r.boundedStatType == BoundedStatType.Health
            );
            return entry.value != 0f ? entry.value : 0f;
        }

        private List<UnboundedStatModifier> LoadCustomGrowthRatesFromLtm()
        {
            try
            {
                var ltm = Brain.Instance.GetComponent<Gameplay.Brain.Components.LongTermMemory>();
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

                return ConvertCustomGrowthRates(data.growthRates);
            }
            catch (Exception ex)
            {
                $"Failed to load custom growth rates from LTM: {ex.Message}".LogWarning(
                    "CharacterInstance"
                );
                return null;
            }
        }

        private static List<UnboundedStatModifier> ConvertCustomGrowthRates(
            Dictionary<string, float> growthRates
        )
        {
            var rates = new List<UnboundedStatModifier>();
            foreach (var kvp in growthRates)
            {
                if (kvp.Key == "Health")
                {
                    continue;
                }

                if (
                    Enum.TryParse<UnboundedStatType>(kvp.Key, out var statType)
                    && Enum.IsDefined(typeof(UnboundedStatType), statType)
                    && statType != UnboundedStatType.Movement
                )
                {
                    rates.Add(new UnboundedStatModifier(statType, kvp.Value));
                }
            }

            return rates.Count > 0 ? rates : null;
        }

        private float? LoadCustomHpGrowthRateFromLtm()
        {
            try
            {
                var brain = Brain.Instance;
                var ltm = brain.ltm;
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
            catch (Exception ex)
            {
                $"Failed to load custom HP growth rate from LTM: {ex.Message}".LogWarning(
                    "CharacterInstance"
                );
                return null;
            }
        }

        [Serializable]
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
