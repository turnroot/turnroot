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
        /// Finds a bounded stat by type in a list.
        /// </summary>
        public static BoundedCharacterStat GetBoundedStat(
            List<BoundedCharacterStat> stats,
            BoundedStatType type
        ) => stats?.Find(s => s.StatType == type);

        /// <summary>
        /// Finds an unbounded stat by type in a list.
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
                var r = stats?.Find(s => s.StatType == type);
                if (r == null)
                {
                    TurnrootLogger.Log(
                        $"StatHelpers.GetUnboundedStat: Stat {type} not found",
                        TurnrootLogger.LogLevel.Warning
                    );
                    return null;
                }
                TurnrootLogger.Log(
                    $"StatHelpers.GetUnboundedStat: Found stat {type} with value {r.Current}"
                );
                return r;
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"StatHelpers.GetUnboundedStat: Exception finding stat {type}: {ex}",
                    TurnrootLogger.LogLevel.Error
                );
                return null;
            }
        }

        public static float GetHealthPercentage(List<BoundedCharacterStat> stats)
        {
            var healthStat = GetBoundedStat(stats, BoundedStatType.Health);
            return (healthStat?.Current / healthStat?.Max) ?? 0;
        }
    }
}
