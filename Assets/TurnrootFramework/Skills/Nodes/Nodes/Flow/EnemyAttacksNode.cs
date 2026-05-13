using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Entry point that fires when an enemy attacks this unit.
    /// <c>context.Participants.Targets</c> is set to the attacking enemy —
    /// no ForEachEnemyNode is needed for single-target checks against the attacker.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Enemy Attacks")]
    [NodeLabel(
        "Runs when an enemy attacks this unit (combat; attacking enemy is already set as target)"
    )]
    public class EnemyAttacksNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
