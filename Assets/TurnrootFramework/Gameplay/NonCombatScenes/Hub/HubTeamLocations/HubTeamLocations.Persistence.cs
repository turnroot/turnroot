using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubTeamLocations
    {
        private const string LtmKeyPrefix = "HubTeamLocationPlacement_";

        private System.Collections.Generic.Dictionary<int, HubSublocationName> LoadSavedPlacement(
            PlayerTeamRoster roster
        )
        {
            if (roster == null || string.IsNullOrEmpty(roster.Id) || _brain?.ltm == null)
            {
                return null;
            }

            var date = _brain.ltm.GetGameDate();
            string key = GetPlacementKey(roster.Id, date);
            string json = _brain.ltm.Recall(key);
            if (string.IsNullOrEmpty(json))
            {
                $"HubTeamLocations: No saved placement found for key {key}".LogInfo();
                return null;
            }

            try
            {
                var newFormat = JsonUtility.FromJson<PlacementMap>(json);
                if (newFormat?.Entries != null && newFormat.Entries.Length > 0)
                {
                    var map = newFormat.Map;
                    return map;
                }

                $"HubTeamLocations: Loaded placement JSON for {key} but map was null".LogWarning();
                return null;
            }
            catch (Exception ex)
            {
                $"HubTeamLocations: Failed to decode placement JSON for {key}: {ex}".LogWarning();
                return null;
            }
        }

        private void SavePlacement(
            PlayerTeamRoster roster,
            System.Collections.Generic.Dictionary<int, HubSublocationName> map
        )
        {
            if (
                roster == null
                || string.IsNullOrEmpty(roster.Id)
                || map == null
                || _brain?.ltm == null
            )
            {
                return;
            }

            var date = _brain.ltm.GetGameDate();
            string key = GetPlacementKey(roster.Id, date);
            var wrapper = new PlacementMap
            {
                Entries = map.Select(kvp => new PlacementEntry
                    {
                        Index = kvp.Key,
                        Location = kvp.Value,
                    })
                    .ToArray(),
            };

            string json = JsonUtility.ToJson(wrapper);
            _brain.ltm.Remember(key, json);
        }

        private string GetPlacementKey(string rosterId, GameDate date) =>
            $"{LtmKeyPrefix}{rosterId}_{date.year:0000}{date.month:00}{date.day:00}";

        [Serializable]
        private class PlacementEntry
        {
            public int Index;
            public HubSublocationName Location;
        }

        [Serializable]
        private class PlacementMap
        {
            public PlacementEntry[] Entries;

            public System.Collections.Generic.Dictionary<int, HubSublocationName> Map =>
                Entries?.ToDictionary(e => e.Index, e => e.Location)
                ?? new System.Collections.Generic.Dictionary<int, HubSublocationName>();
        }
    }
}


