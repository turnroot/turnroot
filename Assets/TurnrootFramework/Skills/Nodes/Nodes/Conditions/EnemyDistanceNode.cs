using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Returns the Manhattan distance between the skill user and the current target enemy.
    ///
    /// In combat flows (Unit Attacks / Enemy Attacks): the target is the specific enemy
    /// being engaged — this works automatically.
    ///
    /// In non-combat flows (Turn Ends, Unit Moves, Battle Starts): place a
    /// <see cref="ForEachEnemyNode"/> upstream so each enemy is set as the target in
    /// turn. Without it, no target is available and this node returns 0 with a warning.
    /// </summary>
    [CreateNodeMenu("Conditions/Position/Enemy Distance")]
    [NodeLabel("Gets the distance to the current target enemy (combat, or inside For Each Enemy)")]
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
            if (context == null || context.Unit.UnitInstance == null)
            {
                "EnemyDistance: Could not retrieve context or unit from graph".LogWarning();
                return new FloatValue { value = 0f };
            }

            // Get enemy from context (first target)
            var enemy =
                context.Participants.Targets != null && context.Participants.Targets.Count > 0
                    ? context.Participants.Targets[0]
                    : null;

            if (enemy == null)
            {
                "EnemyDistance: No enemy target in context".LogWarning();
                return new FloatValue { value = 0f };
            }

            // Calculate Manhattan distance between unit and enemy positions
            var unitPos = context.Unit.UnitInstance.MapGridPosition;
            var enemyPos = enemy.MapGridPosition;
            int distance = Mathf.Abs(unitPos.x - enemyPos.x) + Mathf.Abs(unitPos.y - enemyPos.y);

            return new FloatValue { value = distance };
        }
    }
}
