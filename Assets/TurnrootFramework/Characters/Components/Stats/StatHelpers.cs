using System;
using System.Collections.Generic;
using Turnroot.Utilities;

namespace Turnroot.Characters.Stats
{
    /// <summary>
    /// Helper methods for working with character stats to reduce code duplication.
    /// </summary>
    public static class StatHelpers
    {
        /// <summary>
        /// Finds a bounded stat by type in a list. If missing, creates a sensible default and adds it to the list when possible.
        /// </summary>
        public static BoundedCharacterStat GetBoundedStat(
            List<BoundedCharacterStat> stats,
            BoundedStatType type
        )
        {
            if (stats == null)
            {
                TurnrootLogger.Log(
                    "StatHelpers.GetBoundedStat: stats list is null",
                    TurnrootLogger.LogLevel.Error
                );
                var (max, current, min) = GetDefaultValuesForBoundedStat(type);
                return new BoundedCharacterStat(max, current, min, type);
            }

            try
            {
                var r = stats.Find(s => s.StatType == type);
                if (r == null)
                {
                    TurnrootLogger.Log(
                        $"StatHelpers.GetBoundedStat: Stat {type} not found, creating default",
                        TurnrootLogger.LogLevel.Warning
                    );
                    var (max, current, min) = GetDefaultValuesForBoundedStat(type);
                    var defaultStat = new BoundedCharacterStat(max, current, min, type);
                    stats.Add(defaultStat);
                    return defaultStat;
                }
                return r;
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"StatHelpers.GetBoundedStat: Exception finding stat {type}: {ex}",
                    TurnrootLogger.LogLevel.Error
                );
                var (max, current, min) = GetDefaultValuesForBoundedStat(type);
                return new BoundedCharacterStat(max, current, min, type);
            }
        }

        /// <summary>
        /// Finds an unbounded stat by type in a list. If missing, creates a sensible default and adds it to the list when possible.
        /// </summary>
        public static CharacterStat GetUnboundedStat(
            List<CharacterStat> stats,
            UnboundedStatType type
        )
        {
            if (stats == null)
            {
                TurnrootLogger.Log(
                    "StatHelpers.GetUnboundedStat: stats list is null",
                    TurnrootLogger.LogLevel.Error
                );
                return new CharacterStat(GetDefaultValueForUnboundedStat(type), type);
            }

            try
            {
                var r = stats.Find(s => s.StatType == type);
                if (r == null)
                {
                    TurnrootLogger.Log(
                        $"StatHelpers.GetUnboundedStat: Stat {type} not found, creating default",
                        TurnrootLogger.LogLevel.Warning
                    );
                    var defaultStat = new CharacterStat(
                        GetDefaultValueForUnboundedStat(type),
                        type
                    );
                    stats.Add(defaultStat);
                    return defaultStat;
                }

                TurnrootLogger.Log(
                    $"StatHelpers.GetUnboundedStat: Found stat {type} with value {r.Current}"
                );
                return r;
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"StatHelpers.GetUnboundedStat: Exception finding stat {type}: {ex}",
                    TurnrootLogger.LogLevel.Error
                );
                return new CharacterStat(GetDefaultValueForUnboundedStat(type), type);
            }
        }

        private static (float max, float current, float min) GetDefaultValuesForBoundedStat(
            BoundedStatType type
        )
        {
            return type switch
            {
                BoundedStatType.Health => (100f, 100f, 0f),
                BoundedStatType.Level => (99f, 1f, 1f),
                BoundedStatType.LevelExperience => (100f, 0f, 0f),
                BoundedStatType.ClassExperience => (100f, 0f, 0f),
                _ => (100f, 100f, 0f),
            };
        }

        private static float GetDefaultValueForUnboundedStat(UnboundedStatType type)
        {
            return type switch
            {
                UnboundedStatType.Luck => 5f,
                UnboundedStatType.CriticalAvoidance => 0f,
                UnboundedStatType.Authority => 5f,
                _ => 10f,
            };
        }

        public static float GetHealthPercentage(List<BoundedCharacterStat> stats)
        {
            var healthStat = GetBoundedStat(stats, BoundedStatType.Health);
            if (healthStat == null)
            {
                return 0f;
            }
            return healthStat.Current / healthStat.Max;
        }
    }
}
