using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Triggers skill execution once at the start of a battle.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Battle Starts")]
    [NodeLabel("Runs once at the start of battle")]
    public class BattleStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;

        public override void Execute(BattleContext context) { }
    }
}
