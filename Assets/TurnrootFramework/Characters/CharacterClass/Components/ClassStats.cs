using System;
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Stat minimums, caps, bonuses, and growth rates for a character class.
    /// </summary>
    [Serializable]
    public class ClassStats
    {
        [Header("Stat Minimums")]
        [Tooltip("Minimum bounded stat values this class enforces (0 = no minimum)")]
        public List<StatModifier> StatMinimums = new();

        [Tooltip("Minimum unbounded stat values this class enforces (0 = no minimum)")]
        public List<UnboundedStatModifier> UnboundedStatMinimums = new();

        [Header("Stat Caps")]
        [Tooltip("Maximum bounded stat caps this class imposes (0 = no cap)")]
        public List<StatModifier> StatCaps = new();

        [Tooltip("Maximum unbounded stat caps this class imposes (0 = no cap)")]
        public List<UnboundedStatModifier> UnboundedStatCaps = new();

        [Header("Stat Bonuses")]
        [Tooltip("Flat bounded stat bonuses applied when equipping/occupying this class")]
        public List<StatModifier> StatBonuses = new();

        [Tooltip("Flat unbounded stat bonuses applied when equipping/occupying this class")]
        public List<UnboundedStatModifier> UnboundedStatBonuses = new();

        [Tooltip("Growth rate modifiers (percentage 0-100) for stat increases on level up")]
        public List<UnboundedStatModifier> GrowthRateModifiers = new();

        [Header("Class Change Bonuses")]
        [Tooltip(
            "One-time bounded stat bonuses applied when a character first changes into this class"
        )]
        public List<StatModifier> ClassChangeBonuses = new();

        [Tooltip(
            "One-time unbounded stat bonuses applied when a character first changes into this class"
        )]
        public List<UnboundedStatModifier> UnboundedClassChangeBonuses = new();

        /// <summary>
        /// Gets the stat minimum for a specific bounded stat type.
        /// </summary>
        public float GetStatMinimum(BoundedStatType statType)
        {
            var modifier = StatMinimums.Find(m => m.boundedStatType == statType);
            return modifier.value;
        }

        /// <summary>
        /// Gets the stat cap for a specific bounded stat type.
        /// </summary>
        public float GetStatCap(BoundedStatType statType)
        {
            var modifier = StatCaps.Find(m => m.boundedStatType == statType);
            return modifier.value;
        }

        /// <summary>
        /// Gets the stat bonus for a specific bounded stat type.
        /// </summary>
        public float GetStatBonus(BoundedStatType statType)
        {
            var modifier = StatBonuses.Find(m => m.boundedStatType == statType);
            return modifier.value;
        }

        /// <summary>
        /// Gets the growth rate modifier for a specific unbounded stat type.
        /// </summary>
        public float GetGrowthRateModifier(UnboundedStatType statType)
        {
            var modifier = GrowthRateModifiers.Find(m => m.unboundedStatType == statType);
            return modifier.value;
        }

        /// <summary>
        /// Validates that all stat lists have entries for all stat types.
        /// </summary>
        public bool ValidateCompleteness(int expectedBoundedCount, int expectedUnboundedCount)
        {
            return StatMinimums.Count == expectedBoundedCount
                && StatCaps.Count == expectedBoundedCount
                && StatBonuses.Count == expectedBoundedCount
                && UnboundedStatMinimums.Count == expectedUnboundedCount
                && UnboundedStatCaps.Count == expectedUnboundedCount
                && UnboundedStatBonuses.Count == expectedUnboundedCount
                && GrowthRateModifiers.Count == expectedUnboundedCount;
        }
    }
}
