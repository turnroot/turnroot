using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Flow
{
    [CreateNodeMenu("Flow/Start/Unit Attacks")]
    [NodeLabel("Runs when this unit attacks")]
    public class UnitAttacksNode : SkillNode
    {
        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        public ExecutionFlow execOut;

        public override object GetValue(NodePort port) => null;
    }
}
