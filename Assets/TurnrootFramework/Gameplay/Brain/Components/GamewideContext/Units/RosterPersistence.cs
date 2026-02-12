using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;

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
                $"Roster {roster.name} already registered".LogInfo("RosterPersistence");

                return;
            }

            var hash = ComputeRosterHash(roster);
            _ltm.Remember(key, hash);
            AddToRosterIndex(roster.Id);
        }

        public void RegisterPlayerRoster(PlayerTeamRoster roster)
        {
            if (roster == null)
            {
                return;
            }

            var key = BuildRosterKey(roster.Id);
            var existing = _ltm.Recall(key);

            if (!string.IsNullOrEmpty(existing))
            {
                return;
            }

            var hash = ComputeRosterHash(roster);
            _ltm.Remember(key, hash);
            AddToRosterIndex(roster.Id);
        }

        public bool HasRosterInLTM(GenericRoster roster)
        {
            var key = BuildRosterKey(roster.Id);
            return !string.IsNullOrEmpty(_ltm.Recall(key));
        }

        public bool HasPlayerRosterInLTM(PlayerTeamRoster roster)
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

        private string BuildRosterKey(string rosterId) => $"{LtmKeys.Roster}.{rosterId}";

        private string ComputeRosterHash(Characters.Roster roster)
        {
            var keys = new List<string>();
            foreach (var placement in roster.characters)
            {
                var key =
                    $"{placement.CharacterData?.name ?? "null"}_{placement.SpawnPosition.x},{placement.SpawnPosition.y}_{(int)placement.Status}_{placement.IsActiveRightNow}_{placement.Order}";
                if (placement is PlayerTeamRoster.PlayerTeamRosterUnitPlacement playerPlacement)
                {
                    key += $"_{playerPlacement.ChosenForThisBattle}";
                }
                keys.Add(key);
            }
            keys.Sort();
            var combined = string.Join("|", keys);

            // Simple djb2 hash for performance
            uint hash = 5381;
            foreach (char c in combined)
            {
                hash = ((hash << 5) + hash) + c; // hash * 33 + c
            }
            return hash.ToString();
        }

        private void AddToRosterIndex(string rosterId)
        {
            GamewideContextBrainHelpers.AddToIndexIfMissing(_ltm, LtmKeys.RosterIndex, rosterId);
        }
    }
}
