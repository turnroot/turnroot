using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires when this unit moves to a new tile.
    /// No combat target is set — use a <see cref="ForEachEnemyNode"/> downstream
    /// to iterate all battlefield enemies when per-enemy condition checks are needed.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Unit Moves")]
    [NodeLabel("Runs when this unit moves (non-combat; use For Each Enemy for per-enemy checks)")]
    public class UnitMovesNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
