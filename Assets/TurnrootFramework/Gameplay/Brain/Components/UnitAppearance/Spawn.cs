using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles unit model spawning, positioning, and despawning.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        public OperationResult SpawnUnitAtPosition(
            CharacterInstance unit,
            Vector2Int position,
            bool prebattle = false
        )
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!validation.Success)
            {
                return validation;
            }

            var worldPos = GetWorldPosition(position, prebattle);

            // Validate map grid and grid point to avoid silent spawns at Vector3.zero
            var mapGrid =
                _brain.battleBrain.PreparationObject.MapGrid
                ?? _brain.battleBrain.BattleObject.Context.MapGrid ?? _brain
                    .battleBrain
                    .BattleObject
                    .MapGrid;

            if (mapGrid == null)
            {
                TurnrootLogger.Log(
                    $"SpawnUnitAtPosition: Aborting spawn for {unit.CharacterTemplate.DisplayName} - no MapGrid available",
                    TurnrootLogger.LogLevel.Warning
                );
                return OperationResult.Failure("No MapGrid available for spawn");
            }

            var gridPoint = mapGrid.GetGridPoint(position.x, position.y);
            if (gridPoint == null)
            {
                TurnrootLogger.Log(
                    $"SpawnUnitAtPosition: Aborting spawn for {unit.CharacterTemplate.DisplayName} - invalid grid position {position}",
                    TurnrootLogger.LogLevel.Warning
                );
                return OperationResult.Failure($"Invalid spawn grid point: {position}");
            }

            // Ensure CharacterInstance has correct position for PREBATTLE visuals and precompute only.
            // During an actual battle we rely on `BattleContext.SpawnAtPosition` (SpawnCommand)
            // to set authoritative positions. Avoid overwriting MapGridPosition during battle-start
            // to prevent competing writers.
            try
            {
                if (prebattle)
                {
                    unit.MapGridPosition = position;
                }
                // Mark as spawned during battle if this call is part of the battle flow.
                if (!prebattle)
                {
                    unit.WasSpawnedDuringBattle = true;
                }
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"SpawnUnitAtPosition: Failed setting instance state for {unit.Id}: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            // Recompute exact world position using validated MapGrid
            worldPos = mapGrid.GetTerrainAdjustedWorldPosition(position);

            if (_unitModels.TryGetValue(unit.Id, out var existingModel))
            {
                return MoveExistingModel(unit, existingModel, position, worldPos);
            }

            ClearPositionIfOccupied(position);

            return CreateAndPlaceModel(unit, position, worldPos);
        }

        public OperationResult PrecomputeSpawnModelAt(
            CharacterInstance unit,
            Vector2Int position,
            bool prebattle = false
        )
        {
            var validation = OperationResultGuards.RequireNotNull(unit, nameof(unit));
            if (!validation.Success)
            {
                return validation;
            }

            // If model already exists, don't try to move it - precompute is just for setup
            return _unitModels.ContainsKey(unit.Id)
                ? OperationResult.Successful()
                : SpawnUnitAtPosition(unit, position, prebattle);
        }

        public OperationResult DespawnUnit(string unitId)
        {
            var validation = OperationResultGuards.RequireNotNullOrEmpty(unitId, nameof(unitId));
            if (!validation.Success)
            {
                return validation;
            }

            if (!_unitModels.TryGetValue(unitId, out var model))
            {
                return OperationResult.Failure($"No model found for unit {unitId}");
            }

            var position = _modelPositions.FirstOrDefault(kvp => kvp.Value == unitId).Key;
            if (position != default)
            {
                _modelPositions.Remove(position);
            }

            if (model != null)
            {
                var unit = Brain
                    .gamewideContextBrain.GetAllActiveInstances()
                    .FirstOrDefault(u => u != null && u.Id == unitId);

                if (unit != null)
                {
                    ClearWeaponFromUnit(unit);
                    ClearMountFromUnit(unit);
                }

                Brain.Publish(new ModelDespawnedEvent(unit, unitId, position, model));

                model.SetActive(false);
                Destroy(model);
            }

            _unitModels.Remove(unitId);
            return OperationResult.Successful();
        }

        public OperationResult DespawnUnitAtPosition(Vector2Int position)
        {
            if (!_modelPositions.TryGetValue(position, out var unitId))
            {
                TurnrootLogger.Log("No model found at position", TurnrootLogger.LogLevel.Warning);
                return OperationResult.Successful();
            }
            return DespawnUnit(unitId);
        }

        private OperationResult MoveExistingModel(
            CharacterInstance unit,
            GameObject model,
            Vector2Int newPosition,
            Vector3 worldPos
        )
        {
            var oldPosition = _modelPositions.FirstOrDefault(kvp => kvp.Value == unit.Id).Key;
            if (oldPosition != default)
            {
                _modelPositions.Remove(oldPosition);
            }

            ClearPositionIfOccupied(newPosition);

            model.transform.SetPositionAndRotation(worldPos, Quaternion.identity);

            _modelPositions[newPosition] = unit.Id;

            if (model.TryGetComponent<UnitModelOwnership>(out var ownership))
            {
                ownership.DisplayName = unit.CharacterTemplate.DisplayName;
            }

            if (unit.CurrentWeaponPrefab == null)
            {
                AttachWeaponToUnit(unit, model);
            }

            if (unit.CurrentShieldPrefab == null)
            {
                AttachShieldToUnit(unit, model);
            }

            // Handle mount status changes
            bool shouldBeMounted = ShouldUnitBeMounted(unit);
            if (shouldBeMounted && !unit.IsMounted)
            {
                AttachMountToUnit(unit, model);
            }
            else if (!shouldBeMounted && unit.IsMounted)
            {
                DismountUnit(unit, model);
            }

            Brain.Publish(new ModelSpawnedEvent(unit, unit.Id, newPosition, model));
            return OperationResult.Successful();
        }

        private OperationResult CreateAndPlaceModel(
            CharacterInstance unit,
            Vector2Int position,
            Vector3 worldPos
        )
        {
            // CRITICAL: Create a positioned root FIRST, then build the model in it
            var root = new GameObject($"{unit.CharacterTemplate.DisplayName}_Root");
            root.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            root.transform.localScale = Vector3.one * _brain.uiBrain.uiSettings.ModelsScale;
            var model = CreateModelForUnit(unit, root);
            if (model == null)
            {
                Destroy(root);
                return OperationResult.Failure(
                    $"Failed to create model for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            var ownership = model.AddComponent<UnitModelOwnership>();
            ownership.UnitId = unit.Id;
            ownership.DisplayName = unit.CharacterTemplate.DisplayName;
            model.name = $"{unit.CharacterTemplate.DisplayName}_Model_{unit.Id}";

            ApplyVisuals(unit, model);

            _unitModels[unit.Id] = model;
            _modelPositions[position] = unit.Id;

            AttachWeaponToUnit(unit, model);

            AttachShieldToUnit(unit, model);

            if (ShouldUnitBeMounted(unit))
            {
                AttachMountToUnit(unit, model);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev diagnostic: confirm model created and placed where expected.
            TurnrootLogger.Log(
                $"CreateAndPlaceModel: Created model '{model.name}' for unit {unit.Id} at grid {position}, world {worldPos}",
                TurnrootLogger.LogLevel.Info
            );
#endif

            Brain.Publish(new ModelSpawnedEvent(unit, unit.Id, position, model));
            return OperationResult.Successful();
        }

        private void ClearPositionIfOccupied(Vector2Int position)
        {
            if (_modelPositions.TryGetValue(position, out var occupyingUnitId))
            {
                // Another unit is at this position - despawn it
                DespawnUnit(occupyingUnitId);
            }
        }
    }
}
