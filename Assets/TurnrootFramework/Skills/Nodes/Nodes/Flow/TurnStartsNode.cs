using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point node that triggers at the start of a unit's turn.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Turn Starts")]
    [NodeLabel("Runs at the start of unit's turn")]
    public class TurnStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
