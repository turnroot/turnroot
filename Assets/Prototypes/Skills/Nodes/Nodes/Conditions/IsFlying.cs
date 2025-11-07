using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Is Flying")]
[NodeLabel("Checks if the unit is flying")]
public class IsFlying : SkillNode
{
    [Output]
    BoolValue UnitFlying;

    [Output]
    BoolValue EnemyFlying;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "UnitFlying")
        {
            BoolValue unitFlying = new();
            // TODO: Implement runtime retrieval of unit flying status
            return unitFlying;
        }
        else if (port.fieldName == "EnemyFlying")
        {
            BoolValue enemyFlying = new();
            // TODO: Implement runtime retrieval of enemy flying status
            return enemyFlying;
        }
        return null;
    }
}
