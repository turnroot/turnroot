using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubTeamLocations
    {
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

            foreach (var p in location.UnitSpawnPoints)
            {
                if (p != null)
                {
                    p.gameObject.SetActive(true);
                }
            }

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

            var hubManager =
                _hubManager != null ? _hubManager : FindFirstObjectByType<HubManager>();

            var usedSpawnPoints = new HashSet<Transform>();

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
                        p.gameObject.SetActive(false);
                    }
                }
            }

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
