using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;
using Utils;

public class AStarModified
{
    // Reusable dictionary for GetNeighborsNonAlloc to avoid allocations in hot paths
    private readonly Dictionary<string, MapGridPoint> _neighborsBuffer = new(8);

    private float Heuristic(MapGridPoint a, MapGridPoint b)
    {
        int dRow = Mathf.Abs(a.Row - b.Row);
        int dCol = Mathf.Abs(a.Col - b.Col);
        return dRow + dCol;
    }

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
        if (graph == null || start == null || goal == null)
        {
            return new List<MapGridPoint>();
        }

        // Use canonical grid instances for start/goal so identity checks work
        MapGridPoint canonicalStart = graph.GetGridPoint(start.Row, start.Col) ?? start;
        MapGridPoint canonicalGoal = graph.GetGridPoint(goal.Row, goal.Col) ?? goal;

        PriorityQueue<MapGridPoint, float> frontier = new();
        frontier.Enqueue(canonicalStart, 0f);

        // Use pooled collections to reduce GC allocations
        using var cameFromPooled = PooledDictionary<MapGridPoint, MapGridPoint>.Get();
        using var costSoFarPooled = PooledDictionary<MapGridPoint, float>.Get();
        using var directionFromParentPooled = PooledDictionary<MapGridPoint, string>.Get();
        using var closedPooled = PooledHashSet<MapGridPoint>.Get();

        var cameFrom = cameFromPooled.Dictionary;
        var costSoFar = costSoFarPooled.Dictionary;
        var directionFromParent = directionFromParentPooled.Dictionary;
        var closed = closedPooled.HashSet;

        cameFrom[canonicalStart] = null;
        costSoFar[canonicalStart] = 0f;
        directionFromParent[canonicalStart] = null;

        while (frontier.Count > 0)
        {
            MapGridPoint current = frontier.Dequeue();
            if (
                current == canonicalGoal
                || (current.Row == canonicalGoal.Row && current.Col == canonicalGoal.Col)
            )
            {
                // Reconstruct ordered path
                List<MapGridPoint> result = new();
                MapGridPoint node = current;
                while (node != null)
                {
                    result.Add(node);
                    node = cameFrom.TryGetValue(node, out var parent) ? parent : null;
                }
                result.Reverse();
                return result;
            }

            if (closed.Contains(current))
            {
                continue;
            }

            closed.Add(current);

            // Use non-allocating neighbor retrieval
            current.GetNeighborsNonAlloc(_neighborsBuffer);
            foreach (var neighborPair in _neighborsBuffer)
            {
                var neighbor = neighborPair.Value;
                if (closed.Contains(neighbor))
                {
                    continue;
                }

                float stepCost = neighbor.GetTerrainTypeCost(
                    isWalking,
                    isFlying,
                    isRiding,
                    isMagic,
                    isArmored
                );

                if (
                    directionFromParent.TryGetValue(current, out var parentDir)
                    && parentDir == neighborPair.Key
                )
                {
                    stepCost *= sameDirectionMultiplier;
                }

                float newCost = costSoFar[current] + stepCost;

                if (
                    !costSoFar.TryGetValue(neighbor, out var existingCost)
                    || newCost < existingCost
                )
                {
                    costSoFar[neighbor] = newCost;
                    float priority = newCost + Heuristic(neighbor, canonicalGoal);
                    frontier.Enqueue(neighbor, priority);
                    cameFrom[neighbor] = current;
                    directionFromParent[neighbor] = neighborPair.Key;
                }
            }
        }

        // No path found: return empty list
        return new List<MapGridPoint>();
    }

    /// <summary>
    /// Compute all reachable tiles from start with a maximum movement budget.
    /// Returns a dictionary mapping reachable MapGridPoint -> least cost to reach.
    /// The returned dictionary is owned by the caller and should be managed accordingly.
    /// </summary>
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
        // Don't use pool for returned dictionary as caller owns it
        var result = new Dictionary<MapGridPoint, float>();
        if (graph == null || start == null)
        {
            return result;
        }

        MapGridPoint canonicalStart = graph.GetGridPoint(start.Row, start.Col) ?? start;

        PriorityQueue<MapGridPoint, float> frontier = new();
        frontier.Enqueue(canonicalStart, 0f);

        using var costSoFarPooled = PooledDictionary<MapGridPoint, float>.Get();
        using var directionFromParentPooled = PooledDictionary<MapGridPoint, string>.Get();
        var costSoFar = costSoFarPooled.Dictionary;
        var directionFromParent = directionFromParentPooled.Dictionary;

        costSoFar[canonicalStart] = 0f;
        directionFromParent[canonicalStart] = null;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            float currentCost = costSoFar[current];

            // Don't expand nodes that already exceed budget
            if (currentCost > movementBudget)
            {
                continue;
            }

            result[current] = currentCost;

            // Use non-allocating neighbor retrieval
            current.GetNeighborsNonAlloc(_neighborsBuffer);
            foreach (var kv in _neighborsBuffer)
            {
                var dir = kv.Key;
                var neighbor = kv.Value;
                if (neighbor.IsOccupied)
                {
                    continue;
                }
                float stepCost = neighbor.GetTerrainTypeCost(
                    isWalking,
                    isFlying,
                    isRiding,
                    isMagic,
                    isArmored
                );
                if (directionFromParent.TryGetValue(current, out var parentDir) && parentDir == dir)
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

    /// <summary>
    /// Given a dictionary of reachable tiles (from GetReachable), reconstruct a path from start to goal.
    /// Returns an empty list if no path exists.
    /// </summary>
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

        MapGridPoint current = goal;
        path.Add(current);

        while (current != start)
        {
            MapGridPoint next = null;
            float lowestCost = float.MaxValue;

            // Use non-allocating neighbor retrieval
            current.GetNeighborsNonAlloc(_neighborsBuffer);
            foreach (var kv in _neighborsBuffer)
            {
                var neighbor = kv.Value;
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
                // No path found
                path.Clear();
                return path;
            }

            path.Add(next);
            current = next;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Expand range tiles outward from boundary tiles (tiles at exact movementBudget cost).
    /// Used for attack range display after movement.
    /// </summary>
    private void ExpandRangeFromBoundary(
        Dictionary<MapGridPoint, float> result,
        int movementBudget,
        int maxRange
    )
    {
        // Find all boundary tiles (tiles at movementBudget)
        using var boundaryTilesPooled = PooledList<MapGridPoint>.Get();
        var boundaryTiles = boundaryTilesPooled.List;

        foreach (var kv in result)
        {
            if (Mathf.RoundToInt(kv.Value) == movementBudget)
            {
                boundaryTiles.Add(kv.Key);
            }
        }

        if (boundaryTiles.Count == 0)
        {
            return;
        }

        // Expand from boundary tiles by maxRange steps using BFS
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
                foreach (var n in _neighborsBuffer)
                {
                    var neighbor = n.Value;
                    if (!result.ContainsKey(neighbor) && !expanded.Contains(neighbor))
                    {
                        result[neighbor] = movementBudget + step;
                        nextFrontier.Add(neighbor);
                        expanded.Add(neighbor);
                    }
                }
            }

            if (nextFrontier.Count == 0)
            {
                break;
            }

            // Swap frontiers
            (currentFrontier, nextFrontier) = (nextFrontier, currentFrontier);
        }
    }
}
