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
    /// <summary>
    /// Runtime instance of a character containing all state and behavior.
    /// This partial class contains stat persistence and long-term memory integration.
    /// </summary>
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
    {
        #region Stat Persistence

        // DTOs for serializing stats into LTM
        /// <summary>
        /// Data transfer object for serializing bounded character stats to long-term memory.
        /// </summary>
        [Serializable]
        private class BoundedStatDto
        {
            public string StatType;
            public float Max;
            public float Current;
            public float Min;
        }

        /// <summary>
        /// Data transfer object for serializing unbounded character stats to long-term memory.
        /// </summary>
        [Serializable]
        private class UnboundedStatDto
        {
            public string StatType;
            public float Current;
        }

        /// <summary>
        /// Data transfer object containing all character stats for serialization to long-term memory.
        /// </summary>
        [Serializable]
        private class CharacterInstanceStatsDto
        {
            public BoundedStatDto[] BoundedStats;
            public UnboundedStatDto[] UnboundedStats;
        }

        [NonSerialized]
        private bool _deferredPersistRegistered = false;

        private void EnsurePersistedInLtm()
        {
            try
            {
                var brain = UnityEngine.Object.FindFirstObjectByType<Brain>();
                var ltm = brain?.ltm;
                if (ltm == null)
                {
                    return;
                }

                if (!IsLtmReady(ltm, brain))
                {
                    return;
                }

                // occasionally stats collections can end up with duplicate entries
                DeduplicateRuntimeStats();

                string key = $"CharacterInstance/{Id}/Stats";
                var json = ltm.Recall(key);

                var runtimeDto = BuildRuntimeDto();

                if (string.IsNullOrEmpty(json))
                {
                    HandleNoExistingEntry(ltm, key, runtimeDto, brain);
                    return;
                }

                json = brain.DecodeString(json);
                if (string.IsNullOrEmpty(json))
                {
                    HandleNoExistingEntry(ltm, key, runtimeDto, brain);
                    return;
                }

                var existingDto = JsonUtility.FromJson<CharacterInstanceStatsDto>(json);
                if (existingDto == null)
                {
                    // Nothing valid in LTM; initialize from runtime
                    ltm.Remember(key, brain.EncodeString(JsonUtility.ToJson(runtimeDto)));
                    existingDto = runtimeDto;
                }

                var mergedDto = MergeWithRequiredStats(existingDto, runtimeDto, out var changed);
                if (changed)
                {
                    ltm.Remember(key, brain.EncodeString(JsonUtility.ToJson(mergedDto)));
                }

                EnsureRuntimeContains(mergedDto);
            }
            catch (Exception ex)
            {
                $"CharacterInstance.EnsurePersistedInLtm: failed to persist/merge stats for {Id}: {ex.Message}".LogWarning();
            }
        }

        private bool IsLtmReady(LongTermMemory ltm, Brain brain)
        {
            var existingDefaultKeys = ltm.RecallKeysByPrefix("DefaultStat");
            if (
                (existingDefaultKeys == null || existingDefaultKeys.Count == 0)
                && !_deferredPersistRegistered
            )
            {
                brain.OnLtmKeyCacheUpdated += OnBrainLtmKeyCacheUpdated;
                _deferredPersistRegistered = true;
                return false;
            }
            return true;
        }

        private CharacterInstanceStatsDto BuildRuntimeDto()
        {
            // make sure we aren't serializing duplicate entries
            DeduplicateRuntimeStats();

            var runtimeDto = new CharacterInstanceStatsDto
            {
                BoundedStats = new BoundedStatDto[_runtimeBoundedStats.Count],
                UnboundedStats = new UnboundedStatDto[_runtimeUnboundedStats.Count],
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

            for (int i = 0; i < _runtimeUnboundedStats.Count; i++)
            {
                var s = _runtimeUnboundedStats[i];
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
            var existingUnbounded = new HashSet<string>();
            if (existingDto.UnboundedStats != null)
            {
                foreach (var u in existingDto.UnboundedStats)
                {
                    if (u != null)
                    {
                        existingUnbounded.Add(u.StatType);
                    }
                }
            }

            var requiredUnbounded = Enum.GetValues(typeof(UnboundedStatType));
            var newUnboundedList = new List<UnboundedStatDto>(
                existingDto.UnboundedStats ?? Array.Empty<UnboundedStatDto>()
            );
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
                    var brain = UnityEngine.Object.FindFirstObjectByType<Brain>();
                    if (brain != null)
                    {
                        brain.OnLtmKeyCacheUpdated -= OnBrainLtmKeyCacheUpdated;
                    }
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

        /// <summary>
        /// Unconditionally writes the current runtime stat values to LTM.
        /// Safe to call at any time; silently skips if LTM isn't initialised yet.
        /// </summary>
        private void SaveCurrentStatsToLtm()
        {
            if (_characterTemplate?.IsUnique != true)
            {
                return;
            }

            try
            {
                var brain = UnityEngine.Object.FindFirstObjectByType<Brain>();
                var ltm = brain?.GetComponent<LongTermMemory>();
                if (ltm == null || !ltm.Initialized)
                {
                    return;
                }

                DeduplicateRuntimeStats();
                var dto = BuildRuntimeDto();
                string key = $"CharacterInstance/{Id}/Stats";
                ltm.Remember(key, brain.EncodeString(JsonUtility.ToJson(dto)));
            }
            catch (Exception ex)
            {
                $"CharacterInstance.SaveCurrentStatsToLtm: failed for {Id}: {ex.Message}".LogWarning();
            }
        }

        /// <summary>
        /// Reads the runtime stat values saved in LTM and applies them to the current instance,
        /// overwriting values that came from the template.  Should be called after
        /// <see cref="Initialize"/> and class-restore during <see cref="OnAfterDeserialize"/>.
        /// Defers gracefully if LTM is not yet initialised.
        /// </summary>
        private void RestoreStatsFromLtm()
        {
            if (_characterTemplate?.IsUnique != true)
            {
                return;
            }

            try
            {
                var brain = UnityEngine.Object.FindFirstObjectByType<Brain>();
                var ltm = brain?.GetComponent<LongTermMemory>();

                if (ltm == null || !ltm.Initialized)
                {
                    if (brain != null && !_deferredPersistRegistered)
                    {
                        brain.OnLtmKeyCacheUpdated += OnBrainLtmStatRestoreDeferred;
                        _deferredPersistRegistered = true;
                    }
                    return;
                }

                string key = $"CharacterInstance/{Id}/Stats";
                var json = ltm.Recall(key);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                json = brain.DecodeString(json);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                var dto = JsonUtility.FromJson<CharacterInstanceStatsDto>(json);
                if (dto != null)
                {
                    ApplyStatDtoToRuntime(dto);
                }
            }
            catch (Exception ex)
            {
                $"CharacterInstance.RestoreStatsFromLtm: failed for {Id}: {ex.Message}".LogWarning();
            }
        }

        private void OnBrainLtmStatRestoreDeferred(int version)
        {
            try
            {
                RestoreStatsFromLtm();
            }
            finally
            {
                try
                {
                    var brain = UnityEngine.Object.FindFirstObjectByType<Brain>();
                    if (brain != null)
                    {
                        brain.OnLtmKeyCacheUpdated -= OnBrainLtmStatRestoreDeferred;
                    }
                }
                catch (Exception ex)
                {
                    $"OnBrainLtmStatRestoreDeferred cleanup failed: {ex.Message}".LogWarning();
                }
                _deferredPersistRegistered = false;
            }
        }

        /// <summary>
        /// Applies all stat values from a DTO to the runtime lists, updating existing entries
        /// and adding any that are missing (e.g. newly added stat types).
        /// </summary>
        private void ApplyStatDtoToRuntime(CharacterInstanceStatsDto dto)
        {
            if (dto.BoundedStats != null)
            {
                foreach (var b in dto.BoundedStats)
                {
                    if (b == null || !Enum.TryParse<BoundedStatType>(b.StatType, out var st))
                    {
                        continue;
                    }

                    var existing = StatHelpers.GetBoundedStat(_runtimeBoundedStats, st);
                    if (existing != null)
                    {
                        existing.SetMax(b.Max);
                        existing.SetCurrent(b.Current);
                    }
                    else
                    {
                        _runtimeBoundedStats.Add(
                            new BoundedCharacterStat(b.Max, b.Current, b.Min, st)
                        );
                    }
                }
            }

            if (dto.UnboundedStats != null)
            {
                foreach (var u in dto.UnboundedStats)
                {
                    if (u == null || !Enum.TryParse<UnboundedStatType>(u.StatType, out var ut))
                    {
                        continue;
                    }

                    var existing = StatHelpers.GetUnboundedStat(_runtimeUnboundedStats, ut);
                    if (existing != null)
                    {
                        existing.SetCurrent(u.Current);
                    }
                    else
                    {
                        _runtimeUnboundedStats.Add(new CharacterStat(u.Current, ut));
                    }
                }
            }
        }
    }
        #endregion
}
