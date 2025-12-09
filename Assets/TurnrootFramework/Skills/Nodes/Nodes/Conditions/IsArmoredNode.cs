using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Status/Is Armored")]
    [NodeLabel("Checks if the unit is armored")]
    public class IsArmoredNode : SkillNode
    {
        [Output]
        BoolValue UnitArmored;

        [Output]
        BoolValue EnemyArmored;

        [Output]
        BoolValue AdjacentAllyArmored;

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
                "UnitArmored" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Unit
                ),
                "EnemyArmored" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Enemy
                ),
                "AdjacentAllyArmored" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Ally
                ),
                _ => null,
            };

            if (character == null)
            {
                return new BoolValue { value = false };
            }

            return character == null ? new BoolValue { value = false }
                : character == null ? new BoolValue { value = false }
                : new BoolValue
                {
                    value = character.CurrentClass.ClassData.movementType == MovementType.Armored,
                };
        }
    }
}
