using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Combat/Is First Combat Of Turn")]
[NodeLabel("Checks if this is the unit's first combat this turn")]
public class IsFirstCombatOfTurn : SkillNode
{
    [Output]
    BoolValue IsFirstCombat;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "IsFirstCombat")
        {
            BoolValue isFirstCombat = new();
            // TODO: Implement runtime retrieval of first combat status
            // This should check combat count for the current turn
            return isFirstCombat;
        }
        return null;
    }
}
