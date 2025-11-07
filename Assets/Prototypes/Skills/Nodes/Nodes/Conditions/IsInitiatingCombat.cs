using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Combat/Is Initiating Combat")]
[NodeLabel("Checks if the unit is initiating combat")]
public class IsInitiatingCombat : SkillNode
{
    [Output]
    BoolValue UnitInitiating;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "UnitInitiating")
        {
            BoolValue unitInitiating = new();
            // TODO: Implement runtime retrieval of combat initiation status
            // This should check if the unit is the attacker (initiating) vs defender
            return unitInitiating;
        }
        return null;
    }
}
