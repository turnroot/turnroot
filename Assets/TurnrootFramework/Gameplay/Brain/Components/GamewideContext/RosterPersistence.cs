using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles roster save/load to LongTermMemory.
    /// Single responsibility: persist roster data.
    /// </summary>
    public class RosterPersistence
    {
        private readonly LongTermMemory _ltm;

        public RosterPersistence(LongTermMemory ltm)
        {
            _ltm = ltm;
        }

        public void RegisterRoster(GenericRoster roster)
        {
            if (roster == null)
            {
                return;
            }

            var key = BuildRosterKey(roster.Id);
            var existing = _ltm.Recall(key);

            if (!string.IsNullOrEmpty(existing))
            {
                Debug.Log($"Roster {roster.name} already registered");
                return;
            }

            var hash = ComputeRosterHash(roster);
            _ltm.Remember(key, hash);
            AddToRosterIndex(roster.Id);

            Debug.Log($"Registered roster: {roster.name}");
        }

        public bool HasRosterInLTM(GenericRoster roster)
        {
            var key = BuildRosterKey(roster.Id);
            return !string.IsNullOrEmpty(_ltm.Recall(key));
        }

        public List<string> GetIndexedRosterIds()
        {
            var indexJson = _ltm.Recall(LtmKeys.RosterIndex);
            return string.IsNullOrEmpty(indexJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(indexJson);
        }

        private string BuildRosterKey(string rosterId) => $"GWB.Roster.{rosterId}";

        private string ComputeRosterHash(GenericRoster roster)
        {
            // TODO: Use your existing hash logic
            return Guid.NewGuid().ToString();
        }

        private void AddToRosterIndex(string rosterId)
        {
            var indexJson = _ltm.Recall(LtmKeys.RosterIndex);
            var index = string.IsNullOrEmpty(indexJson)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(indexJson);

            if (!index.Contains(rosterId))
            {
                index.Add(rosterId);
                _ltm.Remember(LtmKeys.RosterIndex, JsonConvert.SerializeObject(index));
            }
        }
    }
}
