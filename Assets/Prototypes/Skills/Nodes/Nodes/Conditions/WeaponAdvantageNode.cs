using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Advantage")]
[NodeLabel("Gets weapon advantage or same type")]
public class WeaponAdvantageNode : SkillNode
{
    [Output]
    BoolValue UnitAdvantage;

    [Output]
    BoolValue SameType;

    public override object GetValue(NodePort port)
    {
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return defaults in editor mode
            return port.fieldName switch
            {
                "UnitAdvantage" => new BoolValue { value = false },
                "SameType" => new BoolValue { value = true },
                _ => null,
            };
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null || context.UnitInstance == null)
        {
            Debug.LogWarning("WeaponAdvantage: Could not retrieve context or unit from graph");
            return new BoolValue { value = false };
        }

        // TODO: Implement weapon advantage calculation when weapon triangle system is added
        // Future implementation:
        // var unitWeapon = context.UnitInstance.GetEquippedWeapon();
        // var enemyWeapon = context.TargetInstance?.GetEquippedWeapon();
        // UnitAdvantage: check weapon triangle (Sword > Axe > Lance > Sword)
        // SameType: unitWeapon.WeaponType == enemyWeapon.WeaponType

        return new BoolValue { value = false };
    }
}
