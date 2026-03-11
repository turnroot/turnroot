using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Growth Rates

        /// <summary>
        /// Apply stat growth for level up with randomized rolls.
        /// <remarks>
        /// Only unbounded stats (Strength/Defense/etc.) are considered here;
        /// HP growth is applied separately by the caller and is not included in
        /// the returned list.  The list is later used to decide good/bad
        /// level‑up events, so health increases are intentionally ignored.
        /// </remarks>
        /// Returns list of stats that increased.
        /// </summary>
        public static List<UnboundedStatType> ApplyStatGrowths(
            List<UnboundedStatModifier> baseGrowths,
            List<UnboundedStatModifier> classGrowthModifiers,
            CharacterInstance character,
            List<UnboundedStatModifier> classCaps = null
        )
        {
            var brain = Object.FindFirstObjectByType<Gameplay.Brain.Brain>();
            var increasedStats = new List<UnboundedStatType>();

            if (character == null || baseGrowths == null)
            {
                return increasedStats;
            }

            foreach (var baseGrowth in baseGrowths)
            {
                var stat = character.GetUnboundedStat(baseGrowth.unboundedStatType);
                if (stat == null)
                {
                    continue;
                }

                // Calculate total growth rate (base + class modifier)
                float totalGrowth = baseGrowth.value;

                if (classGrowthModifiers != null)
                {
                    var classModifier = classGrowthModifiers.Find(m =>
                        m.unboundedStatType == baseGrowth.unboundedStatType
                    );
                    if (!classModifier.Equals(default(UnboundedStatModifier)))
                    {
                        totalGrowth += classModifier.value;
                    }
                }

                var t = totalGrowth;
                totalGrowth = Mathf.Clamp(totalGrowth, 0f, 100f);
                // If t > totalGrowth, the stat gains +2 instead of +1 if GameplayGeneralSettings.LevelUpExtraGrowthChance is enabled
                if (GameplayGeneralSettings.Instance.LevelUpExtraGrowthChance && t > totalGrowth)
                {
                    // auto increase by 1 for exceeding 100% growth, then roll for extra growth
                    stat.SetCurrent(stat.Current + 1);
                    increasedStats.Add(baseGrowth.unboundedStatType);
                }

                float roll = Random.Range(0f, 100f);
                if (roll < totalGrowth)
                {
                    // Check if stat is below cap
                    float cap = float.MaxValue;
                    if (classCaps != null)
                    {
                        var capModifier = classCaps.Find(c =>
                            c.unboundedStatType == baseGrowth.unboundedStatType
                        );
                        if (capModifier.value > 0)
                        {
                            cap = capModifier.value;
                        }
                    }

                    if (stat.Current < cap)
                    {
                        stat.SetCurrent(stat.Current + 1);
                        increasedStats.Add(baseGrowth.unboundedStatType);
                    }
                }
            }

            if (brain != null)
            {
                if (increasedStats.Count <= 2)
                {
                    brain.PublishBadLevelUp(character);
                }
                else
                {
                    brain.PublishGoodLevelUp(character);
                }
            }
            // if brain is null (e.g. running inside editor test window), skip event publishing

            return increasedStats;
        }

        #endregion
    }
}