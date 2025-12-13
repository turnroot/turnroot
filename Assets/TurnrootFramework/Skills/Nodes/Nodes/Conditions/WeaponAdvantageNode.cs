using Turnroot.Gameplay.Objects.Components;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Weapon/Weapon Advantage")]
    [NodeLabel("Gets weapon advantage or same type")]
    public class WeaponAdvantageNode : SkillNode
    {
        [Output]
        public BoolValue UnitAdvantage;

        [Output]
        public BoolValue EnemyAdvantage;

        [Output]
        public BoolValue SameType;

        [Output]
        public BoolValue NeitherOnTriangle;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                // Return defaults in editor mode
                return port.fieldName switch
                {
                    "UnitAdvantage" => new BoolValue { value = false },
                    "EnemyAdvantage" => new BoolValue { value = false },
                    "SameType" => new BoolValue { value = true },
                    "NeitherOnTriangle" => new BoolValue { value = false },
                    _ => null,
                };
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null || context.UnitInstance == null)
            {
                Debug.LogWarning("WeaponAdvantage: Could not retrieve context or unit from graph");
                return new BoolValue { value = false };
            }

            // Get unit's equipped weapon type
            var unitWeaponType = GetEquippedWeaponType(context.UnitInstance);
            var unitTrianglePos = unitWeaponType?.TrianglePosition;

            // Get enemy's equipped weapon type
            var enemy =
                context.Targets != null && context.Targets.Count > 0 ? context.Targets[0] : null;
            var enemyWeaponType = enemy != null ? GetEquippedWeaponType(enemy) : null;
            var enemyTrianglePos = enemyWeaponType?.TrianglePosition;

            // Determine advantage using TrianglePosition
            bool unitHasAdvantage = false;
            bool enemyHasAdvantage = false;
            bool sameType = false;
            bool neitherOnTriangle = false;

            if (unitTrianglePos != null && enemyTrianglePos != null)
            {
                // Check if either is not on triangle
                if (
                    unitTrianglePos.Position == TrianglePositionEnum.NotOnTriangle
                    || enemyTrianglePos.Position == TrianglePositionEnum.NotOnTriangle
                )
                {
                    neitherOnTriangle = true;
                }
                else
                {
                    // Use TrianglePosition's WinsAgainst/LosesTo methods
                    unitHasAdvantage = unitTrianglePos.WinsAgainst(enemyTrianglePos);
                    enemyHasAdvantage = unitTrianglePos.LosesTo(enemyTrianglePos);
                    sameType = unitTrianglePos.Equals(enemyTrianglePos);
                }
            }
            else
            {
                neitherOnTriangle = true;
            }

            return port.fieldName switch
            {
                "UnitAdvantage" => new BoolValue { value = unitHasAdvantage },
                "EnemyAdvantage" => new BoolValue { value = enemyHasAdvantage },
                "SameType" => new BoolValue { value = sameType },
                "NeitherOnTriangle" => new BoolValue { value = neitherOnTriangle },
                _ => new BoolValue { value = false },
            };
        }

        private static WeaponType GetEquippedWeaponType(
            Turnroot.Characters.CharacterInstance character
        )
        {
            var inventory = character?.InventoryInstance;
            var weaponIndex = inventory?.GetEquippedWeaponIndex() ?? -1;
            return weaponIndex >= 0
                && inventory.InventoryItems != null
                && weaponIndex < inventory.InventoryItems.Count
                ? (inventory.InventoryItems[weaponIndex]?.Template?.WeaponType)
                : null;
        }
    }
}
