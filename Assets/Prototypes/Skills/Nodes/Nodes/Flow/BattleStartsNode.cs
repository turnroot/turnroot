using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Flow/Start/Battle Starts")]
[NodeLabel("Runs once at the start of battle")]
public class BattleStartsNode : SkillNode
{
    [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
    public ExecutionFlow flow;

    public override object GetValue(NodePort port)
    {
        return null;
    }
}
