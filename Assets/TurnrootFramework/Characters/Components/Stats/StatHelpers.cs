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
        /// Finds a bounded stat by type in a list. Returns null if not found.
        /// Use GetOrCreateBoundedStat if you want auto-creation behavior.
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
                return null;
            }
            try
            {
                var r = stats.Find(s => s != null && s.StatType == type);
                return r;
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"StatHelpers.GetBoundedStat: Exception finding stat {type}: {ex}",
                    TurnrootLogger.LogLevel.Error
                );
                return null;
            }
        }

        /// <summary>
        /// Gets or creates a bounded stat. Use this ONLY for deserialization repair, not for normal operation.
        /// </summary>
        public static BoundedCharacterStat GetOrCreateBoundedStat(
            List<BoundedCharacterStat> stats,
            BoundedStatType type
        )
        {
            if (stats == null)
            {
                TurnrootLogger.Log(
                    "StatHelpers.GetOrCreateBoundedStat: stats list is null, cannot create",
                    TurnrootLogger.LogLevel.Error
                );
                return null;
            }

            var existing = stats.Find(s => s != null && s.StatType == type);
            if (existing != null)
            {
                return existing;
            }

            TurnrootLogger.Log(
                $"StatHelpers.GetOrCreateBoundedStat: Creating default stat for {type}",
                TurnrootLogger.LogLevel.Warning
            );
            var (max, current, min) = GetDefaultValuesForBoundedStat(type);
            var defaultStat = new BoundedCharacterStat(max, current, min, type);
            stats.Add(defaultStat);
            return defaultStat;
        }

        /// <summary>
        /// Finds an unbounded stat by type in a list. Returns null if not found.
        /// Use GetOrCreateUnboundedStat if you want auto-creation behavior.
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
                return null;
            }
            try
            {
                var r = stats.Find(s => s != null && s.StatType == type);
                return r;
            }
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"StatHelpers.GetUnboundedStat: Exception finding stat {type}: {ex}",
                    TurnrootLogger.LogLevel.Error
                );
                return null;
            }
        }

        /// <summary>
        /// Gets or creates an unbounded stat. Use this ONLY for deserialization repair, not for normal operation.
        /// </summary>
        public static CharacterStat GetOrCreateUnboundedStat(
            List<CharacterStat> stats,
            UnboundedStatType type
        )
        {
            if (stats == null)
            {
                TurnrootLogger.Log(
                    "StatHelpers.GetOrCreateUnboundedStat: stats list is null, cannot create",
                    TurnrootLogger.LogLevel.Error
                );
                return null;
            }

            var existing = stats.Find(s => s != null && s.StatType == type);
            if (existing != null)
            {
                return existing;
            }

            TurnrootLogger.Log(
                $"StatHelpers.GetOrCreateUnboundedStat: Creating default stat for {type}",
                TurnrootLogger.LogLevel.Warning
            );
            var defaultStat = new CharacterStat(GetDefaultValueForUnboundedStat(type), type);
            stats.Add(defaultStat);
            return defaultStat;
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
                UnboundedStatType.Movement => 5f, // Default movement
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
