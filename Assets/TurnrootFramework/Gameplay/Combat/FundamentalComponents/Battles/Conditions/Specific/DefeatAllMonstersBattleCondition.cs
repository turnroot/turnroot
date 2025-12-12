#if TURNROOT_MONSTERS_MODULE
using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

/// <summary>
/// Condition to defeat monsters.
/// </summary>
[Serializable]
public class DefeatAllMonstersBattleCondition : BattleCondition
{
    public DefeatAllMonstersBattleCondition()
        : base("Defeat All Monsters", "Defeat all monsters on the battlefield") { }

    public void CheckCondition(List<Turnroot.Modules.Monsters.MonsterData> monsters)
    {
        // TODO: use battle context to check if all monsters are defeated
        ConditionMet();
    }
}
#endif
