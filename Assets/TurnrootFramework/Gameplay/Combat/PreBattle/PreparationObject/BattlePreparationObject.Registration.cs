using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        #region Model Registration (Low-Level)

        /// <summary>
        /// Register a model at a position. Called by UnitAppearanceBrain after spawning.
        /// </summary>
        public OperationResult RegisterModel(Vector2Int position, GameObject model, string unitId)
        {
            var validation = OperationResultGuards.RequireNotNull(model, nameof(model));
            if (!validation.Success)
            {
                return validation;
            }

            if (string.IsNullOrEmpty(unitId))
            {
                return OperationResult.Failure("Unit ID cannot be null or empty");
            }

            // Clear any existing model at this position
            if (_positionToModel.ContainsKey(position))
            {
                var existingUnitId = GetUnitIdAtPosition(position);
                $"RegisterModel: Replacing existing model at {position} (was unitId={existingUnitId})".LogWarning();
                UnregisterModelAtPosition(position);
            }

            // Clear any existing model for this unit ID
            if (_unitIdToPosition.ContainsKey(unitId))
            {
                var oldPos = _unitIdToPosition[unitId];
                $"RegisterModel: Unit {unitId} moving from {oldPos} to {position}".LogInfo();
                UnregisterModelAtPosition(oldPos);
            }

            _positionToModel[position] = model;
            _positionToUnitId[position] = unitId;
            _unitIdToPosition[unitId] = position;

            return OperationResult.Successful();
        }

        /// <summary>
        /// Unregister model at a position. Called before despawning.
        /// </summary>
        public OperationResult UnregisterModelAtPosition(Vector2Int position)
        {
            if (!_positionToModel.ContainsKey(position))
            {
                return OperationResult.Successful(); // Already unregistered
            }

            var unitId = GetUnitIdAtPosition(position);
            if (!string.IsNullOrEmpty(unitId))
            {
                _unitIdToPosition.Remove(unitId);
            }

            _positionToModel.Remove(position);
            _positionToUnitId.Remove(position);

            return OperationResult.Successful();
        }

        /// <summary>
        /// Unregister model by unit ID. Called before despawning.
        /// </summary>
        public OperationResult UnregisterModelForUnit(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
            {
                return OperationResult.Failure("Unit ID cannot be null or empty");
            }

            if (!_unitIdToPosition.TryGetValue(unitId, out var position))
            {
                return OperationResult.Successful(); // Already unregistered
            }

            return UnregisterModelAtPosition(position);
        }

        /// <summary>
        /// Clear all model tracking. Called when transitioning away from pre-battle.
        /// </summary>
        public void ClearAllModelTracking()
        {
            _positionToModel.Clear();
            _positionToUnitId.Clear();
            _unitIdToPosition.Clear();
        }

        /// <summary>
        /// Update model position when a unit moves. Updates all tracking dictionaries.
        /// </summary>
        public OperationResult UpdateModelPosition(Vector2Int oldPosition, Vector2Int newPosition)
        {
            if (!_positionToModel.TryGetValue(oldPosition, out var model))
            {
                return OperationResult.Failure($"No model found at {oldPosition}");
            }

            var unitId = GetUnitIdAtPosition(oldPosition);
            if (string.IsNullOrEmpty(unitId))
            {
                return OperationResult.Failure($"No unit ID tracked at {oldPosition}");
            }

            // Unregister from old position
            UnregisterModelAtPosition(oldPosition);

            // Register at new position
            return RegisterModel(newPosition, model, unitId);
        }

        /// <summary>
        /// Swap models between two positions. Updates all tracking dictionaries.
        /// </summary>
        public OperationResult SwapModelPositions(Vector2Int posA, Vector2Int posB)
        {
            var modelA = GetModelAtPosition(posA);
            var modelB = GetModelAtPosition(posB);
            var unitIdA = GetUnitIdAtPosition(posA);
            var unitIdB = GetUnitIdAtPosition(posB);

            if (modelA == null || modelB == null)
            {
                return OperationResult.Failure("Both positions must have models to swap");
            }

            if (string.IsNullOrEmpty(unitIdA) || string.IsNullOrEmpty(unitIdB))
            {
                return OperationResult.Failure("Both positions must have tracked unit IDs");
            }

            // Swap the registrations
            UnregisterModelAtPosition(posA);
            UnregisterModelAtPosition(posB);

            RegisterModel(posA, modelB, unitIdB);
            RegisterModel(posB, modelA, unitIdA);

            return OperationResult.Successful();
        }

        #endregion
    }
}
