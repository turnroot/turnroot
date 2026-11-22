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
        private void EnsurePersistedInLtm()
        {
            try
            {
                var ltm = CurrentBrain.ltm;
                if (ltm == null)
                {
                    return;
                }

                if (!IsLtmReady(ltm, CurrentBrain))
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
                    HandleNoExistingEntry(ltm, key, runtimeDto, CurrentBrain);
                    return;
                }

                json = CurrentBrain.DecodeString(json);
                if (string.IsNullOrEmpty(json))
                {
                    HandleNoExistingEntry(ltm, key, runtimeDto, CurrentBrain);
                    return;
                }

                var existingDto = JsonUtility.FromJson<CharacterInstanceStatsDto>(json);
                if (existingDto == null)
                {
                    // Nothing valid in LTM; initialize from runtime
                    ltm.Remember(key, CurrentBrain.EncodeString(JsonUtility.ToJson(runtimeDto)));
                    existingDto = runtimeDto;
                }

                var mergedDto = MergeWithRequiredStats(existingDto, runtimeDto, out var changed);
                if (changed)
                {
                    ltm.Remember(key, CurrentBrain.EncodeString(JsonUtility.ToJson(mergedDto)));
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

    }
}
