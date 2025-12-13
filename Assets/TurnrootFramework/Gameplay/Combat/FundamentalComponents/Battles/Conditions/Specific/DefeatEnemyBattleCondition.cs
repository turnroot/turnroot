using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
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

    private CacheManager<string, List<CharacterInstance>> _enemiesCache;
    private const string CACHE_KEY = "TargetEnemies";

    public DefeatEnemyBattleCondition(string name, string description)
        : base(name, description)
    {
        InitializeCache();
    }

    public DefeatEnemyBattleCondition()
        : base("Defeat enemies", "Kill the listed enemies")
    {
        InitializeCache();
    }

    private void InitializeCache()
    {
        _enemiesCache = new CacheManager<string, List<CharacterInstance>>(
            key => GetMatchingUnits(battleContext.Targets, EnemiesToDefeat)
        );
    }

    public override void InvalidateCache() => _enemiesCache.Invalidate(CACHE_KEY);

    private List<CharacterInstance> GetTargetEnemies() => _enemiesCache.Get(CACHE_KEY);

    public void CheckCondition()
    {
        if (!ValidateBattleContext(nameof(DefeatEnemyBattleCondition)))
        {
            return;
        }

        if (!ValidationHelper.ValidateNotNullOrEmpty(EnemiesToDefeat, nameof(EnemiesToDefeat)))
        {
            return;
        }

        var targetEnemies = GetTargetEnemies();

        if (!ValidationHelper.ValidateNotNullOrEmpty(targetEnemies, nameof(targetEnemies)))
        {
            return;
        }

        if (targetEnemies.All(enemy => enemy.IsDefeatedInCurrentBattle))
        {
            ConditionMet();
        }
    }
}
