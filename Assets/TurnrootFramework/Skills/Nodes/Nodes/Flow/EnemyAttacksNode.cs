using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    [CreateNodeMenu("Flow/Start/Enemy Attacks")]
    [NodeLabel("Runs when an enemy attacks this unit")]
    public class EnemyAttacksNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow flow;

        public override object GetValue(NodePort port) => null;
    }
}
