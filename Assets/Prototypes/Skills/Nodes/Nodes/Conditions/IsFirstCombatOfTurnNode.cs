using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Combat/Is First Combat Of Turn")]
[NodeLabel("Checks if this is the unit's first combat this turn")]
public class IsFirstCombatOfTurnNode : SkillNode
{
    [Output]
    public BoolValue IsFirstCombat;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName != "IsFirstCombat")
            return null;

        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            return new BoolValue { value = true }; // Default to first combat in editor
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null)
        {
            Debug.LogWarning("IsFirstCombatOfTurn: Could not retrieve context from graph");
            return new BoolValue { value = true };
        }

        // TODO: Implement actual first combat check when turn tracking system is added
        // This should check context.CombatCount or similar turn tracking
        // Future implementation: return new BoolValue { value = context.CombatCountThisTurn == 1 };
        return new BoolValue { value = true };
    }
}
