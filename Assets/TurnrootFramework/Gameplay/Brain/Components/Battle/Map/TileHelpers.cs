using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        private OperationResult CalculateValidTiles(CharacterInstance unit)
        {
            if (unit == null || BattleContext?.mapGrid == null)
            {
                return OperationResult.Failure("No unit or BattleContext");
            }

            _validMoveTiles.Clear();
            _validAttackTiles.Clear();
            _aiHelper = BattleContext.AIHelper;

            var currentPos = unit.UnitPositionToMapGridPoint(
                unit.MapGridPosition,
                BattleContext.mapGrid
            );
            bool canHeal = unit.CurrentClass?.ClassData?.Identity?.CanHeal ?? false;

            bool success;
            if (canHeal)
            {
                var healTilesTemp = new Dictionary<MapGridPoint, float>();
                success = _aiHelper.GetTilesForAIWithHealNonAlloc(
                    currentPos,
                    _validMoveTiles,
                    _validAttackTiles,
                    healTilesTemp
                );
            }
            else
            {
                success = _aiHelper.GetTilesForAINonAlloc(
                    currentPos,
                    _validMoveTiles,
                    _validAttackTiles
                );
            }

            if (!success)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"BattleInputControllerBrain: Failed to calculate tiles for {unit.CharacterTemplate.DisplayName}"
                );
#endif
                return OperationResult.Failure(
                    $"Failed to calculate tiles for unit {unit.CharacterTemplate.DisplayName}"
                );
            }

            return OperationResult.SuccessResult();
        }

        // TODO: Implement damage preview system (priorities.md Phase 4.1) - CalculateAttackPreview with hit%/crit%/counters
        // TODO: Implement movement path preview (priorities.md Phase 4.2) - CalculateMovementPath with A* pathfinding

        public bool ValidateTileSelection(MapGridPoint point)
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            return currentState switch
            {
                PlayerTurnStates.MoveActionChosenChoosingDestination => _validMoveTiles.ContainsKey(
                    point
                ),
                PlayerTurnStates.AttackActionChosenChoosingTarget => _validAttackTiles.ContainsKey(
                    point
                ),
                _ => false,
            };
            // TODO: Comprehensive action validation (weapons, skills, rescue/trade requirements, audio/visual feedback)
        }

        private CharacterInstance GetUnitAtPosition(MapGridPoint position)
        {
            var cache = BattleContext.GetCurrentUnitPositions();
            return cache.TryGetValue(position.CoordinatesInt, out var unit) ? unit : null;
        }

        private bool IsPositionWithinTraversableArea(Vector2Int position, MapGrid mapGrid)
        {
            if (mapGrid == null)
            {
                return false;
            }

            var corners = mapGrid.TraversableAreaCorners;

            // Fallback to full grid if no traversable area defined
            if (corners == null || corners.Length != 4)
            {
                return position.x >= 0
                    && position.x < mapGrid.GridWidth
                    && position.y >= 0
                    && position.y < mapGrid.GridHeight;
            }

            // Calculate traversable area bounds
            int minX = int.MaxValue,
                maxX = int.MinValue;
            int minY = int.MaxValue,
                maxY = int.MinValue;

            foreach (var corner in corners)
            {
                if (corner.x < minX)
                {
                    minX = corner.x;
                }

                if (corner.x > maxX)
                {
                    maxX = corner.x;
                }

                if (corner.y < minY)
                {
                    minY = corner.y;
                }

                if (corner.y > maxY)
                {
                    maxY = corner.y;
                }
            }

            return position.x >= minX
                && position.x <= maxX
                && position.y >= minY
                && position.y <= maxY;
        }

        private Vector2Int GetGridMovementFromDirection(Vector2 direction)
        {
            float threshold = _cachedIsKeyboard ? 0.1f : 0.3f;
            Vector2Int gridMovement = Vector2Int.zero;

            if (direction.x > threshold)
            {
                gridMovement.x = 1;
            }
            else if (direction.x < -threshold)
            {
                gridMovement.x = -1;
            }

            if (direction.y > threshold)
            {
                gridMovement.y = 1;
            }
            else if (direction.y < -threshold)
            {
                gridMovement.y = -1;
            }

            return gridMovement;
        }
    }
}
