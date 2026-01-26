using System;
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterInstance : Serialization.IPostDeserialize, IHasStats
    {
        #region Stat Persistence

        // DTOs for serializing stats into LTM
        [Serializable]
        private class BoundedStatDto
        {
            public string StatType;
            public float Max;
            public float Current;
            public float Min;
        }

        [Serializable]
        private class UnboundedStatDto
        {
            public string StatType;
            public float Current;
        }

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

                // If LTM exists but DefaultStat keyset isn't present yet, defer persistence until the cache is populated
                try
                {
                    var existingDefaultKeys = ltm.RecallKeysByPrefix("DefaultStat");
                    if (
                        (existingDefaultKeys == null || existingDefaultKeys.Count == 0)
                        && !_deferredPersistRegistered
                    )
                    {
                        // Subscribe once to the brain's key cache update event and retry when it's ready
                        brain.OnLtmKeyCacheUpdated += OnBrainLtmKeyCacheUpdated;
                        _deferredPersistRegistered = true;
                        TurnrootLogger.Log(
                            $"CharacterInstance.EnsurePersistedInLtm: Deferring persistence for {Id} until LTM key cache is populated."
                        );
                        return;
                    }
                }
                catch { }

                // Use instance-specific persistence for unique characters; otherwise rely on template-level defaults
                string key = $"CharacterInstance/{Id}/Stats";

                var json = ltm.Recall(key);

                // Build a DTO from runtime state
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

                if (string.IsNullOrEmpty(json))
                {
                    // No existing entry - create one for unique characters
                    if (_characterTemplate?.IsUnique == true)
                    {
                        var toSave = JsonUtility.ToJson(runtimeDto);
                        ltm.Remember(key, toSave);
                    }
                    else
                    {
                        // For non-unique characters, ensure template defaults exist via StatHelpers by probing GetOrCreate with templateId
                        var templateId = _characterTemplate?.FullName;
                        foreach (var bs in _runtimeBoundedStats)
                        {
                            StatHelpers.GetOrCreateBoundedStat(
                                _runtimeBoundedStats,
                                bs.StatType,
                                templateId
                            );
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
                    return;
                }

                bool changed = false;
                var existingDto = JsonUtility.FromJson<CharacterInstanceStatsDto>(json);
                if (existingDto == null)
                {
                    existingDto = runtimeDto;
                    changed = true;
                }

                // Ensure LTM contains all stat types; add from runtime or default as needed
                // Bounded
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
                        // Try to get runtime stat
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
                            // Fallback to default values (and persist template defaults)
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

                // Unbounded
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
                                        Current =
                                            StatHelpers.GetDefaultValueForUnboundedStatInternal(t),
                                    }
                                );
                            }
                        }
                        changed = true;
                    }
                }

                if (changed)
                {
                    var toSaveDto = new CharacterInstanceStatsDto
                    {
                        BoundedStats = newBoundedList.ToArray(),
                        UnboundedStats = newUnboundedList.ToArray(),
                    };
                    var toSaveJson = JsonUtility.ToJson(toSaveDto);
                    ltm.Remember(key, toSaveJson);
                }

                // Ensure runtime contains any stats present in LTM but missing runtime
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
            catch (Exception ex)
            {
                TurnrootLogger.Log(
                    $"CharacterInstance.EnsurePersistedInLtm: failed to persist/merge stats for {Id}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
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
                catch { }
                _deferredPersistRegistered = false;
            }
        }
    }
        #endregion
}
