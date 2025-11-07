using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Status/Has Debuff")]
[NodeLabel("Checks if a unit has a debuff")]
public class HasDebuff : SkillNode
{
    [Output]
    BoolValue UnitHasDebuff;

    [Output]
    BoolValue EnemyHasDebuff;

    [Output]
    BoolValue AllyHasDebuff;

    [Tooltip("Specific debuff type to check (leave empty to check for any debuff)")]
    public string debuffType = "";

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "UnitHasDebuff")
        {
            BoolValue unitHasDebuff = new();
            // TODO: Implement runtime retrieval of unit debuff status
            // Check context.UnitInstance for debuffs (specific type or any)
            return unitHasDebuff;
        }
        else if (port.fieldName == "EnemyHasDebuff")
        {
            BoolValue enemyHasDebuff = new();
            // TODO: Implement runtime retrieval of enemy debuff status
            // Check context.Targets[0] for debuffs (specific type or any)
            return enemyHasDebuff;
        }
        else if (port.fieldName == "AllyHasDebuff")
        {
            BoolValue allyHasDebuff = new();
            // TODO: Implement runtime retrieval of adjacent ally debuff status
            // Check context.AdjacentUnits (filtered for allies) for debuffs
            return allyHasDebuff;
        }
        return null;
    }
}
