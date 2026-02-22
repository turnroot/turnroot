using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.Stats;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Runtime instance of a character containing all state and behavior.
    /// This partial class contains stat validation, repair, and access methods.
    /// </summary>
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
    {
        #region Stat Validation
        private OperationResult ValidateRuntimeStatsComplete()
        {
            var res = ValidateStatsFor(_runtimeBoundedStats, s => s.StatType, "bounded");
            return res.Success
                ? ValidateStatsFor(_runtimeUnboundedStats, s => s.StatType, "unbounded")
                : res;
        }

        private OperationResult ValidateStatsFor<TEnum, TStat>(
            IEnumerable<TStat> runtimeStats,
            Func<TStat, TEnum> getStatType,
            string statKind
        )
            where TEnum : Enum
        {
            var required = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToHashSet();
            var existing = new HashSet<TEnum>();

            foreach (var stat in runtimeStats)
            {
                if (stat == null)
                {
                    return OperationResult.Failure(
                        $"CharacterInstance.ValidateRuntimeStatsComplete: null {statKind} stat found for {Id}"
                    );
                }

                var type = getStatType(stat);
                if (!existing.Add(type))
                {
                    return OperationResult.Failure(
                        $"CharacterInstance.ValidateRuntimeStatsComplete: duplicate {statKind} stat {type} for {Id}"
                    );
                }
            }

            var missing = required.Except(existing).ToList();
            return missing.Any()
                ? OperationResult.Failure(
                    $"CharacterInstance.ValidateRuntimeStatsComplete: missing {statKind} stats {string.Join(", ", missing)} for {Id}"
                )
                : OperationResult.Successful();
        }

        private void RepairMissingStats()
        {
            bool anyRepaired = false;

            var templateId = _characterTemplate?.FullName;

            RepairStatsFor(
                Enum.GetValues(typeof(BoundedStatType)).Cast<BoundedStatType>(),
                _runtimeBoundedStats,
                (list, type) => StatHelpers.GetBoundedStat(list, type),
                (list, type) => StatHelpers.GetOrCreateBoundedStat(list, type, templateId),
                ref anyRepaired
            );

            RepairStatsFor(
                Enum.GetValues(typeof(UnboundedStatType)).Cast<UnboundedStatType>(),
                _runtimeUnboundedStats,
                (list, type) => StatHelpers.GetUnboundedStat(list, type),
                (list, type) => StatHelpers.GetOrCreateUnboundedStat(list, type, templateId),
                ref anyRepaired
            );
        }

        private static void RepairStatsFor<TEnum, TStat>(
            IEnumerable<TEnum> enumValues,
            List<TStat> list,
            Func<List<TStat>, TEnum, TStat> getFunc,
            Action<List<TStat>, TEnum> getOrCreateFunc,
            ref bool anyRepaired
        )
            where TEnum : Enum
        {
            foreach (var type in enumValues)
            {
                if (getFunc(list, type) == null)
                {
                    getOrCreateFunc(list, type);
                    anyRepaired = true;
                }
            }
        }

#if UNITY_EDITOR
        private void ValidateStatsComplete()
        {
            bool hasErrors = Enum.GetValues(typeof(BoundedStatType))
                .Cast<BoundedStatType>()
                .Any(type => StatHelpers.GetBoundedStat(_runtimeBoundedStats, type) == null);

            hasErrors |= Enum.GetValues(typeof(UnboundedStatType))
                .Cast<UnboundedStatType>()
                .Any(type => StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, type) == null);

            if (hasErrors)
            {
                $"Character {Id} has missing stats - this will cause runtime errors! Use GameplayGeneralSettings (default stats) or manually add missing stats to the template.".LogError();
            }
        }
#endif
        #endregion


        #region Stat Access
        public void SetRenderer(SkinnedMeshRenderer renderer) => _meshRenderer = renderer;

        public BoundedCharacterStat GetBoundedStat(BoundedStatType type) =>
            StatHelpers.GetBoundedStat(_runtimeBoundedStats, type);

        public CharacterStat GetUnboundedStat(UnboundedStatType type) =>
            StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, type);

        public float GetHealthPercentage() => StatHelpers.GetHealthPercentage(this.BoundedStats);
        #endregion
    }
}

