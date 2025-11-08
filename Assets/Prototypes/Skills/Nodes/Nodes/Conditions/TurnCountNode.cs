using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Counters/Turn Count")]
[NodeLabel("Gets the current turn count")]
public class TurnCountNode : SkillNode
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
            return new FloatValue { value = 1f }; // Default to turn 1 in editor
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null)
        {
            Debug.LogWarning("TurnCount: Could not retrieve context from graph");
            return new FloatValue { value = 1f };
        }

        // TODO: Implement actual turn count retrieval from battle system
        // Future implementation: return new FloatValue { value = context.CurrentTurnNumber };
        return new FloatValue { value = 1f };
    }
}
