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

            foreach (var unit in roster.characters)
            {
                if (unit.Status == UnitStatus.Defeated || subLocations.Length == 0)
                {
                    continue;
                }

                // pick a random valid sublocation; if it's full, try another until we find one or exhaust all
                int attempts = 0;
                int pickIndex = Random.Range(0, subLocations.Length);
                while (attempts < subLocations.Length)
                {
                    var assignedLocation = subLocations[pickIndex];

                    if (assignedLocation.LocationName == HubSublocationName.Battlefields)
                    {
                        // Can't go to battlefields, try another.
                        pickIndex = (pickIndex + 1) % subLocations.Length;
                        attempts++;
                        continue;
                    }

                    assignedLocation.CharactersPresent ??= new CharacterInstance[0];
                    if (assignedLocation.CharactersPresent.Length >= maxPerLocation)
                    {
                        // Location is full, try another.
                        pickIndex = (pickIndex + 1) % subLocations.Length;
                        attempts++;
                        continue;
                    }

                    // locate the layout entry that matches this location explicitly
                    int layoutIndex = -1;
                    if (LocationLayouts != null)
                    {
                        for (int idx = 0; idx < LocationLayouts.Length; idx++)
                        {
                            if (LocationLayouts[idx].location == assignedLocation.LocationName)
                            {
                                layoutIndex = idx;
                                break;
                            }
                        }
                    }
                    if (layoutIndex < 0)
                    {
                        // if user didn't configure layouts correctly fall back to pick index
                        layoutIndex = pickIndex;
                    }

                    CharacterInstance ci = _charFactory?.CreateOrRecall(unit.CharacterData);
                    if (ci != null)
                    {
                        var list = new System.Collections.Generic.List<CharacterInstance>(
                            assignedLocation.CharactersPresent
                        )
                        {
                            ci,
                        };
                        assignedLocation.CharactersPresent = list.ToArray();
                        $"HubTeamLocations: Assigned {ci.CharacterTemplate.DisplayName} to {assignedLocation.LocationName}".LogInfo();
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
                                    ci.CharacterTemplate.DefaultPortrait?.RuntimeSprite
                                        ?? FallBackPortrait
                                );
                            }
                        }
                        else
                        {
                            $"HubTeamLocations: No horizontal layout prefab or unit portrait prefab assigned for {assignedLocation.LocationName}".LogWarning();
                        }
                    }

                    break; // assigned, move to next unit
                }
            }
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
                $"HubSubLocation {location.LocationName}: No characters set to be present in this sublocation".LogInfo();
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
                int j = Random.Range(i, spawnPointIndices.Length);
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
                    // Already spawned for this sublocation.
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

                // POIs are typically attached to the spawn point. Use that POI for this unit.
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
                    // Always set the label for this POI so it matches the associated character.
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
