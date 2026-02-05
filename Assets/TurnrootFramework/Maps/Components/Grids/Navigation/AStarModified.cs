using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;
using Utils;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// A* pathfinding algorithm implementation with support for various movement modes and terrain costs.
    /// Helper methods and SearchContext are located in AStarModifiedPartials/Helpers.cs.
    /// </summary>
    public partial class AStarModified
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
            if (start == null || goal == null || reachable == null)
            {
                return path;
            }

            // Both start and goal must be in the reachable set
            if (!reachable.ContainsKey(start) || !reachable.ContainsKey(goal))
            {
                return path;
            }

            // Forward Dijkstra-like search constrained to nodes present in 'reachable'.
            // Use reachable's cost as the priority so we naturally follow the minimal-cost frontier.
            var frontier = new PriorityQueue<MapGridPoint, float>();
            frontier.Enqueue(start, reachable[start]);

            var cameFrom = new Dictionary<MapGridPoint, MapGridPoint>();
            using var visitedPooled = PooledHashSet<MapGridPoint>.Get();
            var visited = visitedPooled.HashSet;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (!visited.Add(current))
                {
                    continue;
                }

                if (current == goal)
                {
                    // Reconstruct path from start -> goal
                    var node = goal;
                    while (node != null)
                    {
                        path.Add(node);
                        cameFrom.TryGetValue(node, out node);
                    }

                    path.Reverse();
                    return path;
                }

                current.GetNeighborsNonAlloc(_neighborsBuffer);
                foreach (var (_, neighbor) in _neighborsBuffer)
                {
                    if (!reachable.ContainsKey(neighbor) || neighbor.IsOccupied)
                    {
                        continue;
                    }

                    // If not visited and not already in cameFrom, set parent and enqueue
                    if (!cameFrom.ContainsKey(neighbor) && !visited.Contains(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        frontier.Enqueue(neighbor, reachable[neighbor]);
                    }
                }
            }

            // No path found
            return path;
        }
        #endregion

        // Helper methods, path reconstruction, and SearchContext are located in
        // AStarModifiedPartials/Helpers.cs
    }
}
