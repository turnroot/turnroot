using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Range")]
[NodeLabel("Gets the weapon range information")]
public class WeaponRangeNode : SkillNode
{
    [Output]
    public FloatValue MinRange;

    [Output]
    public FloatValue MaxRange;

    [Output]
    public BoolValue IsMelee;

    [Output]
    public BoolValue IsRanged;

    [Output]
    public BoolValue CanCounterattack;

    public override object GetValue(NodePort port)
    {
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return defaults in editor mode
            return port.fieldName switch
            {
                "MinRange" => new FloatValue { value = 1f },
                "MaxRange" => new FloatValue { value = 1f },
                "IsMelee" => new BoolValue { value = true },
                "IsRanged" => new BoolValue { value = false },
                "CanCounterattack" => new BoolValue { value = true },
                _ => null,
            };
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null || context.UnitInstance == null)
        {
            Debug.LogWarning("WeaponRange: Could not retrieve context or unit from graph");
            return port.fieldName switch
            {
                "MinRange" or "MaxRange" => new FloatValue { value = 0f },
                _ => new BoolValue { value = false },
            };
        }

        // TODO: Implement weapon range retrieval from equipped weapon when item system is added
        // Future implementation: var weapon = context.UnitInstance.GetEquippedWeapon();
        // Then return weapon.MinRange, weapon.MaxRange properties
        // Calculate IsMelee (maxRange == 1), IsRanged (maxRange >= 2)
        // CanCounterattack should check if weapon range covers combat distance

        return port.fieldName switch
        {
            "MinRange" or "MaxRange" => new FloatValue { value = 0f },
            _ => new BoolValue { value = false },
        };
    }
}
