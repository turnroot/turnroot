using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
    {
        private CharacterInstanceStatsDto BuildRuntimeDto()
        {
            // make sure we aren't serializing duplicate entries
            DeduplicateRuntimeStats();
            var unboundedStats = _runtimeUnboundedStats
                .Where(s => s != null && Enum.IsDefined(typeof(UnboundedStatType), s.StatType))
                .ToList();

            var runtimeDto = new CharacterInstanceStatsDto
            {
                BoundedStats = new BoundedStatDto[_runtimeBoundedStats.Count],
                UnboundedStats = new UnboundedStatDto[unboundedStats.Count],
            };

            for (int i = 0; i < _runtimeBoundedStats.Count; i++)
            {
                var s = _runtimeBoundedStats[i];
                runtimeDto.BoundedStats[i] = new BoundedStatDto
                {
                    StatType = s.StatType.ToString(),
                    Max = s.Max,
                    Current = s.Current,
                    Min = s.Min,
                };
            }

            for (int i = 0; i < unboundedStats.Count; i++)
            {
                var s = unboundedStats[i];
                runtimeDto.UnboundedStats[i] = new UnboundedStatDto
                {
                    StatType = s.StatType.ToString(),
                    Current = s.Current,
                };
            }

            return runtimeDto;
        }

        private void HandleNoExistingEntry(
            LongTermMemory ltm,
            string key,
            CharacterInstanceStatsDto runtimeDto,
            Brain brain
        )
        {
            if (_characterTemplate?.IsUnique == true)
            {
                var toSave = brain.EncodeString(JsonUtility.ToJson(runtimeDto));
                ltm.Remember(key, toSave);
                return;
            }

            var templateId = _characterTemplate?.FullName;
            foreach (var bs in _runtimeBoundedStats)
            {
                StatHelpers.GetOrCreateBoundedStat(_runtimeBoundedStats, bs.StatType, templateId);
            }
            foreach (var us in _runtimeUnboundedStats)
            {
                StatHelpers.GetOrCreateUnboundedStat(
                    _runtimeUnboundedStats,
                    us.StatType,
                    templateId
                );
            }
        }

        private CharacterInstanceStatsDto MergeWithRequiredStats(
            CharacterInstanceStatsDto existingDto,
            CharacterInstanceStatsDto runtimeDto,
            out bool changed
        )
        {
            changed = false;

            // Merge Bounded
            var existingBounded = new HashSet<string>();
            if (existingDto.BoundedStats != null)
            {
                foreach (var b in existingDto.BoundedStats)
                {
                    if (b != null)
                    {
                        existingBounded.Add(b.StatType);
                    }
                }
            }

            var requiredBounded = Enum.GetValues(typeof(BoundedStatType));
            var newBoundedList = new List<BoundedStatDto>(
                existingDto.BoundedStats ?? Array.Empty<BoundedStatDto>()
            );
            foreach (BoundedStatType t in requiredBounded)
            {
                var tname = t.ToString();
                if (!existingBounded.Contains(tname))
                {
                    var runtime = _runtimeBoundedStats.Find(s => s.StatType == t);
                    if (runtime != null)
                    {
                        newBoundedList.Add(
                            new BoundedStatDto
                            {
                                StatType = tname,
                                Max = runtime.Max,
                                Current = runtime.Current,
                                Min = runtime.Min,
                            }
                        );
                    }
                    else
                    {
                        var templateId = _characterTemplate?.FullName;
                        var defaultValues = StatHelpers.GetDefaultValuesForBoundedStatInternal(
                            t,
                            templateId
                        );
                        newBoundedList.Add(
                            new BoundedStatDto
                            {
                                StatType = tname,
                                Max = defaultValues.max,
                                Current = defaultValues.current,
                                Min = defaultValues.min,
                            }
                        );
                    }
                    changed = true;
                }
            }

            // Merge Unbounded
            var requiredUnbounded = Enum
                .GetValues(typeof(UnboundedStatType))
                .Cast<UnboundedStatType>();
            var newUnboundedList = new List<UnboundedStatDto>();
            var existingUnbounded = new HashSet<string>();
            if (existingDto.UnboundedStats != null)
            {
                foreach (var u in existingDto.UnboundedStats)
                {
                    if (
                        u != null
                        && Enum.TryParse<UnboundedStatType>(u.StatType, out var type)
                        && Enum.IsDefined(typeof(UnboundedStatType), type)
                        && existingUnbounded.Add(u.StatType)
                    )
                    {
                        newUnboundedList.Add(u);
                    }
                    else
                    {
                        changed = true;
                    }
                }
            }

            foreach (UnboundedStatType t in requiredUnbounded)
            {
                var tname = t.ToString();
                if (!existingUnbounded.Contains(tname))
                {
                    var runtime = _runtimeUnboundedStats.Find(s => s.StatType == t);
                    if (runtime != null)
                    {
                        newUnboundedList.Add(
                            new UnboundedStatDto { StatType = tname, Current = runtime.Current }
                        );
                    }
                    else
                    {
                        var templateId = _characterTemplate?.FullName;
                        if (
                            StatHelpers.TryGetUnboundedDefaultValueInternal(
                                t,
                                templateId,
                                out var def
                            )
                        )
                        {
                            newUnboundedList.Add(
                                new UnboundedStatDto { StatType = tname, Current = def }
                            );
                        }
                        else
                        {
                            newUnboundedList.Add(
                                new UnboundedStatDto
                                {
                                    StatType = tname,
                                    Current = StatHelpers.GetDefaultValueForUnboundedStatInternal(
                                        t
                                    ),
                                }
                            );
                        }
                    }
                    changed = true;
                }
            }

            return new CharacterInstanceStatsDto
            {
                BoundedStats = newBoundedList.ToArray(),
                UnboundedStats = newUnboundedList.ToArray(),
            };
        }

        private void EnsureRuntimeContains(CharacterInstanceStatsDto existingDto)
        {
            if (existingDto.BoundedStats != null)
            {
                foreach (var b in existingDto.BoundedStats)
                {
                    if (Enum.TryParse<BoundedStatType>(b.StatType, out var st))
                    {
                        if (StatHelpers.GetBoundedStat(_runtimeBoundedStats, st) == null)
                        {
                            _runtimeBoundedStats.Add(
                                new BoundedCharacterStat(b.Max, b.Current, b.Min, st)
                            );
                        }
                    }
                }
            }

            if (existingDto.UnboundedStats != null)
            {
                foreach (var u in existingDto.UnboundedStats)
                {
                    if (Enum.TryParse<UnboundedStatType>(u.StatType, out var ut))
                    {
                        if (!Enum.IsDefined(typeof(UnboundedStatType), ut))
                        {
                            continue;
                        }

                        if (StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, ut) == null)
                        {
                            _runtimeUnboundedStats.Add(new CharacterStat(u.Current, ut));
                        }
                    }
                }
            }
        }

        private void OnBrainLtmKeyCacheUpdated(int version)
        {
            try
            {
                // Retry persisting/merging now that the LTM key cache was updated
                EnsurePersistedInLtm();
            }
            finally
            {
                try
                {
                    CurrentBrain.OnLtmKeyCacheUpdated -= OnBrainLtmKeyCacheUpdated;
                }
                catch (Exception ex)
                {
                    "OnBrainLtmKeyCacheUpdated cleanup failed: ".LogWarning();
                    ex.Message.LogWarning();
                }
                _deferredPersistRegistered = false;
            }
        }

        // ------------------------------------------------------------
        // utility used by several persistence helpers above
        // ------------------------------------------------------------
        private void DeduplicateRuntimeStats()
        {
            if (_runtimeBoundedStats == null || _runtimeUnboundedStats == null)
            {
                return;
            }

            bool warned = false;

            _runtimeBoundedStats = _runtimeBoundedStats
                .GroupBy(s => s.StatType)
                .Select(g =>
                {
                    if (g.Count() > 1 && !warned)
                    {
                        $"CharacterInstance {Id}: removing {g.Count() - 1} duplicated bounded stat entries of type {g.Key}".LogWarning();
                        warned = true;
                    }
                    return g.First();
                })
                .ToList();

            warned = false;
            _runtimeUnboundedStats = _runtimeUnboundedStats
                .Where(s => s != null && Enum.IsDefined(typeof(UnboundedStatType), s.StatType))
                .ToList();
            _runtimeUnboundedStats = _runtimeUnboundedStats
                .GroupBy(s => s.StatType)
                .Select(g =>
                {
                    if (g.Count() > 1 && !warned)
                    {
                        $"CharacterInstance {Id}: removing {g.Count() - 1} duplicated unbounded stat entries of type {g.Key}".LogWarning();
                        warned = true;
                    }
                    return g.First();
                })
                .ToList();
        }

        /// <summary>
        /// Persists current runtime stats to LongTermMemory.
        /// Call this after modifying stats to ensure changes are saved.
        /// Only writes for unique characters; non-unique characters derive stats from their template.
        /// </summary>
        public void PersistStatsToLtm() => SaveCurrentStatsToLtm();
    }
}
