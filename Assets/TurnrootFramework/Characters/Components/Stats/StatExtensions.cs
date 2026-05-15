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
        /// Apply stat modifiers to a character's stats. Handles both bounded (HP, via isBounded) and unbounded stats.
        /// </summary>
        public static void ApplyStatModifiers(
            this IHasStats stats,
            IEnumerable<CharacterClass.UnboundedStatModifier> modifiers,
            Func<BoundedCharacterStat, float, float> boundedModifier,
            Func<CharacterStat, float, float> unboundedModifier
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
                    if (mod.isBounded)
                    {
                        if (boundedModifier == null)
                        {
                            continue;
                        }

                        var stat = stats.GetBoundedStat(mod.boundedStatType);
                        if (stat != null)
                        {
                            float newValue = boundedModifier(stat, mod.value);
                            stat.SetCurrent(newValue);
                        }
                    }
                    else
                    {
                        if (unboundedModifier == null)
                        {
                            continue;
                        }

                        var stat = stats.GetUnboundedStat(mod.unboundedStatType);
                        if (stat != null)
                        {
                            float newValue = unboundedModifier(stat, mod.value);
                            stat.SetCurrent(newValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply a value change to all stats using the bonus field.
        /// Handles both bounded (HP, via isBounded) and unbounded stats.
        /// </summary>
        public static void ApplyStatBonuses(
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
                    if (mod.isBounded)
                    {
                        var stat = stats.GetBoundedStat(mod.boundedStatType);
                        if (stat != null)
                        {
                            stat.SetBonus(stat.Bonus + mod.value);
                        }
                    }
                    else
                    {
                        var stat = stats.GetUnboundedStat(mod.unboundedStatType);
                        if (stat != null)
                        {
                            stat.SetBonus(stat.Bonus + mod.value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove a value change from all stats using the bonus field.
        /// Handles both bounded (HP, via isBounded) and unbounded stats.
        /// </summary>
        public static void RemoveStatBonuses(
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
                    if (mod.isBounded)
                    {
                        var stat = stats.GetBoundedStat(mod.boundedStatType);
                        if (stat != null)
                        {
                            stat.SetBonus(stat.Bonus - mod.value);
                        }
                    }
                    else
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
}
