using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires once at the start of each individual combat exchange,
    /// before any strikes are resolved. Both the attacker's and defender's
    /// CombatStartsNode skills fire. Combat bonuses applied here (e.g. hit/avoid)
    /// are written to the combat-scoped bonus layer and cleared automatically after
    /// the exchange ends.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Combat Starts")]
    [NodeLabel("Fires once at the start of each combat exchange (before first strike)")]
    public class CombatStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
