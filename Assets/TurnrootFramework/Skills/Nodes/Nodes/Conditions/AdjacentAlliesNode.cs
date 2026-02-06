using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Returns the number of allied units adjacent to the caster.
    /// </summary>
    [CreateNodeMenu("Conditions/Position/Adjacent Allies")]
    [NodeLabel("Gets the current adjacent allies count")]
    public class AdjacentAlliesNode : AdjacentUnitsNodeBase
    {
        protected override string NodeName => "AdjacentAllies";

        protected override int GetAdjacentCount(BattleContext context) =>
            context.Participants.AdjacentUnits.GetAdjacentAllyCount(context);
    }
}
