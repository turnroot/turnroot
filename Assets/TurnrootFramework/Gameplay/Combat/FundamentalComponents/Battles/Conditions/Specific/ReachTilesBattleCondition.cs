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
}
