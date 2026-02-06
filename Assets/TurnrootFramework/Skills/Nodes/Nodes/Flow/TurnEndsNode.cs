using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point node that triggers at the end of a unit's turn.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Turn Ends")]
    [NodeLabel("Runs at the end of unit's turn")]
    public class TurnEndsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
