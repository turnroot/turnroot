using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Position/Adjacent Allies")]
    [NodeLabel("Gets the current adjacent allies count")]
    public class AdjacentAlliesNode : AdjacentUnitsNodeBase
    {
        protected override string NodeName => "AdjacentAllies";

        protected override int GetAdjacentCount(BattleContext context)
        {
            return context.AdjacentUnits.GetAdjacentAllyCount(context);
        }
    }
}
