using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves weapon range information including min/max range and melee/ranged status.
    /// </summary>
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
            if (context == null || context.Unit.UnitInstance == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("WeaponRange: Could not retrieve context or unit from graph");
#endif
                return port.fieldName switch
                {
                    "MinRange" or "MaxRange" => new FloatValue { value = 0f },
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
#if UNITY_EDITOR
                Debug.LogWarning("WeaponRange: No weapon equipped");
#endif
                return port.fieldName switch
                {
                    "MinRange" or "MaxRange" => new FloatValue { value = 0f },
                    _ => new BoolValue { value = false },
                };
            }

            var equippedWeapon = inventory.InventoryItems[weaponIndex];
            var template = equippedWeapon?.Template;
            if (template == null)
            {
                return port.fieldName switch
                {
                    "MinRange" or "MaxRange" => new FloatValue { value = 0f },
                    _ => new BoolValue { value = false },
                };
            }

            int minRange = template.LowerRange;
            int maxRange = template.UpperRange;
            bool isMelee = maxRange <= 1;
            bool isRanged = maxRange >= 2;

            // Get combat distance to determine counterattack capability
            var enemy =
                context.Participants.Targets != null && context.Participants.Targets.Count > 0
                    ? context.Participants.Targets[0]
                    : null;
            int combatDistance = 1;
            if (enemy != null)
            {
                var unitPos = context.Unit.UnitInstance.MapGridPosition;
                var enemyPos = enemy.MapGridPosition;
                combatDistance =
                    Mathf.Abs(unitPos.x - enemyPos.x) + Mathf.Abs(unitPos.y - enemyPos.y);
            }
            bool canCounterattack = combatDistance >= minRange && combatDistance <= maxRange;

            return port.fieldName switch
            {
                "MinRange" => new FloatValue { value = minRange },
                "MaxRange" => new FloatValue { value = maxRange },
                "IsMelee" => new BoolValue { value = isMelee },
                "IsRanged" => new BoolValue { value = isRanged },
                "CanCounterattack" => new BoolValue { value = canCounterattack },
                _ => new BoolValue { value = false },
            };
        }
    }
}
