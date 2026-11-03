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
                        p.UnitSpawnPoint?.gameObject.SetActive(false);
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
                p.UnitSpawnPoint?.gameObject.SetActive(true);
            }

            var spawnPointIndices = GetShuffledSpawnPointIndices(location.UnitSpawnPoints.Length);
            var usedSpawnPoints = new HashSet<Transform>();
            for (int i = 0; i < location.CharactersPresent.Length; i++)
            {
                SpawnCharacterAtLocation(location, brain, i, spawnPointIndices, usedSpawnPoints);
            }

            DeactivateUnusedSpawnPoints(location, usedSpawnPoints);
        }

        private static int[] GetShuffledSpawnPointIndices(int count)
        {
            var indices = new int[count];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            for (int i = 0; i < indices.Length; i++)
            {
                int j = HubDayRandom.Range(i, indices.Length);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }

        private void SpawnCharacterAtLocation(
            HubCharacterSpawnArea location,
            Brain.Brain brain,
            int characterIndex,
            int[] spawnPointIndices,
            HashSet<Transform> usedSpawnPoints
        )
        {
            var character = location.CharactersPresent[characterIndex];
            if (character == null)
            {
                $"HubCharacterSpawnArea {location.LocationName}: CharactersPresent contains a null entry".LogWarning();
                return;
            }

            if (_spawnedCharacterIds.Contains(character.Id))
            {
                return;
            }

            var entry = location.UnitSpawnPoints[
                spawnPointIndices[characterIndex % spawnPointIndices.Length]
            ];
            var spawnPoint = entry.UnitSpawnPoint;
            if (spawnPoint == null)
            {
                $"HubCharacterSpawnArea {location.LocationName}: UnitSpawnPoints contains a null entry".LogWarning();
                return;
            }

            usedSpawnPoints.Add(spawnPoint);
            var poiUi = FindPoiUi(location, spawnPoint);
            var spawnPosition = GetSpawnPosition(spawnPoint);
            var model = GetOrCreateModel(
                location,
                brain,
                character,
                spawnPosition,
                spawnPoint.rotation,
                out bool isNewModel
            );
            if (model == null)
            {
                return;
            }

            model.transform.SetPositionAndRotation(spawnPosition, spawnPoint.rotation);
            model.transform.SetParent(location.transform, worldPositionStays: true);
            _spawnedCharacterIds.Add(character.Id);
            brain.unitAppearanceBrain.SetupHubIdleAnimation(model, character);
            if (isNewModel && poiUi != null)
            {
                poiUi.SetUnitCharacter(character);
                poiUi.AvatarPoint = entry.AvatarPoint;
            }
        }

        private static HubPoiUi FindPoiUi(
            HubCharacterSpawnArea location,
            Transform spawnPoint
        )
        {
            var poiUi = spawnPoint.GetComponentInChildren<HubPoiUi>();
            if (poiUi == null)
            {
                poiUi = spawnPoint.GetComponent<HubPoiUi>();
            }

            if (poiUi == null)
            {
                $"HubCharacterSpawnArea {location.LocationName}: No HubPoiUi found on spawn point or children '{spawnPoint.name}'".LogWarning();
            }

            return poiUi;
        }

        private Vector3 GetSpawnPosition(Transform spawnPoint)
        {
            float spawnY = GetSpawnPointHeight(spawnPoint, spawnPoint.position.y);
            return new Vector3(spawnPoint.position.x, spawnY, spawnPoint.position.z);
        }

        private GameObject GetOrCreateModel(
            HubCharacterSpawnArea location,
            Brain.Brain brain,
            Characters.CharacterInstance character,
            Vector3 position,
            Quaternion rotation,
            out bool isNewModel
        )
        {
            var appearanceBrain = brain.unitAppearanceBrain;
            var model = appearanceBrain?.GetModelForUnit(character.Id);
            isNewModel = model == null;
            if (model == null)
            {
                model = appearanceBrain?.CreateModelForUnit(character);
                if (model == null)
                {
                    $"HubCharacterSpawnArea {location.LocationName}: Failed to create model for character".LogWarning();
                    return null;
                }
            }

            model.transform.SetPositionAndRotation(position, rotation);
            return model;
        }

        private static void DeactivateUnusedSpawnPoints(
            HubCharacterSpawnArea location,
            HashSet<Transform> usedSpawnPoints
        )
        {
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
