using System;
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using UnityEngine;
using UnityEngine.Serialization;

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
            "Minimum stat values this class enforces (0 = no minimum). Supports HP (isBounded) and unbounded stats."
        )]
        [FormerlySerializedAs("UnboundedStatMinimums")]
        public List<UnboundedStatModifier> StatMinimums = new();

        [Header("Stat Caps")]
        [Tooltip(
            "Maximum stat caps this class imposes (0 = no cap). Supports HP (isBounded) and unbounded stats."
        )]
        [FormerlySerializedAs("UnboundedStatCaps")]
        public List<UnboundedStatModifier> StatCaps = new();

        [Header("Stat Bonuses")]
        [Tooltip(
            "Flat stat bonuses applied when equipping/occupying this class. Supports HP (isBounded) and unbounded stats."
        )]
        [FormerlySerializedAs("UnboundedStatBonuses")]
        public List<UnboundedStatModifier> StatBonuses = new();

        [Tooltip("Growth rate modifiers (percentage 0-100) for stat increases on level up")]
        public List<UnboundedStatModifier> GrowthRateModifiers = new();

        [Header("Class Change Bonuses")]
        [Tooltip(
            "One-time stat bonuses applied when a character first changes into this class. Supports HP (isBounded) and unbounded stats."
        )]
        [FormerlySerializedAs("UnboundedClassChangeBonuses")]
        public List<UnboundedStatModifier> ClassChangeBonuses = new();

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
