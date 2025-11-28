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
    [HideInInspector]
    public string Name;

    [SerializeField]
    public string Description;

    // runtime-only state - don't serialize
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
/// Condition to defeat all enemies.
/// </summary>
public class DefeatAllEnemiesBattleCondition : BattleCondition
{
    public DefeatAllEnemiesBattleCondition()
        : base("Defeat All Enemies", "Defeat all enemy units on the battlefield") { }

    public void CheckCondition(List<CharacterData> enemies)
    {
        // see below, not ready yet
        ConditionMet();
    }
}

/// <summary>
/// Condition to defeat specific enemies.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="enemiesToDefeat"></param>
[Serializable]
public class DefeatEnemyBattleCondition : BattleCondition
{
    [SerializeField]
    public CharacterData[] EnemiesToDefeat;

    public DefeatEnemyBattleCondition(
        string name,
        string description,
        CharacterData[] enemiesToDefeat
    )
        : base(name, description)
    {
        EnemiesToDefeat = enemiesToDefeat ?? Array.Empty<CharacterData>();
    }

    // Parameterless constructor required for inspector CreateInstance via reflection
    public DefeatEnemyBattleCondition()
        : base("Defeat enemies", "Kill the listed enemies")
    {
        EnemiesToDefeat = Array.Empty<CharacterData>();
    }

