using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires when this unit's turn ends.
    /// No combat target is set — use a <see cref="ForEachEnemyNode"/> downstream
    /// to iterate all battlefield enemies when per-enemy condition checks are needed.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Turn Ends")]
    [NodeLabel(
        "Runs at the end of unit's turn (non-combat; use For Each Enemy for per-enemy checks)"
    )]
    public class TurnEndsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
