using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves weapon durability information including current uses, maximum uses, and remaining percentage.
    /// </summary>
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
                return port.fieldName switch
                {
                    "CurrentUses" or "MaxUses" or "UsesRemaining" or "PercentRemaining" =>
                        new FloatValue { value = 0f },
                    _ => new BoolValue { value = false },
                };
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null || context.Unit.UnitInstance == null)
            {
                "WeaponDurability: Could not retrieve context or unit from graph".LogWarning();
                return port.fieldName switch
                {
                    "CurrentUses" or "MaxUses" or "UsesRemaining" or "PercentRemaining" =>
                        new FloatValue { value = 0f },
                    _ => new BoolValue { value = false },
                };
            }

            // Get equipped weapon from character inventory
            var inventory = context.Unit.UnitInstance.InventoryInstance;
            var weaponIndex = inventory?.GetEquippedWeaponIndex() ?? -1;
            if (
                weaponIndex < 0
                || inventory.InventoryItems == null
                || weaponIndex >= inventory.InventoryItems.Count
            )
            {
                "WeaponDurability: No weapon equipped".LogWarning();
                return port.fieldName switch
                {
                    "CurrentUses" or "MaxUses" or "UsesRemaining" or "PercentRemaining" =>
                        new FloatValue { value = 0f },
                    _ => new BoolValue { value = false },
                };
            }

            var equippedWeapon = inventory.InventoryItems[weaponIndex];
            var template = equippedWeapon?.Template;
            if (template == null || !template.Durability)
            {
                // Weapon has infinite durability
                return port.fieldName switch
                {
                    "CurrentUses" => new FloatValue { value = 0f },
                    "MaxUses" => new FloatValue { value = float.MaxValue },
                    "UsesRemaining" => new FloatValue { value = float.MaxValue },
                    "PercentRemaining" => new FloatValue { value = 100f },
                    "IsBroken" => new BoolValue { value = false },
                    "IsLowDurability" => new BoolValue { value = false },
                    _ => new BoolValue { value = false },
                };
            }

            // Get durability values from the weapon instance
            int maxUses = template.MaxUses;
            int usesRemaining = equippedWeapon.RemainingUses;
            int currentUses = equippedWeapon.CurrentUses;
            float percentRemaining = maxUses > 0 ? (usesRemaining / (float)maxUses) * 100f : 0f;
            bool isBroken = usesRemaining <= 0;
            bool isLowDurability = percentRemaining < lowDurabilityThreshold;

            return port.fieldName switch
            {
                "CurrentUses" => new FloatValue { value = maxUses - usesRemaining },
                "MaxUses" => new FloatValue { value = maxUses },
                "UsesRemaining" => new FloatValue { value = usesRemaining },
                "PercentRemaining" => new FloatValue { value = percentRemaining },
                "IsBroken" => new BoolValue { value = isBroken },
                "IsLowDurability" => new BoolValue { value = isLowDurability },
                _ => new BoolValue { value = false },
            };
        }
    }
}
