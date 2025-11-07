using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Range")]
[NodeLabel("Gets the weapon range information")]
public class WeaponRange : SkillNode
{
    [Output]
    FloatValue MinRange;

    [Output]
    FloatValue MaxRange;

    [Output]
    BoolValue IsMelee;

    [Output]
    BoolValue IsRanged;

    [Output]
    BoolValue CanCounterattack;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "MinRange")
        {
            FloatValue minRange = new();
            // TODO: Implement runtime retrieval of weapon minimum range
            return minRange;
        }
        else if (port.fieldName == "MaxRange")
        {
            FloatValue maxRange = new();
            // TODO: Implement runtime retrieval of weapon maximum range
            return maxRange;
        }
        else if (port.fieldName == "IsMelee")
        {
            BoolValue isMelee = new();
            // TODO: Implement runtime check for melee range (typically range = 1)
            return isMelee;
        }
        else if (port.fieldName == "IsRanged")
        {
            BoolValue isRanged = new();
            // TODO: Implement runtime check for ranged weapon (range >= 2)
            return isRanged;
        }
        else if (port.fieldName == "CanCounterattack")
        {
            BoolValue canCounterattack = new();
            // TODO: Implement runtime check if unit can counterattack at current distance
            return canCounterattack;
        }
        return null;
    }
}
