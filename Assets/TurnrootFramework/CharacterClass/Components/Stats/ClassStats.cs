using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.Stats;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Stat minimums, caps, bonuses, and growth rates for a character class.
    /// Each list uses UnboundedStatModifier which handles both bounded (HP) and unbounded stats
    /// via the isBounded flag.
    /// </summary>
    [Serializable]
    public class ClassStats
    {
        [Header("Stat Minimums")]
        [Tooltip(
            "Minimum values for the fixed non-Movement stat set (0 = no minimum). Movement is set in Identity."
        )]
        public List<UnboundedStatModifier> StatMinimums = new();

        [Header("Stat Caps")]
        [Tooltip(
            "Maximum caps for the fixed non-Movement stat set (0 = no cap). Movement is set in Identity."
        )]
        public List<UnboundedStatModifier> StatCaps = new();

        [Header("Stat Bonuses")]
        [Tooltip(
            "Flat bonuses for the fixed non-Movement stat set. Movement is set in Identity."
        )]
        public List<UnboundedStatModifier> StatBonuses = new();

        [Tooltip("Growth rate modifiers (percentage 0-100) for the fixed growth stats. Movement never grows.")]
        public List<UnboundedStatModifier> GrowthRateModifiers = new();

        [Header("Class Change Bonuses")]
        [Tooltip(
            "One-time bonuses for the fixed non-Movement stat set when first changing into this class."
        )]
        public List<UnboundedStatModifier> ClassChangeBonuses = new();

        public ClassStats()
        {
            StatMinimums = CreateStatList();
            StatCaps = CreateStatList();
            StatBonuses = CreateStatList();
            GrowthRateModifiers = CreateStatList();
            GrowthRateModifiers.Add(new UnboundedStatModifier(BoundedStatType.Health, 0f));
            ClassChangeBonuses = CreateStatList();
        }

        private static List<UnboundedStatModifier> CreateStatList() =>
            Enum.GetValues(typeof(UnboundedStatType))
                .Cast<UnboundedStatType>()
                .Where(type => type != UnboundedStatType.Movement)
                .Select(type => new UnboundedStatModifier(type, 0f))
                .ToList();

        /// <summary>
        /// Gets the growth rate modifier for a specific unbounded stat type.
        /// </summary>
        public float GetGrowthRateModifier(UnboundedStatType statType)
        {
            var modifier = GrowthRateModifiers.Find(m => m.unboundedStatType == statType);
            return modifier.value;
        }
    }
}