    public void CheckCondition()
    {
        foreach (var enemy in EnemiesToDefeat)
        {
            // get the instance, check IsDefeatedInCurrentBattle
            // this can only work in runtime, since instances
            // are created at runtime

            // TODO: Set up a way to get from CharacterData to CharacterData
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
[Serializable]
public class SurviveTurnsBattleCondition : BattleCondition
{
    [SerializeField]
    public int TurnsToSurvive;
    private int turnsSurvived = 0;

    public SurviveTurnsBattleCondition(string name, string description, int turnsToSurvive)
        : base(name, description)
    {
        TurnsToSurvive = turnsToSurvive;
    }

    public SurviveTurnsBattleCondition()
        : base("Survive Turns", "Survive the specified number of turns")
    {
        TurnsToSurvive = 1;
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
[Serializable]
public class ProtectNPCsBattleCondition : BattleCondition
{
    [SerializeField]
    public CharacterData[] NPCsToProtect;

    [SerializeField]
    public int MustSurviveCount = 0;

    public ProtectNPCsBattleCondition(
        string name,
        string description,
        int mustSurviveCount,
        CharacterData[] npcsToProtect
    )
        : base(name, description)
    {
        NPCsToProtect = npcsToProtect ?? Array.Empty<CharacterData>();
    }

    public ProtectNPCsBattleCondition()
        : base("Protect NPCs", "Prevent listed NPCs from being defeated")
    {
        NPCsToProtect = Array.Empty<CharacterData>();
    }

    public void CheckCondition()
    {
        // See above. Can't do it
        ConditionMet();
    }
}

/// <summary>
/// Condition to occupy specific tiles on the battlefield.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="targetTiles"></param>
/// <param name="allTiles"></param>
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

/// <summary>
/// Condition to limit the battle duration by a number of turns.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="turnLimit"></param>
[Serializable]
public class TimeLimitBattleCondition : BattleCondition
{
    [SerializeField]
    public int TurnLimit;
    private int currentTurn = 0;

    public TimeLimitBattleCondition(string name, string description, int turnLimit)
        : base(name, description)
    {
        TurnLimit = turnLimit;
    }

    public TimeLimitBattleCondition()
        : base("Time Limit", "Limit the battle duration")
    {
        TurnLimit = 1;
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
[Serializable]
public class ProtectTilesBattleCondition : BattleCondition
{
    [SerializeField]
    public Vector2Int[] TilesToProtect;

    [SerializeField]
    public int MustProtectCount = 0;

    public ProtectTilesBattleCondition(
        string name,
        string description,
        Vector2Int[] tilesToProtect,
        int mustProtectCount = 0
    )
        : base(name, description)
    {
        TilesToProtect = tilesToProtect ?? Array.Empty<Vector2Int>();
        MustProtectCount = mustProtectCount;
    }

    public ProtectTilesBattleCondition()
        : base("Protect Tiles", "Protect the listed tiles")
    {
        TilesToProtect = Array.Empty<Vector2Int>();
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
[Serializable]
public class AllAlliesCrossRowOrColumnBattleCondition : BattleCondition
{
    [SerializeField]
    public int RowOrColumnIndex;

    [SerializeField]
    public bool IsRow;

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

    public AllAlliesCrossRowOrColumnBattleCondition()
        : base("All Allies Cross Row/Column", "Have all allies cross the specified row or column")
    {
        RowOrColumnIndex = 0;
        IsRow = true;
    }

    public void CheckCondition(List<CharacterData> allies)
    {
        foreach (var ally in allies)
        {
            // can't do yet!
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
[Serializable]
public class NoEnemiesCrossRowOrColumnBattleCondition : BattleCondition
{
    [SerializeField]
    public int RowOrColumnIndex;

    [SerializeField]
    public bool IsRow;

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

    public NoEnemiesCrossRowOrColumnBattleCondition()
        : base("No Enemies Cross Row/Column", "Ensure no enemies cross the specified row or column")
    {
        RowOrColumnIndex = 0;
        IsRow = true;
    }

    public void CheckCondition(List<CharacterData> enemies)
    {
        foreach (var enemy in enemies)
        {
            // can't do yet
        }
        ConditionMet();
    }
}

/// <summary>
/// Condition to take less than N damage total
/// between all allies.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="maxTotalDamage"></param>
[Serializable]
public class LimitTotalAllyDamageBattleCondition : BattleCondition
{
    [SerializeField]
    public int MaxTotalDamage;

    private int currentTotalDamage = 0;

    public LimitTotalAllyDamageBattleCondition(string name, string description, int maxTotalDamage)
        : base(name, description)
    {
        MaxTotalDamage = maxTotalDamage;
    }

    public LimitTotalAllyDamageBattleCondition()
        : base(
            "Limit Total Ally Damage",
            "Take less than the specified total damage across all allies"
        )
    {
        MaxTotalDamage = 0;
    }

    public void OnAllyDamaged(int damageAmount)
    {
        currentTotalDamage += damageAmount;
        CheckCondition();
    }

    public void CheckCondition()
    {
        if (currentTotalDamage > MaxTotalDamage)
        {
            ConditionFailed();
        }
    }
}

/// <summary>
/// Condition to deal at least N damage total
/// between all enemies.
/// </summary>
/// <param name="name"></param>
/// <param name="description"></param>
/// <param name="minTotalDamage"></param>
[Serializable]
public class DealMinimumTotalEnemyDamageBattleCondition : BattleCondition
{
    [SerializeField]
    public int MinTotalDamage;

    private int currentTotalDamage = 0;

    public DealMinimumTotalEnemyDamageBattleCondition(
        string name,
        string description,
        int minTotalDamage
    )
        : base(name, description)
    {
        MinTotalDamage = minTotalDamage;
    }

    public DealMinimumTotalEnemyDamageBattleCondition()
        : base(
            "Deal Minimum Total Enemy Damage",
            "Deal at least the specified total damage across all enemies"
        )
    {
        MinTotalDamage = 0;
    }

    public void OnEnemyDamaged(int damageAmount)
    {
        currentTotalDamage += damageAmount;
        CheckCondition();
    }

    public void CheckCondition()
    {
        if (currentTotalDamage >= MinTotalDamage)
        {
            ConditionMet();
        }
    }
}

#if TURNROOT_MONSTERS_MODULE
/// <summary>
/// Condition to defeat monsters.
/// </summary>
[Serializable]
public class DefeatAllMonstersBattleCondition : BattleCondition
{
    public DefeatAllMonstersBattleCondition()
        : base("Defeat All Monsters", "Defeat all monsters on the battlefield") { }

    public void CheckCondition(List<CharacterData> monsters)
    {
        // not ready yet
        ConditionMet();
    }
}
#endif
