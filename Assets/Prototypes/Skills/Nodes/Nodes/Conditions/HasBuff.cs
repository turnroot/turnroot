using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Status/Has Buff")]
[NodeLabel("Checks if a unit has a buff")]
public class HasBuff : SkillNode
{
    [Output]
    BoolValue UnitHasBuff;

    [Output]
    BoolValue EnemyHasBuff;

    [Output]
    BoolValue AllyHasBuff;

    [Tooltip("Specific buff type to check (leave empty to check for any buff)")]
    public string buffType = "";

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "UnitHasBuff")
        {
            BoolValue unitHasBuff = new();
            // TODO: Implement runtime retrieval of unit buff status
            // Check context.UnitInstance for buffs (specific type or any)
            return unitHasBuff;
        }
        else if (port.fieldName == "EnemyHasBuff")
        {
            BoolValue enemyHasBuff = new();
            // TODO: Implement runtime retrieval of enemy buff status
            // Check context.Targets[0] for buffs (specific type or any)
            return enemyHasBuff;
        }
        else if (port.fieldName == "AllyHasBuff")
        {
            BoolValue allyHasBuff = new();
            // TODO: Implement runtime retrieval of adjacent ally buff status
            // Check context.AdjacentUnits (filtered for allies) for buffs
            return allyHasBuff;
        }
        return null;
    }
}
