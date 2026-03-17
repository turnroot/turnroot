using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Components.UI;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Characters.Roster;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [System.Serializable]
    public struct AdditionalUnitInfo
    {
        public CharacterInstance character;
        public HubSublocationName location;
    }

    [RequireComponent(typeof(HubManager))]
    /// <remarks>
    /// If you aren't using this feature in your game, you can ignore this-
    /// it will disable itself without activating if GameplayGeneralSettings.Instance.HubHasTeamLocations is false
    /// </remarks>
    public class HubTeamLocations : MonoBehaviour
    {
        private Brain.Brain _brain;
        private HubManager _hubManager;
        private CharacterFactory _charFactory;
        private readonly System.Collections.Generic.HashSet<string> _spawnedCharacterIds = new();

        public AdditionalUnitInfo[] NonRosterUnitsInHub;

        [System.Serializable]
        public struct LocationLayout
        {
            public HubSublocationName location;
            public GameObject layoutObject;
        }

        public LocationLayout[] LocationLayouts;
        public GameObject UnitLocationPortraitPrefab;

        public Sprite FallBackPortrait;

        public void Initialize(Brain.Brain brain, HubSubLocation[] subLocations)
        {
            _hubManager = GetComponent<HubManager>();
            _brain = brain;
            _charFactory = new CharacterFactory(_brain.ltm);

            var persistentRoster =
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            SetTeamLocations(persistentRoster, subLocations);
            SetNonRosterUnitsInHub(subLocations);

            SpawnAllCharacters(subLocations, _brain);
        }

        public void SpawnAllCharacters(HubSubLocation[] subLocations, Brain.Brain brain)
        {
            if (subLocations == null || subLocations.Length == 0)
            {
                return;
            }

            foreach (var location in subLocations)
            {
                SpawnCharactersForLocation(location, brain);
            }
        }

        public void SetTeamLocations(PlayerTeamRoster roster, HubSubLocation[] subLocations)
        {
            int maxPerLocation = GameplayGeneralSettings.Instance.MaxUnitsPerHubLocation;

            // Load an existing placement mapping for this roster/date, if available.
            var placementMap = LoadSavedPlacement(roster);
            bool changed = false;

            if (placementMap == null)
            {
                placementMap = new System.Collections.Generic.Dictionary<int, HubSublocationName>();
                changed = true;
            }

            for (int i = 0; i < roster.characters.Length; i++)
            {
                var unit = roster.characters[i];
                if (unit.Status == UnitStatus.Defeated || subLocations.Length == 0)
                {
                    continue;
                }

                // Use the saved placement if it exists; otherwise choose a new random location.
                if (!placementMap.TryGetValue(i, out var desiredLocation))
                {
                    desiredLocation = PickRandomValidLocation(subLocations, maxPerLocation);
                    placementMap[i] = desiredLocation;
                    changed = true;
                }

                AssignUnitToLocation(
                    roster,
                    i,
                    unit,
                    desiredLocation,
                    subLocations,
                    maxPerLocation
                );
            }

            if (changed)
            {
                SavePlacement(roster, placementMap);
            }
        }

        private HubSublocationName PickRandomValidLocation(
            HubSubLocation[] subLocations,
            int maxPerLocation
        )
        {
            int attempts = 0;
            int pickIndex = HubDayRandom.Range(0, subLocations.Length);

            while (attempts < subLocations.Length)
            {
                var assignedLocation = subLocations[pickIndex];

                if (assignedLocation.LocationName == HubSublocationName.Battlefields)
                {
                    pickIndex = (pickIndex + 1) % subLocations.Length;
                    attempts++;
                    continue;
                }

                assignedLocation.CharactersPresent ??= new CharacterInstance[0];
                if (assignedLocation.CharactersPresent.Length >= maxPerLocation)
                {
                    pickIndex = (pickIndex + 1) % subLocations.Length;
                    attempts++;
                    continue;
                }

                return assignedLocation.LocationName;
            }

            // fallback: choose first valid entry
            foreach (var location in subLocations)
            {
                if (location.LocationName != HubSublocationName.Battlefields)
                {
                    return location.LocationName;
                }
            }

            return HubSublocationName.Market;
        }

        private void AssignUnitToLocation(
            PlayerTeamRoster roster,
            int rosterIndex,
            Turnroot.Characters.Roster.UnitPlacement unit,
            HubSublocationName desiredLocation,
            HubSubLocation[] subLocations,
            int maxPerLocation
        )
        {
            var assignedLocation = Array.Find(subLocations, l => l.LocationName == desiredLocation);
            if (assignedLocation == null)
            {
                return;
            }

            assignedLocation.CharactersPresent ??= new CharacterInstance[0];
            if (assignedLocation.CharactersPresent.Length >= maxPerLocation)
            {
                return;
            }

            CharacterInstance ci = _charFactory?.CreateOrRecall(unit.CharacterData);
            if (ci == null)
            {
                return;
            }

            var list = new System.Collections.Generic.List<CharacterInstance>(
                assignedLocation.CharactersPresent
            )
            {
                ci,
            };
            assignedLocation.CharactersPresent = list.ToArray();

            int layoutIndex = FindLayoutIndexForLocation(assignedLocation.LocationName);
            if (
                layoutIndex >= 0
                && layoutIndex < LocationLayouts.Length
                && LocationLayouts[layoutIndex].layoutObject != null
                && UnitLocationPortraitPrefab != null
            )
            {
                var portrait = Instantiate(
                    UnitLocationPortraitPrefab,
                    LocationLayouts[layoutIndex].layoutObject.transform
                );
                var portraitScript = portrait.GetComponent<UnitLocationPortraitRefs>();
                if (portraitScript != null)
                {
                    portraitScript.Set(
                        ci.CharacterTemplate.DisplayName,
                        ci.CharacterTemplate.DefaultPortrait?.RuntimeSprite ?? FallBackPortrait
                    );
                }
            }
            else
            {
                $"HubTeamLocations: No horizontal layout prefab or unit portrait prefab assigned for {assignedLocation.LocationName}".LogWarning();
            }
        }

        private int FindLayoutIndexForLocation(HubSublocationName locationName)
        {
            if (LocationLayouts == null)
            {
                return -1;
            }

            for (int idx = 0; idx < LocationLayouts.Length; idx++)
            {
                if (LocationLayouts[idx].location == locationName)
                {
                    return idx;
                }
            }
            return -1;
        }

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

        [System.Serializable]
        private class PlacementEntry
        {
            public int Index;
            public HubSublocationName Location;
        }

        [System.Serializable]
        private class PlacementMap
        {
            public PlacementEntry[] Entries;

            public System.Collections.Generic.Dictionary<int, HubSublocationName> Map =>
                Entries?.ToDictionary(e => e.Index, e => e.Location)
                ?? new System.Collections.Generic.Dictionary<int, HubSublocationName>();
        }

        public void SetNonRosterUnitsInHub(HubSubLocation[] subLocations)
        {
            int maxPerLocation = GameplayGeneralSettings.Instance.MaxUnitsPerHubLocation;

            foreach (var info in NonRosterUnitsInHub)
            {
                var location = subLocations.FirstOrDefault(s => s.LocationName == info.location);
                if (location != null)
                {
                    location.CharactersPresent ??= new CharacterInstance[0];
                    if (location.CharactersPresent.Length >= maxPerLocation)
                    {
                        continue;
                    }

                    var list = new System.Collections.Generic.List<CharacterInstance>(
                        location.CharactersPresent
                    )
                    {
                        info.character,
                    };
                    location.CharactersPresent = list.ToArray();
                }
            }
        }

        public void SpawnCharactersForLocation(HubSubLocation location, Brain.Brain brain)
        {
            if (location == null)
            {
                return;
            }

            if (brain == null)
            {
                $"HubSubLocation {location.LocationName}: Cannot spawn characters because Brain is null".LogWarning();
                return;
            }

            if (location.CharactersPresent == null || location.CharactersPresent.Length == 0)
            {
                if (location.UnitSpawnPoints != null)
                {
                    foreach (var p in location.UnitSpawnPoints)
                    {
                        if (p != null)
                        {
                            p.gameObject.SetActive(false);
                        }
                    }
                }

                return;
            }

            if (location.UnitSpawnPoints == null || location.UnitSpawnPoints.Length == 0)
            {
                $"HubSubLocation {location.LocationName}: No spawn points set for this sublocation".LogWarning();
                return;
            }

            // Ensure all spawn points start enabled
            foreach (var p in location.UnitSpawnPoints)
            {
                if (p != null)
                {
                    p.gameObject.SetActive(true);
                }
            }

            // Randomize spawn point order so that runs are different each visit.
            var spawnPointIndices = new int[location.UnitSpawnPoints.Length];
            for (int i = 0; i < spawnPointIndices.Length; i++)
            {
                spawnPointIndices[i] = i;
            }
            for (int i = 0; i < spawnPointIndices.Length; i++)
            {
                int j = HubDayRandom.Range(i, spawnPointIndices.Length);
                (spawnPointIndices[i], spawnPointIndices[j]) = (
                    spawnPointIndices[j],
                    spawnPointIndices[i]
                );
            }

            var hubManager = FindFirstObjectByType<HubManager>();

            var usedSpawnPoints = new System.Collections.Generic.HashSet<Transform>();

            for (int i = 0; i < location.CharactersPresent.Length; i++)
            {
                var character = location.CharactersPresent[i];
                if (character == null)
                {
                    $"HubSubLocation {location.LocationName}: CharactersPresent contains a null entry".LogWarning();
                    continue;
                }

                if (_spawnedCharacterIds.Contains(character.Id))
                {
                    continue;
                }

                var spawnPoint = location.UnitSpawnPoints[
                    spawnPointIndices[i % spawnPointIndices.Length]
                ];
                if (spawnPoint == null)
                {
                    $"HubSubLocation {location.LocationName}: UnitSpawnPoints contains a null entry".LogWarning();
                    continue;
                }

                usedSpawnPoints.Add(spawnPoint);

                var poiUi = spawnPoint.GetComponentInChildren<HubPoiUi>();
                if (poiUi == null)
                {
                    poiUi = spawnPoint.GetComponent<HubPoiUi>();
                }
                if (poiUi == null)
                {
                    $"HubSubLocation {location.LocationName}: No HubPoiUi found on spawn point or children '{spawnPoint.name}'".LogWarning();
                }

                float spawnY =
                    hubManager?.GetSpawnPointHeight(spawnPoint, spawnPoint.position.y)
                    ?? spawnPoint.position.y;
                var spawnPosition = new Vector3(
                    spawnPoint.position.x,
                    spawnY,
                    spawnPoint.position.z
                );

                // If the model already exists, just reposition it
                var existingModel = brain.unitAppearanceBrain?.GetModelForUnit(character.Id);
                if (existingModel != null)
                {
                    existingModel.transform.SetPositionAndRotation(
                        spawnPosition,
                        spawnPoint.rotation
                    );
                    existingModel.transform.SetParent(location.transform, worldPositionStays: true);
                    _spawnedCharacterIds.Add(character.Id);
                    continue;
                }

                var model = brain.unitAppearanceBrain?.CreateModelForUnit(character);
                if (model == null)
                {
                    $"HubSubLocation {location.LocationName}: Failed to create model for character".LogWarning();
                    continue;
                }

                model.transform.SetPositionAndRotation(spawnPosition, spawnPoint.rotation);
                model.transform.SetParent(location.transform, worldPositionStays: true);
                _spawnedCharacterIds.Add(character.Id);
                if (poiUi != null)
                {
                    poiUi.SetUnitCharacter(character);
                }
            }

            if (location.CharactersPresent.Length == 0)
            {
                foreach (var p in location.UnitSpawnPoints)
                {
                    if (p != null)
                    {
                        p.gameObject.SetActive(false); // hide spawn points if there are no characters to spawn
                    }
                }
            }

            // Disable any unused spawn points so only active characters show POI markers
            foreach (var spawnPoint in location.UnitSpawnPoints)
            {
                if (spawnPoint != null && !usedSpawnPoints.Contains(spawnPoint))
                {
                    spawnPoint.gameObject.SetActive(false);
                }
            }
        }
    }
}
