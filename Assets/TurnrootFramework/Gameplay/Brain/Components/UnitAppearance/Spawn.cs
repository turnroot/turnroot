using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        /// <summary>
        /// Spawns or moves a unit's model to the specified grid position.
        /// If the unit already has a model, it's moved. Otherwise, a new model is created.
        /// </summary>
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

            // Does this unit already have a model?
            if (_unitModels.TryGetValue(unit.Id, out var existingModel))
            {
                return MoveExistingModel(unit, existingModel, position, worldPos);
            }

            // Clean up any model that might be at the target position
            ClearPositionIfOccupied(position);

            // Create new model
            return CreateAndPlaceModel(unit, position, worldPos);
        }

        /// <summary>
        /// Public helper for precompute systems to spawn a unit model.
        /// </summary>
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

            // SpawnUnitAtPosition already handles errors and returns OperationResult
            return SpawnUnitAtPosition(unit, position, prebattle);
        }

        /// <summary>
        /// Despawns the unit's model and removes it from tracking.
        /// </summary>
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

            // Find and remove position mapping
            var position = _modelPositions.FirstOrDefault(kvp => kvp.Value == unitId).Key;
            if (position != default)
            {
                _modelPositions.Remove(position);
            }

            // Publish event before destroying
            if (model != null)
            {
                var unit = Brain
                    .gamewideContextBrain?.GetAllActiveInstances()
                    ?.FirstOrDefault(u => u?.Id == unitId);
                Brain.Publish(new ModelDespawnedEvent(unit, unitId, position, model));

                model.SetActive(false);
                Destroy(model);
            }

            _unitModels.Remove(unitId);
            return OperationResult.Successful();
        }

        /// <summary>
        /// Despawns the unit at the specified position.
        /// </summary>
        public OperationResult DespawnUnitAtPosition(Vector2Int position)
        {
            if (!_modelPositions.TryGetValue(position, out var unitId))
            {
                return OperationResult.Failure("No model found at position");
            }

            return DespawnUnit(unitId);
        }

        /// <summary>
        /// Gets the model for a specific unit, if it exists.
        /// </summary>
        public GameObject GetModelForUnit(string unitId)
        {
            _unitModels.TryGetValue(unitId, out var model);
            return model;
        }

        /// <summary>
        /// Gets the unit ID at a specific position, if any.
        /// </summary>
        public string GetUnitIdAtPosition(Vector2Int position)
        {
            _modelPositions.TryGetValue(position, out var unitId);
            return unitId;
        }

        private OperationResult MoveExistingModel(
            CharacterInstance unit,
            GameObject model,
            Vector2Int newPosition,
            Vector3 worldPos
        )
        {
            // Remove old position mapping
            var oldPosition = _modelPositions.FirstOrDefault(kvp => kvp.Value == unit.Id).Key;
            if (oldPosition != default)
            {
                _modelPositions.Remove(oldPosition);
            }

            // Clear target position if occupied by another unit
            ClearPositionIfOccupied(newPosition);

            // Move the model
            model.transform.SetPositionAndRotation(worldPos, Quaternion.identity);

            // Update position tracking
            _modelPositions[newPosition] = unit.Id;

            // Update ownership display name in case it changed
            var ownership = model.GetComponent<UnitModelOwnership>();
            if (ownership != null)
            {
                ownership.DisplayName = unit.CharacterTemplate.DisplayName;
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
            var model = CreateModelForUnit(unit);
            if (model == null)
            {
                return OperationResult.Failure(
                    $"Failed to create model for {unit.CharacterTemplate?.DisplayName}"
                );
            }

            // Position and scale
            model.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            model.transform.localScale = Vector3.one * _brain.uiBrain.uiSettings.ModelsScale;

            // Add ownership component
            var ownership = model.AddComponent<UnitModelOwnership>();
            ownership.UnitId = unit.Id;
            ownership.DisplayName = unit.CharacterTemplate.DisplayName;
            model.name = $"{unit.CharacterTemplate.DisplayName}_Model_{unit.Id}";

            // Apply visuals (materials, blendshapes, animation)
            ApplyVisuals(unit, model);

            // Track the model
            _unitModels[unit.Id] = model;
            _modelPositions[position] = unit.Id;

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
