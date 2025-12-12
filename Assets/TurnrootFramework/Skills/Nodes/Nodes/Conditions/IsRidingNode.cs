using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Status/Is Riding")]
    [NodeLabel("Checks if the unit is riding")]
    public class IsRidingNode : SkillNode
    {
        [Output]
        BoolValue UnitRiding;

        [Output]
        BoolValue EnemyRiding;

        [Output]
        BoolValue AdjacentAllyRiding;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                // Return false in editor mode
                return new BoolValue { value = false };
            }

            // Get context
            var context = GetContextFromGraph(skillGraph);
            if (context == null)
            {
                return new BoolValue { value = false };
            }

            // Determine which character to check based on port
            var character = port.fieldName switch
            {
                "UnitRiding" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Unit
                ),
                "EnemyRiding" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Enemy
                ),
                "AdjacentAllyRiding" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Ally
                ),
                _ => null,
            };

            return character == null ? new BoolValue { value = false }
                : character == null ? new BoolValue { value = false }
                : new BoolValue
                {
                    value =
                        character.CurrentClass.ClassData.Identity.MovementType
                        == MovementType.Riding,
                };
        }
    }
}
