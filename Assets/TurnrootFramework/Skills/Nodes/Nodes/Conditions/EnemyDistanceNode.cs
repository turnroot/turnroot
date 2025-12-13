using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Position/Enemy Distance")]
    [NodeLabel("Gets the distance to the target enemy")]
    public class EnemyDistanceNode : SkillNode
    {
        [Output]
        public FloatValue value;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != "value")
            {
                return null;
            }

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

            // Get enemy from context (first target)
            var enemy =
                context.Targets != null && context.Targets.Count > 0 ? context.Targets[0] : null;

            if (enemy == null)
            {
                Debug.LogWarning("EnemyDistance: No enemy target in context");
                return new FloatValue { value = 0f };
            }

            // Calculate Manhattan distance between unit and enemy positions
            var unitPos = context.UnitInstance.MapGridPosition;
            var enemyPos = enemy.MapGridPosition;
            int distance = Mathf.Abs(unitPos.x - enemyPos.x) + Mathf.Abs(unitPos.y - enemyPos.y);

            return new FloatValue { value = distance };
        }
    }
}
