using System.Collections.Generic;
using UnityEditor.Formats.Fbx.Exporter;
using UnityEngine;
using Utils;

public class AStarModified
{
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
        // Use canonical grid instances for start/goal so identity checks work
        MapGridPoint canonicalStart = graph.GetGridPoint(start.Row, start.Col) ?? start;
        MapGridPoint canonicalGoal = graph.GetGridPoint(goal.Row, goal.Col) ?? goal;

        PriorityQueue<MapGridPoint, float> frontier = new();
        frontier.Enqueue(canonicalStart, 0f);
        Dictionary<MapGridPoint, MapGridPoint> cameFrom = new();
        Dictionary<MapGridPoint, float> costSoFar = new();
        HashSet<MapGridPoint> closed = new();
        Dictionary<MapGridPoint, string> directionFromParent = new();
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
                    node = cameFrom.ContainsKey(node) ? cameFrom[node] : null;
                }
                result.Reverse();
                return result;
            }

            if (closed.Contains(current))
            {
                continue;
            }

            closed.Add(current);

            var neighborSet = current.GetNeighbors();
            foreach (var neighborPair in neighborSet)
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
                    directionFromParent.ContainsKey(current)
                    && directionFromParent[current] == neighborPair.Key
                )
                {
                    stepCost *= sameDirectionMultiplier;
                }

                float newCost = costSoFar[current] + stepCost;

                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
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

    // Compute all reachable tiles from start with a maximum movement budget (int).
    // Returns a dictionary mapping reachable MapGridPoint -> least cost to reach.
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

        MapGridPoint canonicalStart = graph.GetGridPoint(start.Row, start.Col) ?? start;

        PriorityQueue<MapGridPoint, float> frontier = new();
        frontier.Enqueue(canonicalStart, 0f);
        var costSoFar = new Dictionary<MapGridPoint, float>();
        var directionFromParent = new Dictionary<MapGridPoint, string>();
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

            var neighbors = current.GetNeighbors();
            foreach (var kv in neighbors)
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
                if (directionFromParent.ContainsKey(current) && directionFromParent[current] == dir)
                {
                    stepCost *= sameDirectionMultiplier;
                }

                float newCost = currentCost + stepCost;
                if (newCost > movementBudget)
                {
                    continue;
                }

                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    directionFromParent[neighbor] = dir;
                    frontier.Enqueue(neighbor, newCost);
                }
            }
        }
        if (includeRange)
        {
            foreach (var kv in result)
            {
                // add Range to boundary tiles- so take all the highest cost tiles and
                // expand them by maxRange steps in all directions
                // get the highest cost tiles
                // it's not as simple as == budget because they are floats, so we need to
                // round them up to the next integer and then check if they are equal to the budget
                if (Mathf.RoundToInt(kv.Value) == movementBudget)
                {
                    // expand them by maxRange steps in all directions
                    for (int i = 0; i < maxRange; i++)
                    {
                        var neighbors = kv.Key.GetNeighbors();
                        foreach (var n in neighbors)
                        {
                            var neighbor = n.Value;
                            if (!result.ContainsKey(neighbor))
                            {
                                result[neighbor] = kv.Value + 1;
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
}
