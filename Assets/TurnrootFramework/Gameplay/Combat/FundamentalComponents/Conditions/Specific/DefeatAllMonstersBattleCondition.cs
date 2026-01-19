#if TURNROOT_MONSTERS_MODULE
using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
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
            // Only call ConditionMet if all monsters are defeated
            if (monsters != null && monsters.Count > 0)
            {
                bool allDefeated = true;
                foreach (var monster in monsters)
                {
                    // Assuming MonsterData has a property 'IsDefeated' or similar
                    if (monster != null && !(monster.IsDefeated))
                    {
                        allDefeated = false;
                        break;
                    }
                }
                if (allDefeated)
                {
                    ConditionMet();
                }
            }
        }
    }
}
#endif
