using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

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

        // TODO: Implement actual armored status check when movement type system is added
        // For now, return false as placeholder
        // Future implementation: return new BoolValue { value = character.IsArmored };
        return new BoolValue { value = false };
    }
}
