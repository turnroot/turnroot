using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Durability")]
[NodeLabel("Gets the weapon durability information")]
public class WeaponDurability : SkillNode
{
    [Output]
    FloatValue CurrentUses;

    [Output]
    FloatValue MaxUses;

    [Output]
    FloatValue UsesRemaining;

    [Output]
    FloatValue PercentRemaining;

    [Output]
    BoolValue IsBroken;

    [Output]
    BoolValue IsLowDurability;

    [Tooltip("Threshold for low durability warning (percentage)")]
    [Range(0, 100)]
    public float lowDurabilityThreshold = 25f;

    public override object GetValue(NodePort port)
    {
        if (port.fieldName == "CurrentUses")
        {
            FloatValue currentUses = new();
            // TODO: Implement runtime retrieval of current weapon uses
            return currentUses;
        }
        else if (port.fieldName == "MaxUses")
        {
            FloatValue maxUses = new();
            // TODO: Implement runtime retrieval of maximum weapon uses
            return maxUses;
        }
        else if (port.fieldName == "UsesRemaining")
        {
            FloatValue usesRemaining = new();
            // TODO: Implement runtime calculation of uses remaining
            return usesRemaining;
        }
        else if (port.fieldName == "PercentRemaining")
        {
            FloatValue percentRemaining = new();
            // TODO: Implement runtime calculation of percent remaining
            return percentRemaining;
        }
        else if (port.fieldName == "IsBroken")
        {
            BoolValue isBroken = new();
            // TODO: Implement runtime check if weapon is broken (uses <= 0)
            return isBroken;
        }
        else if (port.fieldName == "IsLowDurability")
        {
            BoolValue isLowDurability = new();
            // TODO: Implement runtime check if durability is below threshold
            return isLowDurability;
        }
        return null;
    }
}
