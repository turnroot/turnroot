using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using Turnroot.Services;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Cache Management
        public void ClearReusableTileDictionaries()
        {
            _reusableMoveTiles.Clear();
            _reusableAttackTiles.Clear();
        }

        public void InvalidateAllCaches()
        {
            ClearReusableTileDictionaries();
            _context?.InvalidateAllPathfindingParameters();
        }
        #endregion

        #region Pathfinding
        public bool GetPossibleTilesIncludingRangeNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result,
            bool includeHealRange = false
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.Unit.UnitInstance,
                "GetPossibleTilesIncludingRangeNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacterWithRange(
                _context.Unit.UnitInstance,
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

            ApplyMovementBonuses(parameters);

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

        public bool GetPossibleMoveTilesNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result
        )
        {
            result.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.Unit.UnitInstance,
                "GetPossibleMoveTilesNonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacter(
                _context.Unit.UnitInstance,
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

        public bool GetTilesForAINonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> moveTilesResult,
            Dictionary<MapGridPoint, float> attackTilesResult
        )
        {
            ClearReusableTileDictionaries();
            attackTilesResult.Clear();

            var validation = ValidationService.Instance.ValidateCharacter(
                _context.Unit.UnitInstance,
                "GetTilesForAINonAlloc"
            );
            if (!validation.IsValid)
            {
                return false;
            }

            var parametersWithRange = PathfindingParameters.FromCharacterWithRange(
                _context.Unit.UnitInstance,
                _context.mapGrid,
                start
            );

            if (parametersWithRange == null || !parametersWithRange.IsValid())
            {
                return false;
            }

            var parametersMove = parametersWithRange.Clone();
            parametersMove.IncludeRange = false;
            parametersMove.MaxRange = 0;

            var movePoints = _aStarModified.GetReachable(
                parametersMove.Graph,
                parametersMove.Start,
                parametersMove.MovementBudget,
                parametersMove.IsWalking,
                parametersMove.IsFlying,
                parametersMove.IsRiding,
                parametersMove.IsMagic,
                parametersMove.IsArmored
            );

            if (movePoints != null)
            {
                foreach (var kvp in movePoints)
                {
                    moveTilesResult[kvp.Key] = kvp.Value;
                }
            }

            var allPoints = _aStarModified.GetReachable(
                parametersWithRange.Graph,
                parametersWithRange.Start,
                parametersWithRange.MovementBudget,
                parametersWithRange.IsWalking,
                parametersWithRange.IsFlying,
                parametersWithRange.IsRiding,
                parametersWithRange.IsMagic,
                parametersWithRange.IsArmored,
                parametersWithRange.SameDirectionMultiplier,
                parametersWithRange.IncludeRange,
                parametersWithRange.MaxRange
            );

            if (allPoints != null)
            {
                foreach (var tile in allPoints)
                {
                    if (!moveTilesResult.ContainsKey(tile.Key))
                    {
                        attackTilesResult[tile.Key] = tile.Value;
                    }
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
            ClearReusableTileDictionaries();
            healTilesResult.Clear();

            if (!GetTilesForAINonAlloc(start, moveTilesResult, attackTilesResult))
            {
                return false;
            }

            using var allTilesPooled = PooledDictionary<MapGridPoint, float>.Get();
            var allTiles = allTilesPooled.Dictionary;

            if (!GetPossibleTilesIncludingRangeNonAlloc(start, allTiles, includeHealRange: true))
            {
                return false;
            }

            foreach (var tile in allTiles)
            {
                if (
                    !moveTilesResult.ContainsKey(tile.Key)
                    && !attackTilesResult.ContainsKey(tile.Key)
                )
                {
                    healTilesResult[tile.Key] = tile.Value;
                }
            }

            return true;
        }

        private void ApplyMovementBonuses(PathfindingParameters parameters)
        {
            var classData = _context.Unit.UnitInstance.CurrentClass?.ClassData;
            if (classData?.Stats.UnboundedStatBonuses == null)
            {
                return;
            }

            var bonuses = classData.Stats.UnboundedStatBonuses;
            if (bonuses != null)
            {
                var idx = bonuses.FindIndex(b =>
                    b.unboundedStatType == Characters.Stats.UnboundedStatType.Movement
                );
                if (idx >= 0)
                {
                    parameters.MovementBudget += (int)bonuses[idx].value;
                }
            }
        }
        #endregion

        #region Utility Helpers
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

        public (
            Vector2 closest,
            Vector2 furthest,
            float closestDist,
            float furthestDist
        ) FindClosestAndFurthestEnemies(List<CharacterInstance> enemies)
        {
            float furthestDistance = 0;
            float closestDistance = float.MaxValue;
            Vector2 closestEnemyPos = Vector2.zero;
            Vector2 furthestEnemyPos = Vector2.zero;

            foreach (var target in enemies)
            {
                var distance = Vector2.Distance(
                    _context.Unit.UnitInstance.MapGridPosition,
                    target.MapGridPosition
                );

                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                    furthestEnemyPos = target.MapGridPosition;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemyPos = target.MapGridPosition;
                }
            }

            return (closestEnemyPos, furthestEnemyPos, closestDistance, furthestDistance);
        }

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
                var tileCoords = tile.Key.Coordinates();
                var distanceToClosest = Vector2.Distance(tileCoords, closestEnemyPos);
                var distanceToFurthest = Vector2.Distance(tileCoords, furthestEnemyPos);

                if (distanceToClosest > closestDistance && distanceToFurthest >= furthestDistance)
                {
                    safeTiles.Add(tile.Key);
                }
            }
        }
        #endregion
    }

    public static class PathfinderHelpers
    {
        public static bool TryComputePathMovementCost(
            MapGrid mapGrid,
            PathfindingParameters parameters,
            MapGridPoint destination,
            out float totalCost
        )
        {
            totalCost = 0f;
            if (mapGrid == null || parameters == null)
            {
                return false;
            }

            var astar = new AStarModified();
            return astar.TryComputePathCost(
                mapGrid,
                parameters.Start,
                destination,
                out totalCost,
                parameters.IsWalking,
                parameters.IsFlying,
                parameters.IsRiding,
                parameters.IsMagic,
                parameters.IsArmored,
                parameters.SameDirectionMultiplier
            );
        }

        public static bool TryFindClosestPointPathCost(
            MapGrid mapGrid,
            PathfindingParameters parameters,
            IEnumerable<MapGridPoint> points,
            out float closestCost
        )
        {
            closestCost = float.MaxValue;
            if (points == null)
            {
                return false;
            }

            bool foundAny = false;
            foreach (var p in points)
            {
                if (TryComputePathMovementCost(mapGrid, parameters, p, out float c))
                {
                    foundAny = true;
                    if (c < closestCost)
                    {
                        closestCost = c;
                    }
                }
            }

            return foundAny;
        }

        public static bool TryFindClosestAllyPathCost(
            MapGrid mapGrid,
            CharacterInstance subjectUnit,
            MapGridPoint start,
            IEnumerable<CharacterInstance> allies,
            out float closestCost
        )
        {
            closestCost = float.MaxValue;
            if (mapGrid == null || subjectUnit == null)
            {
                return false;
            }

            var parameters = PathfindingParameters.FromCharacter(subjectUnit, mapGrid, start);
            if (parameters == null)
            {
                return false;
            }

            bool foundAny = false;
            foreach (var ally in allies)
            {
                if (ally == null || ally == subjectUnit)
                {
                    continue;
                }

                var dest = ally.UnitPositionToMapGridPoint(ally.MapGridPosition, mapGrid);
                if (TryComputePathMovementCost(mapGrid, parameters, dest, out float c))
                {
                    foundAny = true;
                    if (c < closestCost)
                    {
                        closestCost = c;
                    }
                }
            }

            return foundAny;
        }
    }
}
