using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Position/Enemy Distance")]
[NodeLabel("Gets the distance to the target enemy")]
public class EnemyDistanceNode : SkillNode
{
    [Output]
    public FloatValue value;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != "value")
            return null;

        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            return new FloatValue { value = 1f }; // Default distance in editor
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null || context.UnitInstance == null)
        {
            Debug.LogWarning("EnemyDistance: Could not retrieve context or unit from graph");
            return new FloatValue { value = 0f };
        }

        // TODO: Implement distance calculation when positioning system is added
        // Future implementation:
        // Get unit and enemy positions from context
        // Calculate Manhattan distance or Euclidean distance
        // Example: return new FloatValue { value = Vector2Int.Distance(unitPos, enemyPos) };

        return new FloatValue { value = 0f };
    }
}
