using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Start/Enemy Attacks")]
[NodeLabel("Runs when an enemy attacks this unit")]
public class EnemyAttacksNode : SkillNode
{
    [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
    public ExecutionFlow flow;

    public override object GetValue(NodePort port)
    {
        return null;
    }
}
