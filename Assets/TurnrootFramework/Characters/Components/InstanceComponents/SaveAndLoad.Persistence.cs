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
                var ltm = CurrentBrain.GetComponent<LongTermMemory>();
                if (ltm == null || !ltm.Initialized)
                {
                    return;
                }

                DeduplicateRuntimeStats();
                var dto = BuildRuntimeDto();
                string key = $"CharacterInstance/{Id}/Stats";
                ltm.Remember(key, CurrentBrain.EncodeString(JsonUtility.ToJson(dto)));
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
                var ltm = CurrentBrain.ltm;

                if (ltm == null || !ltm.Initialized)
                {
                    if (!_deferredPersistRegistered)
                    {
                        CurrentBrain.OnLtmKeyCacheUpdated += OnBrainLtmStatRestoreDeferred;
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

                json = CurrentBrain.DecodeString(json);
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
                    CurrentBrain.OnLtmKeyCacheUpdated -= OnBrainLtmStatRestoreDeferred;
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
                    if (!Enum.IsDefined(typeof(UnboundedStatType), ut))
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
}
