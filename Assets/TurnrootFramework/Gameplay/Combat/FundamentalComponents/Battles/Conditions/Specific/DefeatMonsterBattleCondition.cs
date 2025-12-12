#if TURNROOT_MONSTERS_MODULE
using System;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

/// <summary>
/// Condition to defeat specific monsters.
/// </summary>
[Serializable]
public class DefeatMonsterBattleCondition : BattleCondition
{
    [SerializeField]
    public Turnroot.Modules.Monsters.MonsterData[] MonstersToDefeat;

    public DefeatMonsterBattleCondition(
        string name,
        string description,
        Turnroot.Modules.Monsters.MonsterData[] monstersToDefeat
    )
        : base(name, description)
    {
        MonstersToDefeat = monstersToDefeat ?? Array.Empty<Turnroot.Modules.Monsters.MonsterData>();
    }

    public DefeatMonsterBattleCondition()
        : base("Defeat monsters", "Kill the listed monsters")
    {
        MonstersToDefeat = Array.Empty<Turnroot.Modules.Monsters.MonsterData>();
    }

    public void CheckCondition()
    {
        foreach (var monster in MonstersToDefeat)
        {
            // TODO: use battle context to check if the monster is defeated
        }
        // Implementation pending: Only call ConditionMet() if all monsters are defeated.
        // ConditionMet();
    }
}
#endif
