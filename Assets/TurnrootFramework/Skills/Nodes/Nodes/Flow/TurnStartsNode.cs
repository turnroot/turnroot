using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    [CreateNodeMenu("Flow/Start/Turn Starts")]
    [NodeLabel("Runs at the start of unit's turn")]
    public class TurnStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
