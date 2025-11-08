using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Combat/Is Initiating Combat")]
[NodeLabel("Checks if the unit is initiating combat")]
public class IsInitiatingCombatNode : SkillNode
{
    [Output]
    public BoolValue UnitInitiating;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != "UnitInitiating")
            return null;

        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            return new BoolValue { value = true }; // Default to initiating in editor
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null)
        {
            Debug.LogWarning("IsInitiatingCombat: Could not retrieve context from graph");
            return new BoolValue { value = true };
        }

        // TODO: Implement actual combat initiation check when combat system is added
        // This should check if the unit is the attacker (initiating) vs defender
        // Future implementation: return new BoolValue { value = context.IsInitiatingCombat };
        return new BoolValue { value = true };
    }
}
