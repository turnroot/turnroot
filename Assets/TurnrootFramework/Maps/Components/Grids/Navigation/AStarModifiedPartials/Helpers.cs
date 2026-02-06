using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;
using Utils;

namespace Turnroot.Gameplay.Maps
{
    public partial class AStarModified
    {
        #region Helper Methods
        private bool ValidateInputs(MapGrid graph, MapGridPoint start, MapGridPoint goal) =>
            graph != null && start != null && goal != null;

        private MapGridPoint GetCanonicalPoint(MapGrid graph, MapGridPoint point) =>
            graph.GetGridPoint(point.Row, point.Col) ?? point;

        private bool IsGoalReached(MapGridPoint current, MapGridPoint goal) =>
            current == goal || (current.Row == goal.Row && current.Col == goal.Col);

        private SearchContext CreateSearchContext() => new();

        private void InitializeSearch(SearchContext context, MapGridPoint start)
        {
            context.cameFrom[start] = null;
            context.costSoFar[start] = 0f;
            context.directionFromParent[start] = null;
        }

        private float CalculateStepCost(
            MapGridPoint neighbor,
            bool isWalking,
            bool isFlying,
            bool isRiding,
            bool isMagic,
            bool isArmored
        )
        {
            var grid = neighbor.ParentGrid;
            var key =
                grid != null
                    ? MapGrid.MakeMovementModeKey(isWalking, isFlying, isRiding, isMagic, isArmored)
                    : null;

            return
                grid != null
                && grid.TryGetMovementCostCache(key, out var costCache)
                && costCache?.TryGetValue(neighbor, out var cached) == true
                ? cached
                : neighbor.GetTerrainTypeCost(isWalking, isFlying, isRiding, isMagic, isArmored);
        }

        private void ProcessNeighbors(
            MapGridPoint current,
            MapGridPoint goal,
            PriorityQueue<MapGridPoint, float> frontier,
            SearchContext context,
            bool isWalking,
            bool isFlying,
            bool isRiding,
            bool isMagic,
            bool isArmored,
            float sameDirectionMultiplier
        )
        {
            current.GetNeighborsNonAlloc(_neighborsBuffer);
            foreach (var (dir, neighbor) in _neighborsBuffer)
            {
                if (context.closed.Contains(neighbor))
                {
                    continue;
                }

                float stepCost = CalculateStepCost(
                    neighbor,
                    isWalking,
                    isFlying,
                    isRiding,
                    isMagic,
                    isArmored
                );

                if (
                    context.directionFromParent.TryGetValue(current, out var parentDir)
                    && parentDir == dir
                )
                {
                    stepCost *= sameDirectionMultiplier;
                }

                float newCost = context.costSoFar[current] + stepCost;

                if (
                    !context.costSoFar.TryGetValue(neighbor, out var existingCost)
                    || newCost < existingCost
                )
                {
                    context.costSoFar[neighbor] = newCost;
                    float priority = newCost + Heuristic(neighbor, goal);
                    frontier.Enqueue(neighbor, priority);
                    context.cameFrom[neighbor] = current;
                    context.directionFromParent[neighbor] = dir;
                }
            }
        }

        private List<MapGridPoint> ReconstructPath(
            MapGridPoint goal,
            Dictionary<MapGridPoint, MapGridPoint> cameFrom
        )
        {
            var result = new List<MapGridPoint>();
            var node = goal;

            while (node != null)
            {
                result.Add(node);
                node = cameFrom.TryGetValue(node, out var parent) ? parent : null;
            }

            result.Reverse();
            return result;
        }

        private void ExpandRangeFromBoundary(
            Dictionary<MapGridPoint, float> result,
            int movementBudget,
            int maxRange
        )
        {
            using var boundaryPooled = PooledList<MapGridPoint>.Get();
            var boundaryTiles = boundaryPooled.List;

            foreach (var (tile, cost) in result)
            {
                if (Mathf.RoundToInt(cost) == movementBudget)
                {
                    boundaryTiles.Add(tile);
                }
            }

            if (boundaryTiles.Count == 0)
            {
                return;
            }

            using var expandedPooled = PooledHashSet<MapGridPoint>.Get();
            using var currentFrontierPooled = PooledHashSet<MapGridPoint>.Get();
            using var nextFrontierPooled = PooledHashSet<MapGridPoint>.Get();

            var expanded = expandedPooled.HashSet;
            var currentFrontier = currentFrontierPooled.HashSet;
            var nextFrontier = nextFrontierPooled.HashSet;

            foreach (var tile in boundaryTiles)
            {
                expanded.Add(tile);
                currentFrontier.Add(tile);
            }

            for (int step = 1; step <= maxRange; step++)
            {
                nextFrontier.Clear();

                foreach (var tile in currentFrontier)
                {
                    tile.GetNeighborsNonAlloc(_neighborsBuffer);
                    foreach (var (_, neighbor) in _neighborsBuffer)
                    {
                        if (!result.ContainsKey(neighbor) && expanded.Add(neighbor))
                        {
                            result[neighbor] = movementBudget + step;
                            nextFrontier.Add(neighbor);
                        }
                    }
                }

                if (nextFrontier.Count == 0)
                {
                    break;
                }

                (currentFrontier, nextFrontier) = (nextFrontier, currentFrontier);
            }
        }
        #endregion

        #region Search Context
        /// <summary>
        /// Encapsulates state for A* pathfinding search, using pooled collections for performance.
        /// </summary>
        private class SearchContext : System.IDisposable
        {
            private readonly PooledDictionary<MapGridPoint, MapGridPoint> _cameFromPooled;
            private readonly PooledDictionary<MapGridPoint, float> _costSoFarPooled;
            private readonly PooledDictionary<MapGridPoint, string> _directionPooled;
            private readonly PooledHashSet<MapGridPoint> _closedPooled;

            public Dictionary<MapGridPoint, MapGridPoint> cameFrom;
            public Dictionary<MapGridPoint, float> costSoFar;
            public Dictionary<MapGridPoint, string> directionFromParent;
            public HashSet<MapGridPoint> closed;

            public SearchContext()
            {
                _cameFromPooled = PooledDictionary<MapGridPoint, MapGridPoint>.Get();
                _costSoFarPooled = PooledDictionary<MapGridPoint, float>.Get();
                _directionPooled = PooledDictionary<MapGridPoint, string>.Get();
                _closedPooled = PooledHashSet<MapGridPoint>.Get();

                cameFrom = _cameFromPooled.Dictionary;
                costSoFar = _costSoFarPooled.Dictionary;
                directionFromParent = _directionPooled.Dictionary;
                closed = _closedPooled.HashSet;
            }

            public void Dispose()
            {
                _cameFromPooled.Dispose();
                _costSoFarPooled.Dispose();
                _directionPooled.Dispose();
                _closedPooled.Dispose();
            }
        }
        #endregion
    }
}
