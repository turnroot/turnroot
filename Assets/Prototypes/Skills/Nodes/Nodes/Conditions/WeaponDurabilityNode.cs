using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Durability")]
[NodeLabel("Gets the weapon durability information")]
public class WeaponDurabilityNode : SkillNode
{
    [Output]
    public FloatValue CurrentUses;

    [Output]
    public FloatValue MaxUses;

    [Output]
    public FloatValue UsesRemaining;

    [Output]
    public FloatValue PercentRemaining;

    [Output]
    public BoolValue IsBroken;

    [Output]
    public BoolValue IsLowDurability;

    [Tooltip("Threshold for low durability warning (percentage)")]
    [Range(0, 100)]
    public float lowDurabilityThreshold = 25f;

    public override object GetValue(NodePort port)
    {
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return defaults in editor mode
            return port.fieldName switch
            {
                "CurrentUses" => new FloatValue { value = 30f },
                "MaxUses" => new FloatValue { value = 50f },
                "UsesRemaining" => new FloatValue { value = 20f },
                "PercentRemaining" => new FloatValue { value = 60f },
                "IsBroken" => new BoolValue { value = false },
                "IsLowDurability" => new BoolValue { value = false },
                _ => null,
            };
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null || context.UnitInstance == null)
        {
            Debug.LogWarning("WeaponDurability: Could not retrieve context or unit from graph");
            return port.fieldName switch
            {
                "CurrentUses" or "MaxUses" or "UsesRemaining" or "PercentRemaining" =>
                    new FloatValue { value = 0f },
                _ => new BoolValue { value = false },
            };
        }

        // TODO: Implement weapon durability retrieval from equipped weapon when item system is added
        // Future implementation: var weapon = context.UnitInstance.GetEquippedWeapon();
        // Then return weapon.CurrentUses, weapon.MaxUses properties
        // Calculate UsesRemaining (MaxUses - CurrentUses)
        // Calculate PercentRemaining ((CurrentUses / MaxUses) * 100)
        // IsBroken: CurrentUses <= 0
        // IsLowDurability: PercentRemaining < lowDurabilityThreshold

        return port.fieldName switch
        {
            "CurrentUses" or "MaxUses" or "UsesRemaining" or "PercentRemaining" => new FloatValue
            {
                value = 0f,
            },
            _ => new BoolValue { value = false },
        };
    }
}
