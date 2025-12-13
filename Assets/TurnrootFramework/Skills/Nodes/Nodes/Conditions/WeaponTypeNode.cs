using Turnroot.Gameplay.Objects.Components;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Weapon/Weapon Type")]
    [NodeLabel("Gets the weapon type information")]
    public class WeaponTypeNode : SkillNode
    {
        [Tooltip(
            "The weapon type to compare against (optional - leave empty to just get current weapon type info)"
        )]
        public WeaponType targetWeaponType;

        [Output]
        public StringValue TypeName;

        [Output]
        public BoolValue MatchesTarget;

        [Output]
        public BoolValue IsMagic;

        [Output]
        public BoolValue IsPhysical;

        [Output]
        public BoolValue IsOnTriangle;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                return port.fieldName switch
                {
                    "TypeName" => new StringValue { value = string.Empty },
                    "MatchesTarget" => new BoolValue { value = false },
                    "IsMagic" => new BoolValue { value = false },
                    "IsPhysical" => new BoolValue { value = true },
                    "IsOnTriangle" => new BoolValue { value = true },
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

            // Get equipped weapon from character inventory
            var inventory = context.UnitInstance.InventoryInstance;
            var weaponIndex = inventory?.GetEquippedWeaponIndex() ?? -1;
            if (
                weaponIndex < 0
                || inventory.InventoryItems == null
                || weaponIndex >= inventory.InventoryItems.Count
            )
            {
                Debug.LogWarning("WeaponType: No weapon equipped");
                return port.fieldName switch
                {
                    "TypeName" => new StringValue { value = "" },
                    _ => new BoolValue { value = false },
                };
            }

            var equippedWeapon = inventory.InventoryItems[weaponIndex];
            var weaponType = equippedWeapon?.Template?.WeaponType;
            var weaponTypeName = weaponType?.Name ?? "";
            var trianglePosition = weaponType?.TrianglePosition;

            bool matchesTarget =
                targetWeaponType != null
                && weaponType != null
                && WeaponTypeHelpers.Equals(weaponType, targetWeaponType);
            bool isMagic = weaponType?.IsMagic ?? false;
            bool isOnTriangle = trianglePosition?.Position != TrianglePositionEnum.NotOnTriangle;

            return port.fieldName switch
            {
                "TypeName" => new StringValue { value = weaponTypeName },
                "MatchesTarget" => new BoolValue { value = matchesTarget },
                "IsMagic" => new BoolValue { value = isMagic },
                "IsPhysical" => new BoolValue { value = !isMagic },
                "IsOnTriangle" => new BoolValue { value = isOnTriangle },
                _ => new BoolValue { value = false },
            };
        }
    }
}
