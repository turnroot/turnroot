using System;
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

        // Track the map state version to invalidate caches when the map mutates (terrain/feature/occupancy changes)
        private int _lastSeenMapVersion = -1;

        // Pathfinding cache keyed by turn and parameter key to avoid recalculating
        private int _lastCachedTurnNumber = -1;
        private readonly Dictionary<string, Dictionary<MapGridPoint, float>> _reachabilityCache =
            new();

        private static string BuildPathCacheKey(PathfindingParameters p)
        {
            // Include start, movement budget and flags relevant to reachability
            return $"{p.Start.Col}_{p.Start.Row}_mb{p.MovementBudget}_w{p.IsWalking}_f{p.IsFlying}_r{p.IncludeRange}_max{p.MaxRange}";
        }

        /// <summary>
        /// Gets references to the reusable tile dictionaries for callers that need them.
        /// WARNING: These dictionaries are reused between calls. Copy values if you need to persist them.
        /// </summary>
        public (
            Dictionary<MapGridPoint, float> MoveTiles,
            Dictionary<MapGridPoint, float> AttackTiles
        ) GetReusableTileDictionaries() => (_reusableMoveTiles, _reusableAttackTiles);

        /// <summary>
        /// Clears internal reusable tile dictionaries to avoid stale data when the active unit changes or is removed.
        /// </summary>
        public void ClearReusableTileDictionaries()
        {
            _reusableMoveTiles.Clear();
            _reusableAttackTiles.Clear();
        }

        /// <summary>
        /// Populates the provided dictionary with possible tiles that the unit can move to, including the range of its attacks.
        /// </summary>
        public bool GetPossibleTilesIncludingRangeNonAlloc(
            MapGridPoint start,
            Dictionary<MapGridPoint, float> result,
            bool includeHealRange = false
        )
        {
            // If map changed, clear reusable caches to ensure up-to-date tiles
            if (_context?.mapGrid != null && _context.mapGrid.StateVersion != _lastSeenMapVersion)
            {
                ClearReusableTileDictionaries();
                _lastSeenMapVersion = _context.mapGrid.StateVersion;
            }

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

            // Apply movement bonuses
            var classData = _context.Unit.UnitInstance.CurrentClass.ClassData;
            var movementBonusMod = classData.Stats.UnboundedStatBonuses?.Find(b =>
                b.unboundedStatType == Characters.Stats.UnboundedStatType.Movement
            );
            if (movementBonusMod.HasValue)
            {
                parameters.MovementBudget += (int)movementBonusMod.Value.value;
            }

            // Turn-level caching: clear if turn changed
            int currentTurn = _context?.Brain?.CurrentTurnNumber ?? -1;
            if (currentTurn != _lastCachedTurnNumber)
            {
                _reachabilityCache.Clear();
                _lastCachedTurnNumber = currentTurn;
            }

            var cacheKey = BuildPathCacheKey(parameters);
            if (_reachabilityCache.TryGetValue(cacheKey, out var cached))
            {
                foreach (var kvp in cached)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            else
            {
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
                    var copy = new Dictionary<MapGridPoint, float>(points);
                    _reachabilityCache[cacheKey] = copy;

                    foreach (var kvp in copy)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
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
            // If map changed, clear reusable caches to ensure up-to-date tiles
            if (_context?.mapGrid != null && _context.mapGrid.StateVersion != _lastSeenMapVersion)
            {
                ClearReusableTileDictionaries();
                _lastSeenMapVersion = _context.mapGrid.StateVersion;
            }

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

            // Turn-level caching: clear if turn changed
            int currentTurn = _context?.Brain?.CurrentTurnNumber ?? -1;
            if (currentTurn != _lastCachedTurnNumber)
            {
                _reachabilityCache.Clear();
                _lastCachedTurnNumber = currentTurn;
            }

            var cacheKey =
                $"{parameters.Start.Col}_{parameters.Start.Row}_mb{parameters.MovementBudget}_w{parameters.IsWalking}_f{parameters.IsFlying}_rfalse_max0";
            if (_reachabilityCache.TryGetValue(cacheKey, out var cached))
            {
                foreach (var kvp in cached)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            else
            {
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
                    var copy = new Dictionary<MapGridPoint, float>(points);
                    _reachabilityCache[cacheKey] = copy;

                    foreach (var kvp in copy)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
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
            // If map changed, clear reusable caches to ensure up-to-date tiles
            if (_context?.mapGrid != null && _context.mapGrid.StateVersion != _lastSeenMapVersion)
            {
                ClearReusableTileDictionaries();
                _lastSeenMapVersion = _context.mapGrid.StateVersion;
            }

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
            // If map changed, clear reusable caches to ensure up-to-date tiles
            if (_context?.mapGrid != null && _context.mapGrid.StateVersion != _lastSeenMapVersion)
            {
                ClearReusableTileDictionaries();
                _lastSeenMapVersion = _context.mapGrid.StateVersion;
            }

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
                $"[Distance Check] MyPosition: {_context.Unit.UnitInstance.MapGridPosition}, "
                    + $"TargetCount: {enemies.Count}"
            );
            foreach (var target in enemies)
            {
#if UNITY_EDITOR
                Debug.Log($"  Target {target.Id}: Position={target.MapGridPosition}");
#endif
            }
            float furthestDistance = 0;
            float closestDistance = float.MaxValue;
            Vector2 closestEnemyPos = Vector2.zero;
            Vector2 furthestEnemyPos = Vector2.zero;

            foreach (var target in enemies)
            {
                var targetPosition = target.MapGridPosition;
                var distance = Vector2.Distance(
                    _context.Unit.UnitInstance.MapGridPosition,
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

        /// <summary>
        /// Utility helpers for computing path-cost-based distances using the A* search.
        /// </summary>
        public static class PathfinderHelpers
        {
            /// <summary>
            /// Computes the movement-cost-aware path cost from parameters.Start to destination.
            /// Returns true and sets totalCost when a path exists; returns false otherwise.
            /// </summary>
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
                var path = astar.AStarSearch(
                    mapGrid,
                    parameters.Start,
                    destination,
                    parameters.IsWalking,
                    parameters.IsFlying,
                    parameters.IsRiding,
                    parameters.IsMagic,
                    parameters.IsArmored,
                    parameters.SameDirectionMultiplier
                );

                if (path == null || path.Count == 0)
                {
                    return false;
                }

                float sum = 0f;
                string prevDir = null;
                for (int i = 1; i < path.Count; i++)
                {
                    var from = path[i - 1];
                    var to = path[i];
                    int dRow = to.Row - from.Row;
                    int dCol = to.Col - from.Col;
                    string dir =
                        dRow == -1 && dCol == 0 ? "N"
                        : dRow == -1 && dCol == 1 ? "NE"
                        : dRow == 0 && dCol == 1 ? "E"
                        : dRow == 1 && dCol == 1 ? "SE"
                        : dRow == 1 && dCol == 0 ? "S"
                        : dRow == 1 && dCol == -1 ? "SW"
                        : dRow == 0 && dCol == -1 ? "W"
                        : dRow == -1 && dCol == -1 ? "NW"
                        : null;

                    float stepCost = to.GetTerrainTypeCost(
                        parameters.IsWalking,
                        parameters.IsFlying,
                        parameters.IsRiding,
                        parameters.IsMagic,
                        parameters.IsArmored
                    );

                    if (prevDir != null && prevDir == dir)
                    {
                        stepCost *= parameters.SameDirectionMultiplier;
                    }

                    sum += stepCost;
                    prevDir = dir;
                }

                totalCost = sum;
                return true;
            }

            /// <summary>
            /// Finds the lowest path-cost to any point in the provided sequence of MapGridPoints.
            /// </summary>
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

            /// <summary>
            /// Convenience wrapper to find the closest path-cost to any of the provided characters (skips the subject unit).
            /// </summary>
            public static bool TryFindClosestAllyPathCost(
                MapGrid mapGrid,
                CharacterInstance subjectUnit,
                MapGridPoint start,
                IEnumerable<CharacterInstance> allies,
                out float closestCost
            )
            {
                if (mapGrid == null || subjectUnit == null)
                {
                    closestCost = float.MaxValue;
                    return false;
                }

                var parameters = PathfindingParameters.FromCharacter(subjectUnit, mapGrid, start);
                if (parameters == null)
                {
                    closestCost = float.MaxValue;
                    return false;
                }

                var points = new List<MapGridPoint>();
                foreach (var a in allies)
                {
                    if (a == null || a == subjectUnit)
                    {
                        continue;
                    }
                    points.Add(a.UnitPositionToMapGridPoint(a.MapGridPosition, mapGrid));
                }

                return TryFindClosestPointPathCost(mapGrid, parameters, points, out closestCost);
            }
        }
    } // end partial class BattleContextAIHelper

    /// <summary>
    /// Utility helpers for computing path-cost-based distances using the A* search.
    /// </summary>
    public static class PathfinderHelpers
    {
        /// <summary>
        /// Computes the movement-cost-aware path cost from parameters.Start to destination.
        /// Returns true and sets totalCost when a path exists; returns false otherwise.
        /// </summary>
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
            if (
                !astar.TryComputePathCost(
                    mapGrid,
                    parameters.Start,
                    destination,
                    out float computedCost,
                    parameters.IsWalking,
                    parameters.IsFlying,
                    parameters.IsRiding,
                    parameters.IsMagic,
                    parameters.IsArmored,
                    parameters.SameDirectionMultiplier
                )
            )
            {
                return false;
            }

            totalCost = computedCost;
            return true;
        }

        /// <summary>
        /// Finds the lowest path-cost to any point in the provided sequence of MapGridPoints.
        /// </summary>
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

        /// <summary>
        /// Convenience wrapper to find the closest path-cost to any of the provided characters (skips the subject unit).
        /// </summary>
        public static bool TryFindClosestAllyPathCost(
            MapGrid mapGrid,
            CharacterInstance subjectUnit,
            MapGridPoint start,
            IEnumerable<CharacterInstance> allies,
            out float closestCost
        )
        {
            if (mapGrid == null || subjectUnit == null)
            {
                closestCost = float.MaxValue;
                return false;
            }

            var parameters = PathfindingParameters.FromCharacter(subjectUnit, mapGrid, start);
            if (parameters == null)
            {
                closestCost = float.MaxValue;
                return false;
            }

            bool foundAny = false;
            closestCost = float.MaxValue;

            foreach (var a in allies)
            {
                if (a == null || a == subjectUnit)
                {
                    continue;
                }

                var dest = a.UnitPositionToMapGridPoint(a.MapGridPosition, mapGrid);
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
