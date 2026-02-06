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

            // SpawnUnitAtPosition already handles errors and returns OperationResult
            return SpawnUnitAtPosition(unit, position, prebattle);
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

            // Find and remove position mapping
            var position = _modelPositions.FirstOrDefault(kvp => kvp.Value == unitId).Key;
            if (position != default)
            {
                _modelPositions.Remove(position);
            }

            if (model != null)
            {
                var unit = Brain
                    .gamewideContextBrain?.GetAllActiveInstances()
                    ?.FirstOrDefault(u => u?.Id == unitId);

                // Clear weapon reference
                if (unit != null)
                {
                    ClearWeaponFromUnit(unit);
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
            return !_modelPositions.TryGetValue(position, out var unitId)
                ? OperationResult.Failure("No model found at position")
                : DespawnUnit(unitId);
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

            var ownership = model.GetComponent<UnitModelOwnership>();
            if (ownership != null)
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

            model.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            model.transform.localScale = Vector3.one * _brain.uiBrain.uiSettings.ModelsScale;

            var ownership = model.AddComponent<UnitModelOwnership>();
            ownership.UnitId = unit.Id;
            ownership.DisplayName = unit.CharacterTemplate.DisplayName;
            model.name = $"{unit.CharacterTemplate.DisplayName}_Model_{unit.Id}";

            ApplyVisuals(unit, model);

            _unitModels[unit.Id] = model;
            _modelPositions[position] = unit.Id;

            AttachWeaponToUnit(unit, model);

            AttachShieldToUnit(unit, model);

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
