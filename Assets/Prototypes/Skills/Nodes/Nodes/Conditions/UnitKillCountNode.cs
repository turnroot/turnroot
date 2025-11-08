using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit/Kill Count")]
[NodeLabel("Gets the unit's kill count")]
public class UnitKillCountNode : SkillNode
{
    [Output]
    FloatValue value;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != "value")
            return null;

        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            return new FloatValue { value = 0f }; // Default to 0 kills in editor
        }

        var context = GetContextFromGraph(skillGraph);
        var character = ConditionHelpers.GetCharacterFromContext(
            context,
            ConditionHelpers.CharacterSource.Unit
        );

        if (character == null)
        {
            Debug.LogWarning("UnitKillCount: Could not retrieve unit from context");
            return new FloatValue { value = 0f };
        }

        // TODO: Implement actual kill count retrieval when tracking system is added
        // Future implementation: return new FloatValue { value = character.KillCount };
        return new FloatValue { value = 0f };
    }
}
