using System;
using System.Collections.Generic;

namespace Turnroot.Characters.Stats
{
    /// <summary>
    /// Extension methods for working with stats to eliminate repetitive iteration patterns.
    /// </summary>
    public static class StatExtensions
    {
        /// <summary>
        /// Execute an action on all bounded stats in the collection.
        /// </summary>
        public static void ForEachBoundedStat(
            this IHasStats stats,
            Action<BoundedCharacterStat> action
        )
        {
            if (stats?.BoundedStats == null || action == null)
            {
                return;
            }

            foreach (var stat in stats.BoundedStats)
            {
                if (stat != null)
                {
                    action(stat);
                }
            }
        }

        /// <summary>
        /// Execute an action on all unbounded stats in the collection.
        /// </summary>
        public static void ForEachUnboundedStat(this IHasStats stats, Action<CharacterStat> action)
        {
            if (stats?.UnboundedStats == null || action == null)
            {
                return;
            }

            foreach (var stat in stats.UnboundedStats)
            {
                if (stat != null)
                {
                    action(stat);
                }
            }
        }

        /// <summary>
        /// Execute an action on all stats (bounded and unbounded) as BaseCharacterStat.
        /// Useful for operations that work on the common base class.
        /// </summary>
        public static void ForEachStat(this IHasStats stats, Action<BaseCharacterStat> action)
        {
            if (stats == null || action == null)
            {
                return;
            }

            stats.ForEachBoundedStat(action);
            stats.ForEachUnboundedStat(action);
        }

        /// <summary>
        /// Apply bounded stat modifiers to a character's stats.
        /// </summary>
        public static void ApplyBoundedModifiers(
            this IHasStats stats,
            IEnumerable<CharacterClass.StatModifier> modifiers,
            Func<BoundedCharacterStat, float, float> modifier
        )
        {
            if (stats == null || modifiers == null || modifier == null)
            {
                return;
            }

            foreach (var mod in modifiers)
            {
                if (mod.value != 0)
                {
                    var stat = stats.GetBoundedStat(mod.boundedStatType);
                    if (stat != null)
                    {
                        float newValue = modifier(stat, mod.value);
                        stat.SetCurrent(newValue);
                    }
                }
            }
        }

        /// <summary>
        /// Apply unbounded stat modifiers to a character's stats.
        /// </summary>
        public static void ApplyUnboundedModifiers(
            this IHasStats stats,
            IEnumerable<CharacterClass.UnboundedStatModifier> modifiers,
            Func<CharacterStat, float, float> modifier
        )
        {
            if (stats == null || modifiers == null || modifier == null)
            {
                return;
            }

            foreach (var mod in modifiers)
            {
                if (mod.value != 0)
                {
                    var stat = stats.GetUnboundedStat(mod.unboundedStatType);
                    if (stat != null)
                    {
                        float newValue = modifier(stat, mod.value);
                        stat.SetCurrent(newValue);
                    }
                }
            }
        }

        /// <summary>
        /// Apply a value change to all bounded stats using bonus field.
        /// </summary>
        public static void ApplyBoundedBonuses(
            this IHasStats stats,
            IEnumerable<CharacterClass.StatModifier> modifiers
        )
        {
            if (stats == null || modifiers == null)
            {
                return;
            }

            foreach (var mod in modifiers)
            {
                if (mod.value != 0)
                {
                    var stat = stats.GetBoundedStat(mod.boundedStatType);
                    if (stat != null)
                    {
                        stat.SetBonus(stat.Bonus + mod.value);
                    }
                }
            }
        }

        /// <summary>
        /// Apply a value change to all unbounded stats using bonus field.
        /// </summary>
        public static void ApplyUnboundedBonuses(
            this IHasStats stats,
            IEnumerable<CharacterClass.UnboundedStatModifier> modifiers
        )
        {
            if (stats == null || modifiers == null)
            {
                return;
            }

            foreach (var mod in modifiers)
            {
                if (mod.value != 0)
                {
                    var stat = stats.GetUnboundedStat(mod.unboundedStatType);
                    if (stat != null)
                    {
                        stat.SetBonus(stat.Bonus + mod.value);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a value change from all bounded stats using bonus field.
        /// </summary>
        public static void RemoveBoundedBonuses(
            this IHasStats stats,
            IEnumerable<CharacterClass.StatModifier> modifiers
        )
        {
            if (stats == null || modifiers == null)
            {
                return;
            }

            foreach (var mod in modifiers)
            {
                if (mod.value != 0)
                {
                    var stat = stats.GetBoundedStat(mod.boundedStatType);
                    if (stat != null)
                    {
                        stat.SetBonus(stat.Bonus - mod.value);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a value change from all unbounded stats using bonus field.
        /// </summary>
        public static void RemoveUnboundedBonuses(
            this IHasStats stats,
            IEnumerable<CharacterClass.UnboundedStatModifier> modifiers
        )
        {
            if (stats == null || modifiers == null)
            {
                return;
            }

            foreach (var mod in modifiers)
            {
                if (mod.value != 0)
                {
                    var stat = stats.GetUnboundedStat(mod.unboundedStatType);
                    if (stat != null)
                    {
                        stat.SetBonus(stat.Bonus - mod.value);
                    }
                }
            }
        }
    }
}
