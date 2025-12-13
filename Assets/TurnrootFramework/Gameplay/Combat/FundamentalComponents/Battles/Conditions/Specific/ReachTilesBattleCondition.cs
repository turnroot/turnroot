using System;
using System.Linq;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

/// <summary>
/// Condition to occupy specific tiles on the battlefield.
/// </summary>
[Serializable]
public class ReachTilesBattleCondition : BattleCondition
{
    [SerializeField]
    public Vector2Int[] TargetTiles;

    [SerializeField]
    public Vector2Int[] ReachedTiles = Array.Empty<Vector2Int>();

    [SerializeField]
    public bool allTiles = true;

    public ReachTilesBattleCondition(
        string name,
        string description,
        Vector2Int[] targetTiles,
        bool allTiles = true
    )
        : base(name, description)
    {
        TargetTiles = targetTiles ?? Array.Empty<Vector2Int>();
        this.allTiles = allTiles;
    }

    public ReachTilesBattleCondition()
        : base("Reach Tiles", "Occupy the listed tiles")
    {
        TargetTiles = Array.Empty<Vector2Int>();
        ReachedTiles = Array.Empty<Vector2Int>();
        allTiles = true;
    }

    public void CheckCondition()
    {
        if (allTiles)
        {
            foreach (var tile in TargetTiles)
            {
                if (!ReachedTiles.Contains(tile))
                {
                    return;
                }
            }
            ConditionMet();
        }
        else
        {
            foreach (var tile in TargetTiles)
            {
                if (ReachedTiles.Contains(tile))
                {
                    ConditionMet();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Called when a unit moves to a tile. Tracks if the tile is a target tile.
    /// </summary>
    public void OnUnitReachedTile(Vector2Int position)
    {
        // Check if this is a target tile that hasn't been reached yet
        if (TargetTiles.Contains(position) && !ReachedTiles.Contains(position))
        {
            // Add to reached tiles array
            var newReachedTiles = new Vector2Int[ReachedTiles.Length + 1];
            Array.Copy(ReachedTiles, newReachedTiles, ReachedTiles.Length);
            newReachedTiles[ReachedTiles.Length] = position;
            ReachedTiles = newReachedTiles;

            UnityEngine.Debug.Log(
                $"ReachTilesBattleCondition: Tile {position} reached ({ReachedTiles.Length}/{TargetTiles.Length})"
            );

            // Check if condition is now met
            CheckCondition();
        }
    }

    /// <summary>
    /// Resets the reached tiles tracking.
    /// </summary>
    public void ResetReachedTiles()
    {
        ReachedTiles = Array.Empty<Vector2Int>();
    }
}
