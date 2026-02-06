using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Triggers skill execution when an enemy attacks the unit.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Enemy Attacks")]
    [NodeLabel("Runs when an enemy attacks this unit")]
    public class EnemyAttacksNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
