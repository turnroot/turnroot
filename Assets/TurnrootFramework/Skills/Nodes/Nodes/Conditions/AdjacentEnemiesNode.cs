using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Returns the number of enemy units adjacent to the caster.
    /// </summary>
    [CreateNodeMenu("Conditions/Position/Adjacent Enemies")]
    [NodeLabel("Gets the current adjacent enemies count")]
    public class AdjacentEnemiesNode : AdjacentUnitsNodeBase
    {
        protected override string NodeName => "AdjacentEnemies";

        protected override int GetAdjacentCount(BattleContext context) =>
            context.Participants.AdjacentUnits.GetAdjacentEnemyCount(context);
    }
}
