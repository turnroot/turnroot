using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires once after all strikes in a combat exchange have resolved,
    /// regardless of outcome (win, loss, both survive). Both the attacker's and defender's
    /// PostCombatNode skills fire. Use this for after-combat effects such as debuffing,
    /// repositioning, or stat changes that should take effect after the exchange.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Post-Combat")]
    [NodeLabel("Fires once after all strikes in a combat exchange have resolved")]
    public class PostCombatNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
