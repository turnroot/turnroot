using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;

namespace Turnroot.Characters.Stats
{
    /// <summary>
    /// Utility class providing helper methods for creating, retrieving, and managing character stats.
    /// </summary>
    public static class StatHelpers
    {
        #region Bounded Stats
        public static BoundedCharacterStat GetBoundedStat(
            List<BoundedCharacterStat> stats,
            BoundedStatType type
        )
        {
            CleanupDuplicateBounded(stats);
            return GetStat(stats, type, s => s.StatType);
        }

        public static BoundedCharacterStat GetOrCreateBoundedStat(
            List<BoundedCharacterStat> stats,
            BoundedStatType type
        )
        {
            CleanupDuplicateBounded(stats);
            return GetOrCreateBoundedStat(stats, type, null);
        }

        public static BoundedCharacterStat GetOrCreateBoundedStat(
            List<BoundedCharacterStat> stats,
            BoundedStatType type,
            string templateId
        )
        {
            if (!ValidationHelper.ValidateNotNull(stats, nameof(stats)))
            {
                return null;
            }

            CleanupDuplicateBounded(stats);

            var existing = stats.Find(s => s?.StatType == type);
            if (existing != null)
            {
                return existing;
            }

            var (max, current, min) = TryLoadBoundedFromLtm(type, templateId, out var loaded)
                ? loaded
                : GetDefaultBoundedValues(type);

            var stat = new BoundedCharacterStat(max, current, min, type);
            stats.Add(stat);
            SaveBoundedToLtm(type, stat, templateId);
            return stat;
        }

        internal static (
            float max,
            float current,
            float min
        ) GetDefaultValuesForBoundedStatInternal(BoundedStatType type, string templateId = null) =>
            TryLoadBoundedFromLtm(type, templateId, out var loaded)
                ? loaded
                : GetDefaultBoundedValues(type);

        private static (float max, float current, float min) GetDefaultBoundedValues(
            BoundedStatType type
        ) =>
            type switch
            {
                BoundedStatType.Health => (100f, 100f, 0f),
                BoundedStatType.Level => (99f, 1f, 1f),
                BoundedStatType.LevelExperience => (100f, 0f, 0f),
                BoundedStatType.ClassExperience => (100f, 0f, 0f),
                _ => (100f, 100f, 0f),
            };
        #endregion

        #region Unbounded Stats


        // ensure a bounded stats list has no duplicate types
        private static void CleanupDuplicateBounded(List<BoundedCharacterStat> stats)
        {
            if (!ValidationHelper.ValidateNotNull(stats, nameof(stats)))
            {
                return;
            }

            var seen = new HashSet<BoundedStatType>();
            for (int i = stats.Count - 1; i >= 0; i--)
            {
                var s = stats[i];
                if (s == null)
                {
                    continue;
                }

                if (seen.Contains(s.StatType))
                {
                    stats.RemoveAt(i);
                }
                else
                {
                    seen.Add(s.StatType);
                }
            }
        }

        public static CharacterStat GetUnboundedStat(
            List<CharacterStat> stats,
            UnboundedStatType type
        )
        {
            return GetStat(stats, type, s => s.StatType);
        }

        public static CharacterStat GetOrCreateUnboundedStat(
            List<CharacterStat> stats,
            UnboundedStatType type
        )
        {
            return GetOrCreateUnboundedStat(stats, type, null);
        }

        public static CharacterStat GetOrCreateUnboundedStat(
            List<CharacterStat> stats,
            UnboundedStatType type,
            string templateId
        )
        {
            if (stats == null)
            {
                return null;
            }

            var existing = stats.Find(s => s?.StatType == type);
            if (existing != null)
            {
                return existing;
            }

            var value = TryLoadUnboundedFromLtm(type, templateId, out var loaded)
                ? loaded
                : GetDefaultUnboundedValue(type);

            var stat = new CharacterStat(value, type);
            stats.Add(stat);
            SaveUnboundedToLtm(type, stat, templateId);
            return stat;
        }

        internal static float GetDefaultValueForUnboundedStatInternal(UnboundedStatType type) =>
            GetDefaultUnboundedValue(type);

        internal static bool TryGetUnboundedDefaultValueInternal(
            UnboundedStatType type,
            string templateId,
            out float value
        ) => TryLoadUnboundedFromLtm(type, templateId, out value);

        private static float GetDefaultUnboundedValue(UnboundedStatType type) =>
            type switch
            {
                UnboundedStatType.Luck => 5f,
                UnboundedStatType.CriticalAvoidance => 0f,
                UnboundedStatType.Authority => 5f,
                UnboundedStatType.Movement => 5f,
                _ => 10f,
            };
        #endregion

        #region Shared Helpers
        private static TStat GetStat<TStat, TEnum>(
            List<TStat> stats,
            TEnum type,
            Func<TStat, TEnum> getType
        )
            where TStat : class
            where TEnum : Enum => stats?.Find(s => s != null && getType(s).Equals(type));

        public static float GetHealthPercentage(List<BoundedCharacterStat> stats)
        {
            var health = GetBoundedStat(stats, BoundedStatType.Health);
            return health != null ? health.Current / health.Max : 0f;
        }
        #endregion

        #region LTM Persistence
        private class StatDto
        {
            public float max,
                current,
                min;
        }

        private static Brain GetBrain() => UnityEngine.Object.FindFirstObjectByType<Brain>();

        private static bool TryLoadBoundedFromLtm(
            BoundedStatType type,
            string templateId,
            out (float max, float current, float min) values
        )
        {
            values = default;
            if (string.IsNullOrEmpty(templateId))
            {
                return false;
            }

            var cb = GetBrain()?.charactersBrain;
            return cb?.TryGetTemplateBoundedDefault(templateId, type, out values) ?? false;
        }

        private static void SaveBoundedToLtm(
            BoundedStatType type,
            BoundedCharacterStat stat,
            string templateId
        )
        {
            if (string.IsNullOrEmpty(templateId))
            {
                return;
            }

            GetBrain()
                ?.charactersBrain.SaveTemplateBoundedDefault(
                    templateId,
                    type,
                    (stat.Max, stat.Current, stat.Min)
                );
        }

        private static bool TryLoadUnboundedFromLtm(
            UnboundedStatType type,
            string templateId,
            out float value
        )
        {
            value = 0f;
            if (string.IsNullOrEmpty(templateId))
            {
                return false;
            }

            var cb = GetBrain()?.charactersBrain;
            return cb?.TryGetTemplateUnboundedDefault(templateId, type, out value) ?? false;
        }

        private static void SaveUnboundedToLtm(
            UnboundedStatType type,
            CharacterStat stat,
            string templateId
        )
        {
            if (string.IsNullOrEmpty(templateId))
            {
                return;
            }

            GetBrain()
                ?.charactersBrain.SaveTemplateUnboundedDefault(templateId, type, stat.Current);
        }
        #endregion
    }
}
