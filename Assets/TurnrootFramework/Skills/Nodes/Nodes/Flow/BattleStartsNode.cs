using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires once when the battle begins, then re-evaluates as a
    /// passive aura on every significant battle event (turn begin/end, unit moved, etc.).
    /// No combat target is set — use a <see cref="ForEachEnemyNode"/> downstream
    /// to check per-enemy conditions.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Battle Starts")]
    [NodeLabel(
        "Runs once at battle start, then re-evaluates passively (use For Each Enemy for per-enemy checks)"
    )]
    public class BattleStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;

        public override void Execute(BattleContext context) { }
    }
}
