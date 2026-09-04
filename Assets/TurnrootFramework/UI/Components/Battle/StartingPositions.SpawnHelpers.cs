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
            // don't run twice and ensure we actually have placement data
            if (
                _replaced
                || !ValidationHelper.ValidateNotNull(
                    _prepObject.placements,
                    nameof(_prepObject.placements)
                )
            )
            {
                return;
            }

            if (!_gridPointsEnsured)
            {
                _mapGrid.EnsureGridPoints();
                _gridPointsEnsured = true;
            }

            CleanupOrphanedModels();
            DespawnExistingModels();

            if (
                _prepObject.PlayerTeamSpawnPoints.Count
                != _prepObject.PlayerTeamSpawnPoints.Distinct().Count()
            )
            {
                "SpawnAllUnitModels: Duplicate PlayerTeamSpawnPoints detected".LogWarning();
            }

            foreach (var placement in _prepObject.placements)
            {
                var data = placement.Value;
                var pos = placement.Key;

                if (!_prepObject.PlayerTeamSpawnPoints.Contains(pos))
                {
                    $"SpawnAllUnitModels: Skipping spawn for {data?.DisplayName ?? "<no-data>"} at {pos} - not a valid player spawn point".LogWarning();
                    continue;
                }

                // Resolve an active instance for the character data. Prefer the runtime roster instance if available.
                CharacterInstance unitInst = null;
                var gw = _prepObject.Brain?.gamewideContextBrain;
                if (gw != null)
                {
                    var persistent =
                        gw.GamewidePersistentPlayerRoster
                        ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
                    var runtimeInstance =
                        persistent != null ? gw.GetOrCreatePlayerTeamRoster(persistent) : null;
                    unitInst =
                        runtimeInstance?.GetInstanceFor(data) ?? gw.FindInstanceByTemplate(data);
                }

                if (unitInst == null)
                {
                    $"SpawnAllUnitModels: No active instance found for {data?.DisplayName ?? "<no-data>"} at {pos}; skipping model spawn".LogWarning();
                    continue;
                }

                var spawnResult = _prepObject.Brain.unitAppearanceBrain.SpawnUnitAtPosition(
                    unit: unitInst,
                    position: pos,
                    prebattle: true
                );
                if (!spawnResult.Success)
                {
                    $"SpawnAllUnitModels: Failed to spawn at {pos}: {spawnResult.ErrorMessage}".LogWarning();
                    continue;
                }

                // UnitAppearanceBrain is now the sole owner of model tracking - no need to store here
                $"SpawnAllUnitModels: Model spawned for {data?.DisplayName ?? "<no-data>"} at {pos}".LogInfo();
            }
        }

        internal void CleanupOrphanedModels_Impl()
        {
            var gw = _prepObject.Brain?.gamewideContextBrain;
            var validIds = new HashSet<string>();
            foreach (var data in _prepObject.placements.Values)
            {
                if (data == null)
                {
                    continue;
                }
                var inst = gw?.FindInstanceByTemplate(data);
                if (inst != null && !string.IsNullOrEmpty(inst.Id))
                {
                    validIds.Add(inst.Id);
                }
            }

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
            if (_prepObject?.Brain == null || _prepObject.placements == null)
            {
                return;
            }

            var gw = _prepObject.Brain.gamewideContextBrain;
            if (gw == null)
            {
                return;
            }

            // Get all unit IDs from placements and despawn their models
            foreach (var placement in _prepObject.placements)
            {
                var inst = gw.FindInstanceByTemplate(placement.Value);
                if (inst != null && !string.IsNullOrEmpty(inst.Id))
                {
                    _prepObject.Brain.unitAppearanceBrain.DespawnUnit(inst.Id);
                }
            }
        }

        public void DespawnAllModels_Internal()
        {
            if (_prepObject?.placements == null)
            {
                return;
            }

            var gw = _prepObject.Brain?.gamewideContextBrain;
            if (gw == null)
            {
                return;
            }

            // Despawn all models by getting unit IDs from placements
            foreach (var placement in _prepObject.placements)
            {
                var inst = gw.FindInstanceByTemplate(placement.Value);
                if (inst != null && !string.IsNullOrEmpty(inst.Id))
                {
                    if (_prepObject.Brain != null)
                    {
                        _prepObject.Brain.unitAppearanceBrain.DespawnUnit(inst.Id);
                    }
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

            _prepObject.Brain.Publish(
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
                var all = _prepObject.Brain.gamewideContextBrain.GetAllActiveInstances();
                inst = all.FirstOrDefault(u => u != null && u.Id == id);
            }

            _prepObject.Brain.Publish(
                new Gameplay.Brain.Events.ModelMovedEvent(inst, id, from, to, model)
            );
        }
    }
}
