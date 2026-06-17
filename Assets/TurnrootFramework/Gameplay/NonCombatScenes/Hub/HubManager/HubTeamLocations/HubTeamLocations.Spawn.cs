using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        private OperationResult ValidateSpawnability(
            HubCharacterSpawnArea location,
            Brain.Brain brain
        )
        {
            if (location == null)
            {
                return OperationResult.Failure("Cannot spawn characters for a null location");
            }

            if (brain == null)
            {
                $"HubCharacterSpawnArea {location.LocationName}: Cannot spawn characters because Brain is null".LogWarning();
                return OperationResult.Failure("Cannot spawn characters because Brain is null");
            }

            if (location.CharactersPresent == null || location.CharactersPresent.Length == 0)
            {
                if (location.UnitSpawnPoints != null)
                {
                    foreach (var p in location.UnitSpawnPoints)
                    {
                        if (p.UnitSpawnPoint != null)
                        {
                            p.UnitSpawnPoint.gameObject.SetActive(false);
                        }
                    }
                }

                return OperationResult.Failure("No characters assigned to this location");
            }

            if (location.UnitSpawnPoints == null || location.UnitSpawnPoints.Length == 0)
            {
                $"HubCharacterSpawnArea {location.LocationName}: No spawn points set for this area".LogWarning();
                return OperationResult.Failure("No spawn points set for this area");
            }
            return OperationResult.Successful();
        }

        public void SpawnCharactersForLocation(HubCharacterSpawnArea location, Brain.Brain brain)
        {
            if (!ValidateSpawnability(location, brain).Success)
            {
                return;
            }

            foreach (var p in location.UnitSpawnPoints)
            {
                if (p.UnitSpawnPoint != null)
                {
                    p.UnitSpawnPoint.gameObject.SetActive(true);
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

            var usedSpawnPoints = new HashSet<Transform>();

            for (int i = 0; i < location.CharactersPresent.Length; i++)
            {
                var character = location.CharactersPresent[i];
                if (character == null)
                {
                    $"HubCharacterSpawnArea {location.LocationName}: CharactersPresent contains a null entry".LogWarning();
                    continue;
                }

                if (_spawnedCharacterIds.Contains(character.Id))
                {
                    continue;
                }

                var entry = location.UnitSpawnPoints[
                    spawnPointIndices[i % spawnPointIndices.Length]
                ];
                var spawnPoint = entry.UnitSpawnPoint;
                if (spawnPoint == null)
                {
                    $"HubCharacterSpawnArea {location.LocationName}: UnitSpawnPoints contains a null entry".LogWarning();
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
                    $"HubCharacterSpawnArea {location.LocationName}: No HubPoiUi found on spawn point or children '{spawnPoint.name}'".LogWarning();
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
                    brain.unitAppearanceBrain.SetupHubIdleAnimation(existingModel, character);
                    continue;
                }

                var model = brain.unitAppearanceBrain?.CreateModelForUnit(character);
                if (model == null)
                {
                    $"HubCharacterSpawnArea {location.LocationName}: Failed to create model for character".LogWarning();
                    continue;
                }

                model.transform.SetPositionAndRotation(spawnPosition, spawnPoint.rotation);
                model.transform.SetParent(location.transform, worldPositionStays: true);
                _spawnedCharacterIds.Add(character.Id);
                brain.unitAppearanceBrain.SetupHubIdleAnimation(model, character);
                if (poiUi != null)
                {
                    poiUi.SetUnitCharacter(character);
                    poiUi.AvatarPoint = entry.AvatarPoint;
                }
            }

            if (location.CharactersPresent.Length == 0)
            {
                foreach (var p in location.UnitSpawnPoints)
                {
                    if (p.UnitSpawnPoint != null)
                    {
                        p.UnitSpawnPoint.gameObject.SetActive(false);
                    }
                }
            }

            foreach (var entry in location.UnitSpawnPoints)
            {
                if (entry.UnitSpawnPoint != null && !usedSpawnPoints.Contains(entry.UnitSpawnPoint))
                {
                    entry.UnitSpawnPoint.gameObject.SetActive(false);
                }
            }
        }
    }
}
