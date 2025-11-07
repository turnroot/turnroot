using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Enemy/Enemy Class")]
[NodeLabel("Gets the enemy's class type")]
public class EnemyClass : SkillNode
{
    [Output]
    StringValue ClassName;

    [Output]
    BoolValue IsInfantry;

    [Output]
    BoolValue IsCavalry;

    [Output]
    BoolValue IsFlying;

    [Output]
    BoolValue IsArmored;

    [Output]
    BoolValue IsDragon;

    [Output]
    BoolValue IsBeast;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "ClassName")
        {
            StringValue className = new();
            // TODO: Implement runtime retrieval of enemy class name
            return className;
        }
        else if (port.fieldName == "IsInfantry")
        {
            BoolValue isInfantry = new();
            // TODO: Implement runtime check for infantry class
            return isInfantry;
        }
        else if (port.fieldName == "IsCavalry")
        {
            BoolValue isCavalry = new();
            // TODO: Implement runtime check for cavalry class
            return isCavalry;
        }
        else if (port.fieldName == "IsFlying")
        {
            BoolValue isFlying = new();
            // TODO: Implement runtime check for flying class
            return isFlying;
        }
        else if (port.fieldName == "IsArmored")
        {
            BoolValue isArmored = new();
            // TODO: Implement runtime check for armored class
            return isArmored;
        }
        else if (port.fieldName == "IsDragon")
        {
            BoolValue isDragon = new();
            // TODO: Implement runtime check for dragon class
            return isDragon;
        }
        else if (port.fieldName == "IsBeast")
        {
            BoolValue isBeast = new();
            // TODO: Implement runtime check for beast class
            return isBeast;
        }
        return null;
    }
}
