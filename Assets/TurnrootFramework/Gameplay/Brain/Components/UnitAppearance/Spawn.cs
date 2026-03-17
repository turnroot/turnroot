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
        #region Model Tracking Helpers - Delegate to BattlePreparationObject

        private GameObject GetModelAtPosition(Vector2Int position)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep?.GetModelAtPosition(position);
        }

        private OperationResult RegisterModel(Vector2Int position, GameObject model, string unitId)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep == null ? OperationResult.Failure("No model tracking source available") : prep.RegisterModel(position, model, unitId);
        }

        private OperationResult UnregisterModelAtPosition(Vector2Int position)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep == null ? OperationResult.Failure("No model tracking source available") : prep.UnregisterModelAtPosition(position);
        }

        private OperationResult UnregisterModelForUnit(string unitId)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep == null ? OperationResult.Failure("No model tracking source available") : prep.UnregisterModelForUnit(unitId);
        }

        private string GetUnitIdAtPosition(Vector2Int position)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep?.GetUnitIdAtPosition(position);
        }

        private Vector2Int? GetPositionForUnit(string unitId)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep?.GetPositionForUnit(unitId);
        }

        private OperationResult UpdateModelPosition(Vector2Int oldPosition, Vector2Int newPosition)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep == null
                ? OperationResult.Failure("No model tracking source available")
                : prep.UpdateModelPosition(oldPosition, newPosition);
        }

        #endregion

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
                LogWarning(
                    $"SpawnUnitAtPosition: Aborting spawn for {unit.CharacterTemplate.DisplayName} - no MapGrid available"
                );
                return OperationResult.Failure("No MapGrid available for spawn");
            }

            var gridPoint = mapGrid.GetGridPoint(position.x, position.y);
            if (gridPoint == null)
            {
                LogWarning(
                    $"SpawnUnitAtPosition: Aborting spawn for {unit.CharacterTemplate.DisplayName} - invalid grid position {position}"
                );
                return OperationResult.Failure($"Invalid spawn grid point: {position}");
            }

            try
            {
                if (prebattle)
                {
                    unit.MapGridPosition = position;
                }
                else
                {
                    unit.WasSpawnedDuringBattle = true;
                }
            }
            catch (System.Exception ex)
            {
                LogWarning(
                    $"SpawnUnitAtPosition: Failed setting instance state for {unit.Id}: {ex.Message}"
                );
            }

            // Recompute exact world position using validated MapGrid
            worldPos = mapGrid.GetTerrainAdjustedWorldPosition(position);

            // Check if model already exists (queries current source of truth)
            var existingModel = GetModelForUnit(unit.Id);
            if (existingModel != null)
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
            return GetModelForUnit(unit.Id) != null
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

            // Get model from current source of truth
            var model = GetModelForUnit(unitId);
            if (model == null)
            {
                return OperationResult.Failure($"No model found for unit {unitId}");
            }

            var position = GetPositionForUnit(unitId);

            var unit = Brain
                .gamewideContextBrain.GetAllActiveInstances()
                .FirstOrDefault(u => u != null && u.Id == unitId);

            if (unit != null)
            {
                ClearWeaponFromUnit(unit);
                ClearMountFromUnit(unit);
            }

            Brain.Publish(new ModelDespawnedEvent(unit, unitId, position ?? default, model));

            model.SetActive(false);
            Destroy(model);

            // Unregister from current source of truth
            UnregisterModelForUnit(unitId);

            return OperationResult.Successful();
        }

        public OperationResult DespawnUnitAtPosition(Vector2Int position)
        {
            var unitId = GetUnitIdAtPosition(position);
            if (string.IsNullOrEmpty(unitId))
            {
                LogWarning("No model found at position");
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
            var oldPosition = GetPositionForUnit(unit.Id);
            if (oldPosition.HasValue)
            {
                // Update position tracking in current source of truth
                var updateResult = UpdateModelPosition(oldPosition.Value, newPosition);
                if (!updateResult.Success)
                {
                    $"MoveExistingModel: Failed to update model position: {updateResult.ErrorMessage}".LogWarning();
                }
            }
            else
            {
                // Model wasn't registered, register it now
                RegisterModel(newPosition, model, unit.Id);
            }

            ClearPositionIfOccupied(newPosition);

            var facingRotation = GetInitialFacingRotation(unit, worldPos);
            model.transform.SetPositionAndRotation(worldPos, facingRotation);

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
            var facingRotation = GetInitialFacingRotation(unit, worldPos);
            root.transform.SetPositionAndRotation(worldPos, facingRotation);
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

            // Register model
            var registerResult = RegisterModel(position, model, unit.Id);
            if (!registerResult.Success)
            {
                Destroy(model);
                return OperationResult.Failure(
                    $"Failed to register model: {registerResult.ErrorMessage}"
                );
            }

            ApplyVisuals(unit, model);

            AttachWeaponToUnit(unit, model);

            AttachShieldToUnit(unit, model);

            if (ShouldUnitBeMounted(unit))
            {
                AttachMountToUnit(unit, model);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev diagnostic: confirm model created and placed where expected.
            $"CreateAndPlaceModel: Created model '{model.name}' for unit {unit.Id} at grid {position}, world {worldPos}".LogInfo(
                "UnitAppearanceBrain"
            );
#endif

            Brain.Publish(new ModelSpawnedEvent(unit, unit.Id, position, model));
            return OperationResult.Successful();
        }

        private void ClearPositionIfOccupied(Vector2Int position)
        {
            var occupyingUnitId = GetUnitIdAtPosition(position);
            if (!string.IsNullOrEmpty(occupyingUnitId))
            {
                // Another unit is at this position - despawn it
                DespawnUnit(occupyingUnitId);
            }
        }

        /// <summary>
        /// Returns a Y-axis rotation snapped to the nearest 90° that faces this unit toward
        /// the nearest already-spawned model on the opposing team.
        /// Falls back to <see cref="Quaternion.identity"/> when no opponents are visible yet.
        /// </summary>
        private Quaternion GetInitialFacingRotation(CharacterInstance unit, Vector3 unitWorldPos)
        {
            bool isEnemy = unit.CharacterTemplate?.IsEnemyOrNPC ?? false;

            var allInstances = Brain.gamewideContextBrain?.GetAllActiveInstances();
            if (allInstances == null)
            {
                return Quaternion.identity;
            }

            Vector3? nearestOpponentPos = null;
            float nearestSqrDist = float.MaxValue;

            foreach (var other in allInstances)
            {
                if (other == null || other.Id == unit.Id)
                {
                    continue;
                }

                bool otherIsEnemy = other.CharacterTemplate?.IsEnemyOrNPC ?? false;
                if (otherIsEnemy == isEnemy)
                {
                    continue; // same team
                }

                var otherModel = GetModelForUnit(other.Id);
                if (otherModel == null)
                {
                    continue; // not yet spawned
                }

                var otherPos = otherModel.transform.position;
                float sqrDist = (otherPos - unitWorldPos).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearestOpponentPos = otherPos;
                }
            }

            if (nearestOpponentPos == null)
            {
                return Quaternion.identity;
            }

            var direction = nearestOpponentPos.Value - unitWorldPos;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float snapped = Mathf.Round(angle / 90f) * 90f;
            return Quaternion.Euler(0f, snapped, 0f);
        }
    }
}
