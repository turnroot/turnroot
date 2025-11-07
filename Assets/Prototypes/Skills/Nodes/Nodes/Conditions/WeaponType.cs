using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Type")]
[NodeLabel("Gets the weapon type information")]
public class WeaponType : SkillNode
{
    [Output]
    StringValue TypeName;

    [Output]
    BoolValue IsSword;

    [Output]
    BoolValue IsLance;

    [Output]
    BoolValue IsAxe;

    [Output]
    BoolValue IsBow;

    [Output]
    BoolValue IsTome;

    [Output]
    BoolValue IsStaff;

    [Output]
    BoolValue IsDagger;

    [Output]
    BoolValue IsDragonstone;

    [Output]
    BoolValue IsBeaststone;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "TypeName")
        {
            StringValue typeName = new();
            // TODO: Implement runtime retrieval of weapon type name
            return typeName;
        }
        else if (port.fieldName == "IsSword")
        {
            BoolValue isSword = new();
            // TODO: Implement runtime check for sword type
            return isSword;
        }
        else if (port.fieldName == "IsLance")
        {
            BoolValue isLance = new();
            // TODO: Implement runtime check for lance type
            return isLance;
        }
        else if (port.fieldName == "IsAxe")
        {
            BoolValue isAxe = new();
            // TODO: Implement runtime check for axe type
            return isAxe;
        }
        else if (port.fieldName == "IsBow")
        {
            BoolValue isBow = new();
            // TODO: Implement runtime check for bow type
            return isBow;
        }
        else if (port.fieldName == "IsTome")
        {
            BoolValue isTome = new();
            // TODO: Implement runtime check for tome type
            return isTome;
        }
        else if (port.fieldName == "IsStaff")
        {
            BoolValue isStaff = new();
            // TODO: Implement runtime check for staff type
            return isStaff;
        }
        else if (port.fieldName == "IsDagger")
        {
            BoolValue isDagger = new();
            // TODO: Implement runtime check for dagger type
            return isDagger;
        }
        else if (port.fieldName == "IsDragonstone")
        {
            BoolValue isDragonstone = new();
            // TODO: Implement runtime check for dragonstone type
            return isDragonstone;
        }
        else if (port.fieldName == "IsBeaststone")
        {
            BoolValue isBeaststone = new();
            // TODO: Implement runtime check for beaststone type
            return isBeaststone;
        }
        return null;
    }
}
