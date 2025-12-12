using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

/// <summary>
/// Condition to defeat specific enemies.
/// </summary>
[Serializable]
public class DefeatEnemyBattleCondition : BattleCondition
{
    [SerializeField]
    public CharacterData[] EnemiesToDefeat;

    private readonly CacheManager<string, List<CharacterInstance>> _enemiesCache = new();
    private const string CACHE_KEY = "TargetEnemies";

    public DefeatEnemyBattleCondition(string name, string description)
        : base(name, description) { }

    public DefeatEnemyBattleCondition()
        : base("Defeat enemies", "Kill the listed enemies") { }

    public override void InvalidateCache() => _enemiesCache.Clear();

    private List<CharacterInstance> GetTargetEnemies()
    {
        return _enemiesCache.GetOrAdd(
            CACHE_KEY,
            () => GetMatchingUnits(battleContext.Targets, EnemiesToDefeat)
        );
    }

    public void CheckCondition()
    {
        if (!ValidateBattleContext(nameof(DefeatEnemyBattleCondition)))
        {
            return;
        }

        if (!ValidationHelper.ValidateNotNullOrEmpty(EnemiesToDefeat, nameof(EnemiesToDefeat)))
        {
            Debug.LogWarning("DefeatEnemyBattleCondition: No enemies specified.");
            return;
        }

        var targetEnemies = GetTargetEnemies();

        if (!ValidationHelper.ValidateNotNullOrEmpty(targetEnemies, nameof(targetEnemies)))
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
