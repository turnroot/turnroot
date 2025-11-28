using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
/// <summary>
/// Base class for battle conditions.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
public class BattleCondition
{
    public string Name { get; set; }
    public string Description { get; set; }
    private bool IsActive { get; set; } = false;

    public UnityEvent OnConditionMet;
    public UnityEvent OnConditionActive;
    public UnityEvent OnConditionInactive;
    public UnityEvent OnConditionFailed;

    public BattleCondition(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void ActivateCondition()
    {
        IsActive = true;
        OnConditionActive?.Invoke();
    }

    public void DeactivateCondition()
    {
        IsActive = false;
        OnConditionInactive?.Invoke();
    }

    public void ConditionMet()
    {
        OnConditionMet?.Invoke();
    }

    public void ConditionFailed()
    {
        OnConditionFailed?.Invoke();
    }
}

/// <summary>
/// Condition to defeat specific enemies.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="enemiesToDefeat"></param>
public class DefeatEnemyBattleCondition : BattleCondition
{
    public CharacterInstance[] EnemiesToDefeat { get; set; }

    public DefeatEnemyBattleCondition(
        string name,
        string description,
        CharacterInstance[] enemiesToDefeat
    )
        : base(name, description)
    {
        EnemiesToDefeat = enemiesToDefeat ?? Array.Empty<CharacterInstance>();
    }

    public void CheckCondition()
    {
        foreach (var enemy in EnemiesToDefeat)
        {
            if (!enemy.IsDefeatedInCurrentBattle)
            {
                return;
            }
        }
        ConditionMet();
    }
}

/// <summary>
/// Condition to survive a certain number of turns.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="turnsToSurvive"></param>
public class SurviveTurnsBattleCondition : BattleCondition
{
    public int TurnsToSurvive { get; set; }
    private int turnsSurvived = 0;

    public SurviveTurnsBattleCondition(string name, string description, int turnsToSurvive)
        : base(name, description)
    {
        TurnsToSurvive = turnsToSurvive;
    }

    public void OnTurnEnd()
    {
        turnsSurvived++;
        CheckCondition();
    }

    public void CheckCondition()
    {
        if (turnsSurvived >= TurnsToSurvive)
        {
            ConditionMet();
        }
    }
}

/// <summary>
/// Condition to protect specific NPCs from being defeated.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="npcsToProtect"></param>
public class ProtectNPCsBattleCondition : BattleCondition
{
    public CharacterInstance[] NPCsToProtect { get; set; }

    public ProtectNPCsBattleCondition(
        string name,
        string description,
        CharacterInstance[] npcsToProtect
    )
        : base(name, description)
    {
        NPCsToProtect = npcsToProtect ?? Array.Empty<CharacterInstance>();
    }

    public void CheckCondition()
    {
        foreach (var npc in NPCsToProtect)
        {
            if (npc.IsDefeatedInCurrentBattle)
            {
                ConditionFailed();
            }
        }
    }
}

/// <summary>
/// Condition to occupy specific tiles on the battlefield.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="targetTiles"></param>
/// <param name="allTiles"></param>
public class ReachTilesBattleCondition : BattleCondition
{
    public Vector2Int[] TargetTiles { get; set; }
    public Vector2Int[] ReachedTiles { get; set; } = Array.Empty<Vector2Int>();
    private bool allTiles = true;

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

/// <summary>
/// Condition to limit the battle duration by a number of turns.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="turnLimit"></param>
public class TimeLimitBattleCondition : BattleCondition
{
    public int TurnLimit { get; set; }
    private int currentTurn = 0;

    public TimeLimitBattleCondition(string name, string description, int turnLimit)
        : base(name, description)
    {
        TurnLimit = turnLimit;
    }

    public void OnTurnEnd()
    {
        currentTurn++;
        CheckCondition();
    }

    public void CheckCondition()
    {
        if (currentTurn >= TurnLimit)
        {
            ConditionFailed();
        }
    }
}

/// <summary>
/// Condition to protect specific tiles from being captured or lost.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="tilesToProtect"></param>
public class ProtectTilesBattleCondition : BattleCondition
{
    public Vector2Int[] TilesToProtect { get; set; }

    public ProtectTilesBattleCondition(string name, string description, Vector2Int[] tilesToProtect)
        : base(name, description)
    {
        TilesToProtect = tilesToProtect ?? Array.Empty<Vector2Int>();
    }

    public void CheckCondition(Dictionary<Vector2Int, bool> tileStatus)
    {
        foreach (var tile in TilesToProtect)
        {
            if (tileStatus.ContainsKey(tile) && tileStatus[tile] == false)
            {
                ConditionFailed();
            }
        }
    }
}

/// <summary>
/// Condition to have all allies cross a specific row or column.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="rowOrColumnIndex"></param>
/// <param name="isRow"></param>
public class AllAlliesCrossRowOrColumnBattleCondition : BattleCondition
{
    public int RowOrColumnIndex { get; set; }
    public bool IsRow { get; set; }

    public AllAlliesCrossRowOrColumnBattleCondition(
        string name,
        string description,
        int rowOrColumnIndex,
        bool isRow = true
    )
        : base(name, description)
    {
        RowOrColumnIndex = rowOrColumnIndex;
        IsRow = isRow;
    }

    public void CheckCondition(List<CharacterInstance> allies)
    {
        foreach (var ally in allies)
        {
            Vector2Int position = ally.MapGridPosition;
            if (IsRow)
            {
                if (position.y <= RowOrColumnIndex)
                {
                    return;
                }
            }
            else
            {
                if (position.x <= RowOrColumnIndex)
                {
                    return;
                }
            }
        }
        ConditionMet();
    }
}

/// <summary>
/// Condition to have no enemies cross a specific row or column.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="rowOrColumnIndex"></param>
/// <param name="isRow"></param>
public class NoEnemiesCrossRowOrColumnBattleCondition : BattleCondition
{
    public int RowOrColumnIndex { get; set; }
    public bool IsRow { get; set; }

    public NoEnemiesCrossRowOrColumnBattleCondition(
        string name,
        string description,
        int rowOrColumnIndex,
        bool isRow = true
    )
        : base(name, description)
    {
        RowOrColumnIndex = rowOrColumnIndex;
        IsRow = isRow;
    }

    public void CheckCondition(List<CharacterInstance> enemies)
    {
        foreach (var enemy in enemies)
        {
            Vector2Int position = enemy.MapGridPosition;
            if (IsRow)
            {
                if (position.y >= RowOrColumnIndex)
                {
                    return;
                }
            }
            else
            {
                if (position.x >= RowOrColumnIndex)
                {
                    return;
                }
            }
        }
        ConditionMet();
    }
}
