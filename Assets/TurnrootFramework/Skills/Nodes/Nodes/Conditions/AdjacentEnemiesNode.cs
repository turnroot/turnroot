using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Position/Adjacent Enemies")]
    [NodeLabel("Gets the current adjacent enemies count")]
    public class AdjacentEnemiesNode : AdjacentUnitsNodeBase
    {
        protected override string NodeName => "AdjacentEnemies";

        protected override int GetAdjacentCount(BattleContext context)
        {
            return context.AdjacentUnits.GetAdjacentEnemyCount(context);
        }
    }
}
