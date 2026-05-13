using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point node that triggers at the start of ANY unit's turn (player, enemy, or
    /// third-party). Fired via <c>Brain.OnUnitTurnStarted</c> from TurnRotisserie for every
    /// unit activation. In non-combat flows, <c>context.Participants.Targets</c> holds the
    /// full enemy list — pair with ForEachEnemyNode to iterate enemies individually.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Turn Starts")]
    [NodeLabel("Runs at the start of any unit's turn (player & enemy)")]
    public class TurnStartsNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
