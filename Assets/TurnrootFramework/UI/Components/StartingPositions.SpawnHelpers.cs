using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public partial class StartingPositions
    {
        // Extracted spawn/cleanup helpers to keep main file concise.
        internal void SpawnAllUnitModels_Impl()
        {
            if (_replaced || _prepObject?.placements == null)
            {
                return;
            }

            if (!_gridPointsEnsured)
            {
                _mapGrid?.EnsureGridPoints();
                _gridPointsEnsured = true;
            }

            CleanupOrphanedModels();
            DespawnExistingModels();

            TurnrootLogger.Log(
                $"SpawnAllUnitModels: spawn points={_prepObject.PlayerTeamSpawnPoints?.Count ?? 0}, placements={_prepObject.placements?.Count ?? 0}"
            );

            if (_prepObject.PlayerTeamSpawnPoints != null)
            {
                foreach (var p in _prepObject.PlayerTeamSpawnPoints)
                {
                    TurnrootLogger.Log($"SpawnAllUnitModels: spawnPoint {p}");
                }
            }

            if (
                _prepObject.PlayerTeamSpawnPoints != null
                && _prepObject.PlayerTeamSpawnPoints.Count
                    != _prepObject.PlayerTeamSpawnPoints.Distinct().Count()
            )
            {
                TurnrootLogger.Log(
                    "SpawnAllUnitModels: Duplicate PlayerTeamSpawnPoints detected",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            foreach (var placement in _prepObject.placements)
            {
                var unit = placement.Value;
                var pos = placement.Key;

                if (
                    _prepObject.PlayerTeamSpawnPoints == null
                    || !_prepObject.PlayerTeamSpawnPoints.Contains(pos)
                )
                {
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Skipping spawn for {unit?.CharacterTemplate?.DisplayName ?? "<null>"} at {pos} - not a valid player spawn point",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                TurnrootLogger.Log(
                    $"SpawnAllUnitModels: Spawning at {pos} unitId={(unit?.Id ?? "<null>")} name={(unit?.CharacterTemplate?.DisplayName ?? "<unknown>")}"
                );

                var spawnResult = _prepObject.Brain.unitAppearanceBrain.SpawnUnitAtPosition(
                    unit: placement.Value,
                    position: placement.Key,
                    prebattle: true
                );
                if (!spawnResult.Success)
                {
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Failed to spawn at {placement.Key}: {spawnResult.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                var model = _prepObject.Brain.unitAppearanceBrain.GetModelForUnit(unit.Id);
                if (model != null)
                {
                    _unitModels[placement.Key] = model;
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Model spawned for {unit?.CharacterTemplate?.DisplayName} at {placement.Key}"
                    );
                }
                else
                {
                    TurnrootLogger.Log(
                        $"SpawnAllUnitModels: Model spawned but not found for {unit?.CharacterTemplate?.DisplayName} at {placement.Key}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
        }

        internal void CleanupOrphanedModels_Impl()
        {
            var validIds = new HashSet<string>(
                _prepObject
                    .placements.Values.Where(p => p != null && !string.IsNullOrEmpty(p.Id))
                    .Select(p => p.Id)
            );

            var ownerships = FindObjectsByType<UnitModelOwnership>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var own in ownerships)
            {
                if (own == null || string.IsNullOrEmpty(own.UnitId))
                {
                    continue;
                }

                if (validIds.Contains(own.UnitId))
                {
                    continue;
                }

                DestroyModel(own.gameObject);
                RemoveModelMapping(own.gameObject);
            }
        }

        internal void DespawnExistingModels_Impl()
        {
            if (_unitModels.Count == 0 || _prepObject?.Brain == null)
            {
                return;
            }

            var positions = _unitModels.Keys.ToList();
            foreach (var pos in positions)
            {
                _prepObject.Brain.unitAppearanceBrain.DespawnUnitAtPosition(pos);
            }
        }

        public void DespawnAllModels_Internal()
        {
            if (_unitModels == null || _unitModels.Count == 0)
            {
                return;
            }

            var positions = _unitModels.Keys.ToList();
            foreach (var pos in positions)
            {
                if (_prepObject?.Brain != null)
                {
                    _prepObject.Brain.unitAppearanceBrain.DespawnUnitAtPosition(pos);
                }
                else if (_unitModels.TryGetValue(pos, out var model) && model != null)
                {
                    DestroyModel(model);
                    _unitModels.Remove(pos);
                }
            }
        }

        private void UpdateModelPosition(GameObject model, Vector2Int pos)
        {
            var worldPos = _mapGrid.GetTerrainAdjustedWorldPosition(pos);
            model.transform.position = worldPos;
        }

        private void PublishSwapEvent(
            GameObject modelA,
            GameObject modelB,
            Vector2Int posA,
            Vector2Int posB
        )
        {
            var idA = modelA.GetComponent<UnitModelOwnership>().UnitId;
            var idB = modelB.GetComponent<UnitModelOwnership>().UnitId;

            _prepObject.Brain?.Publish(
                new Gameplay.Brain.Events.ModelSwappedEvent(idA, idB, posA, posB, modelA, modelB)
            );
        }

        private void PublishMoveEvent(GameObject model, Vector2Int from, Vector2Int to)
        {
            var owner = model.GetComponent<UnitModelOwnership>();
            var id = owner?.UnitId;
            CharacterInstance inst = null;

            if (!string.IsNullOrEmpty(id))
            {
                var all = _prepObject.Brain?.gamewideContextBrain?.GetAllActiveInstances();
                inst = all?.FirstOrDefault(u => u != null && u.Id == id);
            }

            _prepObject.Brain?.Publish(
                new Gameplay.Brain.Events.ModelMovedEvent(inst, id, from, to, model)
            );
        }
    }
}
