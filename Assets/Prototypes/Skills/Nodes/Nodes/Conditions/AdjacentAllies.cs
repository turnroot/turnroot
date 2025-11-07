using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Adjacent Allies")]
[NodeLabel("Gets the current adjacent allies count")]
public class AdjacentAllies : SkillNode
{
    [Output]
    FloatValue value;

    [Output]
    BoolValue adjacentAlly;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "value")
        {
            FloatValue adjacentAlliesCount = new();
            // TODO: Implement runtime retrieval of adjacent allies count
            return adjacentAlliesCount;
        }
        else if (port.fieldName == "adjacentAlly")
        {
            BoolValue hasAdjacentAlly = new();
            // TODO: Implement runtime retrieval of adjacent allies presence
            return hasAdjacentAlly;
        }
        return null;
    }
}
