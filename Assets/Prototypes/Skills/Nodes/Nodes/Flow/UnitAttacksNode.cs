using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Start/Unit Attacks")]
[NodeLabel("Runs when this unit attacks")]
public class UnitAttacksNode : SkillNode
{
    [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
    public ExecutionFlow execOut;

    public override object GetValue(NodePort port)
    {
        return null;
    }
}
