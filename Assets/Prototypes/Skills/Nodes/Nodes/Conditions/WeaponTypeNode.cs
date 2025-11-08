using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Weapon/Weapon Type")]
[NodeLabel("Gets the weapon type information")]
public class WeaponTypeNode : SkillNode
{
    [Output]
    public StringValue TypeName;

    [Output]
    public BoolValue IsSword;

    [Output]
    public BoolValue IsLance;

    [Output]
    public BoolValue IsAxe;

    [Output]
    public BoolValue IsBow;

    [Output]
    public BoolValue IsTome;

    [Output]
    public BoolValue IsStaff;

    [Output]
    public BoolValue IsDagger;

    [Output]
    public BoolValue IsDragonstone;

    [Output]
    public BoolValue IsBeaststone;

    public override object GetValue(NodePort port)
    {
        var skillGraph = graph as SkillGraph;
        if (skillGraph == null || !Application.isPlaying)
        {
            // Return defaults in editor mode
            return port.fieldName switch
            {
                "TypeName" => new StringValue { value = "Sword" },
                "IsSword" => new BoolValue { value = true },
                "IsLance" => new BoolValue { value = false },
                "IsAxe" => new BoolValue { value = false },
                "IsBow" => new BoolValue { value = false },
                "IsTome" => new BoolValue { value = false },
                "IsStaff" => new BoolValue { value = false },
                "IsDagger" => new BoolValue { value = false },
                "IsDragonstone" => new BoolValue { value = false },
                "IsBeaststone" => new BoolValue { value = false },
                _ => null,
            };
        }

        var context = GetContextFromGraph(skillGraph);
        if (context == null || context.UnitInstance == null)
        {
            Debug.LogWarning("WeaponType: Could not retrieve context or unit from graph");
            return port.fieldName switch
            {
                "TypeName" => new StringValue { value = "" },
                _ => new BoolValue { value = false },
            };
        }

        // TODO: Implement weapon type retrieval from equipped weapon when item system is added
        // Future implementation: var weapon = context.UnitInstance.GetEquippedWeapon();
        // Then check weapon.WeaponType property against WeaponType enum values
        // Example: return new BoolValue { value = weapon.WeaponType == WeaponType.Sword };

        return port.fieldName switch
        {
            "TypeName" => new StringValue { value = "" },
            _ => new BoolValue { value = false },
        };
    }
}
