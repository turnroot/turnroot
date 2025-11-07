using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Is Armored")]
[NodeLabel("Checks if the unit is armored")]
public class IsArmored : SkillNode
{
    [Output]
    BoolValue UnitArmored;

    [Output]
    BoolValue EnemyArmored;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "UnitArmored")
        {
            BoolValue unitArmored = new();
            // TODO: Implement runtime retrieval of unit armored status
            return unitArmored;
        }
        else if (port.fieldName == "EnemyArmored")
        {
            BoolValue enemyArmored = new();
            // TODO: Implement runtime retrieval of enemy armored status
            return enemyArmored;
        }
        return null;
    }
}
