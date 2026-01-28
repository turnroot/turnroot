using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;
using Utils;

namespace Turnroot.Gameplay.Maps
{
    public class AStarModified
    {
        private readonly Dictionary<string, MapGridPoint> _neighborsBuffer = new(8);

        private float Heuristic(MapGridPoint a, MapGridPoint b) =>
            Mathf.Abs(a.Row - b.Row) + Mathf.Abs(a.Col - b.Col);

        #region A* Search
        public List<MapGridPoint> AStarSearch(
            MapGrid graph,
            MapGridPoint start,
            MapGridPoint goal,
            bool isWalking = true,
            bool isFlying = false,
            bool isRiding = false,
            bool isMagic = false,
            bool isArmored = false,
            float sameDirectionMultiplier = 0.95f
        )
        {
            if (!ValidateInputs(graph, start, goal))
            {
                return new List<MapGridPoint>();
            }

            var canonicalStart = GetCanonicalPoint(graph, start);
            var canonicalGoal = GetCanonicalPoint(graph, goal);

            var frontier = new PriorityQueue<MapGridPoint, float>();
            frontier.Enqueue(canonicalStart, 0f);

            using var context = CreateSearchContext();
            InitializeSearch(context, canonicalStart);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (IsGoalReached(current, canonicalGoal))
                {
                    return ReconstructPath(current, context.cameFrom);
                }

                if (!context.closed.Add(current))
                {
                    continue;
                }

                ProcessNeighbors(
                    current,
                    canonicalGoal,
                    frontier,
                    context,
                    isWalking,
                    isFlying,
                    isRiding,
                    isMagic,
                    isArmored,
                    sameDirectionMultiplier
                );
            }

            return new List<MapGridPoint>();
        }

        public bool TryComputePathCost(
            MapGrid graph,
            MapGridPoint start,
            MapGridPoint goal,
            out float totalCost,
            bool isWalking = true,
            bool isFlying = false,
            bool isRiding = false,
            bool isMagic = false,
            bool isArmored = false,
            float sameDirectionMultiplier = 0.95f
        )
        {
            totalCost = 0f;
            if (!ValidateInputs(graph, start, goal))
            {
                return false;
            }

            var canonicalStart = GetCanonicalPoint(graph, start);
            var canonicalGoal = GetCanonicalPoint(graph, goal);

            var frontier = new PriorityQueue<MapGridPoint, float>();
            frontier.Enqueue(canonicalStart, 0f);

            using var context = CreateSearchContext();
            InitializeSearch(context, canonicalStart);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (IsGoalReached(current, canonicalGoal))
                {
                    return context.costSoFar.TryGetValue(current, out totalCost);
                }

                if (!context.closed.Add(current))
                {
                    continue;
                }

                ProcessNeighbors(
                    current,
                    canonicalGoal,
                    frontier,
                    context,
                    isWalking,
                    isFlying,
                    isRiding,
                    isMagic,
                    isArmored,
                    sameDirectionMultiplier
                );
            }

            return false;
        }
        #endregion

        #region Reachable Tiles
        public Dictionary<MapGridPoint, float> GetReachable(
            MapGrid graph,
            MapGridPoint start,
            int movementBudget,
            bool isWalking = true,
            bool isFlying = false,
            bool isRiding = false,
            bool isMagic = false,
            bool isArmored = false,
            float sameDirectionMultiplier = 0.95f,
            bool includeRange = false,
            int maxRange = 0
        )
        {
            var result = new Dictionary<MapGridPoint, float>();
            if (graph == null || start == null)
            {
                return result;
            }

            var canonicalStart = GetCanonicalPoint(graph, start);
            var frontier = new PriorityQueue<MapGridPoint, float>();
            frontier.Enqueue(canonicalStart, 0f);

            using var costSoFarPooled = PooledDictionary<MapGridPoint, float>.Get();
            using var directionPooled = PooledDictionary<MapGridPoint, string>.Get();
            var costSoFar = costSoFarPooled.Dictionary;
            var directionFromParent = directionPooled.Dictionary;

            costSoFar[canonicalStart] = 0f;
            directionFromParent[canonicalStart] = null;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                float currentCost = costSoFar[current];

                if (currentCost > movementBudget)
                {
                    continue;
                }

                result[current] = currentCost;

                current.GetNeighborsNonAlloc(_neighborsBuffer);
                foreach (var (dir, neighbor) in _neighborsBuffer)
                {
                    if (neighbor.IsOccupied)
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
                        directionFromParent.TryGetValue(current, out var parentDir)
                        && parentDir == dir
                    )
                    {
                        stepCost *= sameDirectionMultiplier;
                    }

                    float newCost = currentCost + stepCost;
                    if (newCost > movementBudget)
                    {
                        continue;
                    }

                    if (
                        !costSoFar.TryGetValue(neighbor, out var existingCost)
                        || newCost < existingCost
                    )
                    {
                        costSoFar[neighbor] = newCost;
                        directionFromParent[neighbor] = dir;
                        frontier.Enqueue(neighbor, newCost);
                    }
                }
            }

            if (includeRange && maxRange > 0)
            {
                ExpandRangeFromBoundary(result, movementBudget, maxRange);
            }

            return result;
        }

        public List<MapGridPoint> GetPathThroughReachable(
            MapGridPoint start,
            MapGridPoint goal,
            Dictionary<MapGridPoint, float> reachable
        )
        {
            var path = new List<MapGridPoint>();
            if (!reachable.ContainsKey(goal))
            {
                return path;
            }

            var current = goal;
            path.Add(current);

            while (current != start)
            {
                MapGridPoint next = null;
                float lowestCost = float.MaxValue;

                current.GetNeighborsNonAlloc(_neighborsBuffer);
                foreach (var (_, neighbor) in _neighborsBuffer)
                {
                    if (
                        reachable.TryGetValue(neighbor, out var cost)
                        && cost < lowestCost
                        && !neighbor.IsOccupied
                    )
                    {
                        lowestCost = cost;
                        next = neighbor;
                    }
                }

                if (next == null)
                {
                    path.Clear();
                    return path;
                }

                path.Add(next);
                current = next;
            }

            path.Reverse();
            return path;
        }
        #endregion

        #region Helper Methods
        private bool ValidateInputs(MapGrid graph, MapGridPoint start, MapGridPoint goal) =>
            graph != null && start != null && goal != null;

        private MapGridPoint GetCanonicalPoint(MapGrid graph, MapGridPoint point) =>
            graph.GetGridPoint(point.Row, point.Col) ?? point;

        private bool IsGoalReached(MapGridPoint current, MapGridPoint goal) =>
            current == goal || (current.Row == goal.Row && current.Col == goal.Col);

        private SearchContext CreateSearchContext() => new SearchContext();

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

            return grid != null
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
