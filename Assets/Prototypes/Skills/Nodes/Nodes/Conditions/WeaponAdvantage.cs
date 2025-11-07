using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Advantage")]
[NodeLabel("Gets weapon advantage or same type")]
public class WeaponAdvantage : SkillNode
{
    [Output]
    BoolValue UnitAdvantage;

    [Output]
    BoolValue SameType;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "UnitAdvantage")
        {
            BoolValue unitAdvantage = new();
            // TODO: Implement runtime retrieval of unit advantage
            return unitAdvantage;
        }
        else if (port.fieldName == "SameType")
        {
            BoolValue sameType = new();
            // TODO: Implement runtime retrieval of same type
            return sameType;
        }
        return null;
    }
}
