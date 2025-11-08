using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Position/Adjacent Allies")]
[NodeLabel("Gets the current adjacent allies count")]
public class AdjacentAlliesNode : SkillNode
{
    [Output]
    public FloatValue value;

    [Output]
    BoolValue adjacentAlly;

    public override object GetValue(NodePort port)
    {
        // Get context from the graph
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null)
        {
            Debug.LogWarning("AdjacentAllies: Could not get SkillGraph");
            return port.fieldName == "value" ? new FloatValue() : (object)new BoolValue();
        }

        var context = GetContextFromGraph(skillGraph);
        if (context?.AdjacentUnits == null)
        {
            Debug.LogWarning("AdjacentAllies: No adjacent units in context");
            return port.fieldName == "value" ? new FloatValue() : (object)new BoolValue();
        }

        if (port.fieldName == "value")
        {
            int count = context.AdjacentUnits.GetAdjacentAllyCount(context);
            return new FloatValue { value = count };
        }
        else if (port.fieldName == "adjacentAlly")
        {
            int count = context.AdjacentUnits.GetAdjacentAllyCount(context);
            return new BoolValue { value = count > 0 };
        }
        return null;
    }
}
