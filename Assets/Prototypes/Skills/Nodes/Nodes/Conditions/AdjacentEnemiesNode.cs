using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Position/Adjacent Enemies")]
[NodeLabel("Gets the current adjacent enemies count")]
public class AdjacentEnemiesNode : SkillNode
{
    [Output]
    public FloatValue value;

    [Output]
    public BoolValue adjacentEnemy;

    public override object GetValue(NodePort port)
    {
        // Get context from the graph
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null)
        {
            Debug.LogWarning("AdjacentEnemies: Could not get SkillGraph");
            return port.fieldName == "value" ? new FloatValue() : (object)new BoolValue();
        }

        var context = GetContextFromGraph(skillGraph);
        if (context?.AdjacentUnits == null)
        {
            Debug.LogWarning("AdjacentEnemies: No adjacent units in context");
            return port.fieldName == "value" ? new FloatValue() : (object)new BoolValue();
        }

        if (port.fieldName == "value")
        {
            int count = context.AdjacentUnits.GetAdjacentEnemyCount(context);
            return new FloatValue { value = count };
        }
        else if (port.fieldName == "adjacentEnemy")
        {
            int count = context.AdjacentUnits.GetAdjacentEnemyCount(context);
            return new BoolValue { value = count > 0 };
        }
        return null;
    }
}
