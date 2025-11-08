using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Status/Is Flying")]
[NodeLabel("Checks if the unit is flying")]
public class IsFlyingNode : SkillNode
{
    [Output]
    BoolValue UnitFlying;

    [Output]
    BoolValue EnemyFlying;

    [Output]
    BoolValue AdjacentAllyFlying;

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
            "UnitFlying" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Unit
            ),
            "EnemyFlying" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Enemy
            ),
            "AdjacentAllyFlying" => ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Ally
            ),
            _ => null,
        };

        if (character == null)
        {
            return new BoolValue { value = false };
        }

        // TODO: Implement actual flying status check when movement type system is added
        // For now, return false as placeholder
        // Future implementation: return new BoolValue { value = character.IsFlying };
        return new BoolValue { value = false };
    }
}
