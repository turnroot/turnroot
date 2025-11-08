using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

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

        if (character == null)
        {
            return new BoolValue { value = false };
        }

        // TODO: Implement actual riding status check when movement type system is added
        // For now, return false as placeholder
        // Future implementation: return new BoolValue { value = character.IsRiding };
        return new BoolValue { value = false };
    }
}
