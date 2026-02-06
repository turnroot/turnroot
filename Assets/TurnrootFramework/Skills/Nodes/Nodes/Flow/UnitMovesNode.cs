using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point node that triggers when a unit moves.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Unit Moves")]
    [NodeLabel("Runs when this unit moves")]
    public class UnitMovesNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
