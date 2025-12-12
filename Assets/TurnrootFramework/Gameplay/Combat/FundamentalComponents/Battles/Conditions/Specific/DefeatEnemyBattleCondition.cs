using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

/// <summary>
/// Condition to defeat specific enemies.
/// </summary>
[Serializable]
public class DefeatEnemyBattleCondition : BattleCondition
{
    [SerializeField]
    public CharacterData[] EnemiesToDefeat;

    private List<CharacterInstance> _cachedEnemies;
    private bool _cacheIsDirty = true;

    public DefeatEnemyBattleCondition(string name, string description)
        : base(name, description) { }

    public DefeatEnemyBattleCondition()
        : base("Defeat enemies", "Kill the listed enemies") { }

    public override void InvalidateCache()
    {
        _cacheIsDirty = true;
    }

    private List<CharacterInstance> GetTargetEnemies()
    {
        if (_cacheIsDirty || _cachedEnemies == null)
        {
            _cachedEnemies = GetMatchingUnits(battleContext.Targets, EnemiesToDefeat);
            _cacheIsDirty = false;
        }
        return _cachedEnemies;
    }

    public void CheckCondition()
    {
        if (!ValidateBattleContext(nameof(DefeatEnemyBattleCondition)))
        {
            return;
        }

        if (EnemiesToDefeat == null || EnemiesToDefeat.Length == 0)
        {
            Debug.LogWarning("DefeatEnemyBattleCondition: No enemies specified.");
            return;
        }

        var targetEnemies = GetTargetEnemies();

        if (targetEnemies.Count == 0)
        {
            Debug.LogWarning("DefeatEnemyBattleCondition: No matching enemies found in battle.");
            return;
        }

        if (targetEnemies.All(enemy => enemy.IsDefeatedInCurrentBattle))
        {
            ConditionMet();
        }
    }
}
