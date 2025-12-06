using System;
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Helper utility for applying stat modifiers to characters.
    /// Provides high-level operations that delegate to StatExtensions for the actual work.
    /// </summary>
    public static class StatApplicationHelper
    {
        #region Bonus Application

        /// <summary>
        /// Apply bounded stat modifiers to a character's bonus values.
        /// Delegates to StatExtensions.ApplyBoundedBonuses.
        /// </summary>
        public static void ApplyBoundedBonuses(
            List<StatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        )
        {
            character?.ApplyBoundedBonuses(modifiers);
        }

        /// <summary>
        /// Apply unbounded stat modifiers to a character's bonus values.
        /// Delegates to StatExtensions.ApplyUnboundedBonuses.
        /// </summary>
        public static void ApplyUnboundedBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        )
        {
            character?.ApplyUnboundedBonuses(modifiers);
        }

        /// <summary>
        /// Remove bounded stat modifiers from a character's bonus values.
        /// Delegates to StatExtensions.RemoveBoundedBonuses.
        /// </summary>
        public static void RemoveBoundedBonuses(
            List<StatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        )
        {
            character?.RemoveBoundedBonuses(modifiers);
        }

        /// <summary>
        /// Remove unbounded stat modifiers from a character's bonus values.
        /// Delegates to StatExtensions.RemoveUnboundedBonuses.
        /// </summary>
        public static void RemoveUnboundedBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        )
        {
            character?.RemoveUnboundedBonuses(modifiers);
        }

        #endregion

        #region Permanent Bonuses

        /// <summary>
        /// Apply permanent bounded stat increases (class change bonuses).
        /// </summary>
        /// <param name="modifiers">List of stat modifiers to apply permanently</param>
        /// <param name="character">Target character instance</param>
        /// <param name="logChanges">Whether to log changes to console</param>
        public static void ApplyBoundedPermanentBonuses(
            List<StatModifier> modifiers,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (character == null || modifiers == null)
                return;

            foreach (var modifier in modifiers)
            {
                if (modifier.value != 0)
                {
                    var stat = character.GetBoundedStat(modifier.boundedStatType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + modifier.value);
                        if (logChanges)
                        {
                            Debug.Log(
                                $"Class change bonus: {modifier.boundedStatType} +{modifier.value} (now {stat.Current})"
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply permanent unbounded stat increases (class change bonuses).
        /// </summary>
        /// <param name="modifiers">List of stat modifiers to apply permanently</param>
        /// <param name="character">Target character instance</param>
        /// <param name="logChanges">Whether to log changes to console</param>
        public static void ApplyUnboundedPermanentBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (character == null || modifiers == null)
                return;

            foreach (var modifier in modifiers)
            {
                if (modifier.value != 0)
                {
                    var stat = character.GetUnboundedStat(modifier.unboundedStatType);
                    if (stat != null)
                    {
                        stat.SetCurrent(stat.Current + modifier.value);
                        if (logChanges)
                        {
                            Debug.Log(
                                $"Class change bonus: {modifier.unboundedStatType} +{modifier.value} (now {stat.Current})"
                            );
                        }
                    }
                }
            }
        }

        #endregion

        #region Stat Enforcement

        /// <summary>
        /// Enforce bounded stat minimums - raise stats to minimum if below.
        /// </summary>
        /// <param name="minimums">List of minimum stat values</param>
        /// <param name="character">Target character instance</param>
        /// <param name="logChanges">Whether to log changes to console</param>
        public static void EnforceBoundedMinimums(
            List<StatModifier> minimums,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (character == null || minimums == null)
                return;

            foreach (var minimum in minimums)
            {
                if (minimum.value > 0)
                {
                    var stat = character.GetBoundedStat(minimum.boundedStatType);
                    if (stat != null && stat.Current < minimum.value)
                    {
                        stat.SetCurrent(minimum.value);
                        if (logChanges)
                        {
                            Debug.Log(
                                $"Enforced minimum: {minimum.boundedStatType} raised to {minimum.value}"
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enforce unbounded stat minimums - raise stats to minimum if below.
        /// </summary>
        /// <param name="minimums">List of minimum stat values</param>
        /// <param name="character">Target character instance</param>
        /// <param name="logChanges">Whether to log changes to console</param>
        public static void EnforceUnboundedMinimums(
            List<UnboundedStatModifier> minimums,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (character == null || minimums == null)
                return;

            foreach (var minimum in minimums)
            {
                if (minimum.value > 0)
                {
                    var stat = character.GetUnboundedStat(minimum.unboundedStatType);
                    if (stat != null && stat.Current < minimum.value)
                    {
                        stat.SetCurrent(minimum.value);
                        if (logChanges)
                        {
                            Debug.Log(
                                $"Enforced minimum: {minimum.unboundedStatType} raised to {minimum.value}"
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply bounded stat caps - set maximum values for bounded stats.
        /// </summary>
        /// <param name="caps">List of stat cap values</param>
        /// <param name="character">Target character instance</param>
        public static void ApplyBoundedCaps(List<StatModifier> caps, CharacterInstance character)
        {
            if (character == null || caps == null)
                return;

            foreach (var cap in caps)
            {
                if (cap.value > 0)
                {
                    var stat = character.GetBoundedStat(cap.boundedStatType);
                    if (stat != null)
                    {
                        stat.SetMax(cap.value);
                    }
                }
            }
        }

        /// <summary>
        /// Check if any unbounded stat exceeds class caps.
        /// </summary>
        /// <param name="caps">List of stat cap values</param>
        /// <param name="character">Character to check</param>
        /// <returns>True if any stat exceeds its cap</returns>
        public static bool IsAboveUnboundedCaps(
            List<UnboundedStatModifier> caps,
            CharacterInstance character
        )
        {
            if (character == null || caps == null)
                return false;

            foreach (var cap in caps)
            {
                if (cap.value > 0)
                {
                    var stat = character.GetUnboundedStat(cap.unboundedStatType);
                    if (stat != null && stat.Current > cap.value)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Growth Rates

        /// <summary>
        /// Apply stat growth for level up with randomized rolls.
        /// Returns list of stats that increased.
        /// </summary>
        /// <param name="baseGrowths">Character's base growth rates</param>
        /// <param name="classGrowthModifiers">Class modifiers to growth rates</param>
        /// <param name="character">Character instance to level up</param>
        /// <param name="classCaps">Optional stat caps to enforce</param>
        /// <returns>List of stats that increased during level up</returns>
        public static List<UnboundedStatType> ApplyStatGrowths(
            List<UnboundedStatModifier> baseGrowths,
            List<UnboundedStatModifier> classGrowthModifiers,
            CharacterInstance character,
            List<UnboundedStatModifier> classCaps = null
        )
        {
            var increasedStats = new List<UnboundedStatType>();

            if (character == null || baseGrowths == null)
                return increasedStats;

            foreach (var baseGrowth in baseGrowths)
            {
                var stat = character.GetUnboundedStat(baseGrowth.unboundedStatType);
                if (stat == null)
                    continue;

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
                    // If no matching modifier is found, add nothing (explicitly 0)
                }

                // Clamp growth rate to 0-100 range
                totalGrowth = Mathf.Clamp(totalGrowth, 0f, 100f);

                float roll = UnityEngine.Random.Range(0f, 100f);
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

            return increasedStats;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate required references for stat operations.
        /// Returns true if valid, false otherwise with warning logged.
        /// </summary>
        /// <param name="character">Character instance to validate</param>
        /// <param name="classData">Class data to validate</param>
        /// <param name="operationName">Name of operation for logging</param>
        /// <returns>True if references are valid, false otherwise</returns>
        public static bool ValidateReferences(
            CharacterInstance character,
            CharacterClassData classData,
            string operationName
        )
        {
            if (character == null)
            {
                Debug.LogWarning($"{operationName}: character is null");
                return false;
            }

            if (classData == null)
            {
                Debug.LogWarning($"{operationName}: classData is null");
                return false;
            }

            return true;
        }

        #endregion
    }
}
