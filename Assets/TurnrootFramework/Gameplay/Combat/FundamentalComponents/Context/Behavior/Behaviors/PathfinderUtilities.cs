using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Maps;
using Turnroot.Services;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Pathfinding Helpers

        /// <summary>
        /// Gets references to the reusable tile dictionaries for callers that need them.
        /// WARNING: These dictionaries are reused between calls. Copy values if you need to persist them.
        /// </summary>
        public (
            Dictionary<MapGridPoint, float> MoveTiles,
            Dictionary<MapGridPoint, float> AttackTiles
        ) GetReusableTileDictionaries() => (_reusableMoveTiles, _reusableAttackTiles);

        /// <summary>
        /// Populates the provided dictionary with possible tiles that the unit can move to, including the range of its attacks.
        /// </summary>
        public bool GetPossibleTilesIncludingRangeNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result,
            bool includeHealRange = false
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.UnitInstance,
                "GetPossibleTilesIncludingRangeNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacterWithRange(
                _context.UnitInstance,
                _context.mapGrid,
                start
            );
            if (includeHealRange)
            {
                parameters.IncludeHealRange = true;
            }
            if (parameters == null || !parameters.IsValid())
            {
                return false;
            }

            // Apply movement bonuses
            var classData = _context.UnitInstance.CurrentClass.ClassData;
            var movementBonusMod = classData.Stats.UnboundedStatBonuses?.Find(b =>
                b.unboundedStatType == Characters.Stats.UnboundedStatType.Movement
            );
            if (movementBonusMod.HasValue)
            {
                parameters.MovementBudget += (int)movementBonusMod.Value.value;
            }

            var points = _aStarModified.GetReachable(
                parameters.Graph,
                parameters.Start,
                parameters.MovementBudget,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored,
                parameters.SameDirectionMultiplier,
                parameters.IncludeRange,
                parameters.MaxRange
            );

            if (points != null)
            {
                foreach (var kvp in points)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return true;
        }

        /// <summary>
        /// Populates the provided dictionary with all tiles the unit can move to (excluding attack-only range).
        /// </summary>
        public bool GetPossibleMoveTilesNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.UnitInstance,
                "GetPossibleMoveTilesNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacter(
                _context.UnitInstance,
                _context.mapGrid,
                start
            );

            if (parameters == null || !parameters.IsValid())
            {
                return false;
            }

            var points = _aStarModified.GetReachable(
                parameters.Graph,
                parameters.Start,
                parameters.MovementBudget,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored
            );

            if (points != null)
            {
                foreach (var kvp in points)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes both movement and attack-only tiles for AI decision making.
        /// </summary>
        public bool GetTilesForAINonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> moveTilesResult,
            Dictionary<MapGridPoint, float> attackTilesResult
        )
        {
            attackTilesResult.Clear();

            if (!GetPossibleMoveTilesNonAlloc(start, moveTilesResult))
            {
                return false;
            }

            using var allTilesPooled = PooledDictionary<MapGridPoint, float>.Get();
            var allTiles = allTilesPooled.Dictionary;

            if (!GetPossibleTilesIncludingRangeNonAlloc(start, allTiles))
            {
                return false;
            }

            foreach (var tile in allTiles)
            {
                if (!moveTilesResult.TryGetValue(tile.Key, out _))
                {
                    attackTilesResult[tile.Key] = tile.Value;
                }
            }

            return true;
        }

        public bool GetTilesForAIWithHealNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> moveTilesResult,
            Dictionary<MapGridPoint, float> attackTilesResult,
            Dictionary<MapGridPoint, float> healTilesResult
        )
        {
            healTilesResult.Clear();

            if (!GetTilesForAINonAlloc(start, moveTilesResult, attackTilesResult))
            {
                return false;
            }

            using var allTilesPooled = PooledDictionary<MapGridPoint, float>.Get();
            var allTiles = allTilesPooled.Dictionary;

            if (!GetPossibleTilesIncludingRangeNonAlloc(start, allTiles))
            {
                return false;
            }

            foreach (var tile in allTiles)
            {
                if (
                    !moveTilesResult.TryGetValue(tile.Key, out _)
                    && !attackTilesResult.TryGetValue(tile.Key, out _)
                )
                {
                    healTilesResult[tile.Key] = tile.Value;
                }
            }

            return true;
        }

        #endregion

        #region Utility Helpers

        /// <summary>
        /// Finds the closest point from a list of points relative to a starting point.
        /// </summary>
        public MapGridPoint FindClosestFromListOfPoints(
            List<MapGridPoint> points,
            MapGridPoint start
        )
        {
            var currentDistance = float.MaxValue;
            var closestPoint = new MapGridPoint();
            foreach (var point in points)
            {
                var distance = Vector2.Distance(start.Coordinates(), point.Coordinates());
                if (distance < currentDistance)
                {
                    currentDistance = distance;
                    closestPoint = point;
                }
            }
            return closestPoint;
        }

        /// <summary>
        /// Finds the closest and furthest enemies from the unit's current position.
        /// Useful for defensive and tactical calculations.
        /// </summary>
        public (
            Vector2 closest,
            Vector2 furthest,
            float closestDist,
            float furthestDist
        ) FindClosestAndFurthestEnemies(List<CharacterInstance> enemies)
        {
            Debug.Log(
                $"[Distance Check] MyPosition: {_context.UnitInstance.MapGridPosition}, "
                    + $"TargetCount: {enemies.Count}"
            );
            foreach (var target in enemies)
            {
                Debug.Log($"  Target {target.Id}: Position={target.MapGridPosition}");
            }
            float furthestDistance = 0;
            float closestDistance = float.MaxValue;
            Vector2 closestEnemyPos = Vector2.zero;
            Vector2 furthestEnemyPos = Vector2.zero;

            foreach (var target in enemies)
            {
                var targetPosition = target.MapGridPosition;
                var distance = Vector2.Distance(
                    _context.UnitInstance.MapGridPosition,
                    targetPosition
                );

                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                    furthestEnemyPos = targetPosition;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemyPos = targetPosition;
                }
            }

            return (closestEnemyPos, furthestEnemyPos, closestDistance, furthestDistance);
        }

        /// <summary>
        /// Filters tiles that increase distance from enemies (useful for retreat logic).
        /// </summary>
        public void FilterSafeTilesNonAlloc(
            Dictionary<MapGridPoint, float> moveTiles,
            Vector2 closestEnemyPos,
            Vector2 furthestEnemyPos,
            float closestDistance,
            float furthestDistance,
            List<MapGridPoint> safeTiles
        )
        {
            safeTiles.Clear();

            foreach (var tile in moveTiles)
            {
                var tilePosition = tile.Key;
                var tileCoords = tilePosition.Coordinates();
                var distanceToClosest = Vector2.Distance(tileCoords, closestEnemyPos);
                var distanceToFurthest = Vector2.Distance(tileCoords, furthestEnemyPos);

                if (distanceToClosest > closestDistance && distanceToFurthest >= furthestDistance)
                {
                    safeTiles.Add(tilePosition);
                }
            }
        }

        #endregion
    }
}
