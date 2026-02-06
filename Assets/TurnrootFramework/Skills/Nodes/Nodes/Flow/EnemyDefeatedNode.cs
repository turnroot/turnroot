using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    /// <summary>
    /// Triggers skill execution when the unit defeats an enemy.
    /// </summary>
    [CreateNodeMenu("Flow/Start/Enemy Defeated")]
    [NodeLabel("Runs when an enemy is defeated by this unit")]
    public class EnemyDefeatedNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
