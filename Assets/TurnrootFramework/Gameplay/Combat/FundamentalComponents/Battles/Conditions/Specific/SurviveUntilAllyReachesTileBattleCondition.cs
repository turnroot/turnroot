using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

/// <summary>
/// Condition to survive until a specific ally reaches a target tile.
/// </summary>
[Serializable]
public class SurviveUntilAllyReachesTileBattleCondition : BattleCondition
{
    [SerializeField]
    public CharacterData AllyToReachTile;

    [SerializeField]
    public Vector2Int TargetTile;

    private readonly SingleValueCache<CharacterInstance> _allyCache = new();

    public SurviveUntilAllyReachesTileBattleCondition(
        string name,
        string description,
        CharacterData allyToReachTile,
        Vector2Int targetTile
    )
        : base(name, description)
    {
        AllyToReachTile = allyToReachTile;
        TargetTile = targetTile;
    }

    public SurviveUntilAllyReachesTileBattleCondition()
        : base(
            "Survive Until Ally Reaches Tile",
            "Survive until the specified ally reaches the target tile"
        )
    {
        AllyToReachTile = null;
        TargetTile = Vector2Int.zero;
    }

    public override void InvalidateCache() => _allyCache.Invalidate();

    private CharacterInstance GetTargetAlly()
    {
        return _allyCache.GetOrCompute(() =>
            battleContext.Allies.FirstOrDefault(a => a.CharacterTemplate == AllyToReachTile)
        );
    }

    public void CheckCondition()
    {
        if (!ValidateBattleContext(nameof(SurviveUntilAllyReachesTileBattleCondition)))
        {
            return;
        }

        var ally = GetTargetAlly();
        if (ally != null && ally.MapGridPosition == TargetTile)
        {
            ConditionMet();
        }
    }
}
