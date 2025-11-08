using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Start/Enemy Defeated")]
[NodeLabel("Runs when an enemy is defeated by this unit")]
public class EnemyDefeatedNode : SkillNode
{
    [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
    public ExecutionFlow flow;

    public override object GetValue(NodePort port)
    {
        return null;
    }
}
