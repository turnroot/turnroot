using Turnroot.Gameplay.Objects.Components;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves weapon type information for the first target (enemy). Mirrors WeaponTypeNode
    /// but reads from <c>context.Participants.Targets[0]</c> instead of the unit.
    /// Useful for breaker-type skills (Swordbreaker, Lancebreaker, etc.) that trigger
    /// based on what weapon the enemy is carrying.
    /// </summary>
    [CreateNodeMenu("Conditions/Weapon/Enemy Weapon Type")]
    [NodeLabel("Gets the target enemy's weapon type information")]
    public class EnemyWeaponTypeNode : SkillNode
    {
        [Tooltip("The weapon type to compare against (leave empty to just get info)")]
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
            if (
                context == null
                || context.Participants?.Targets == null
                || context.Participants.Targets.Count == 0
            )
            {
                "EnemyWeaponType: No target in context".LogWarning();
                return port.fieldName switch
                {
                    "TypeName" => new StringValue { value = "" },
                    _ => new BoolValue { value = false },
                };
            }

            var enemy = context.Participants.Targets[0];
            var equippedWeapon = enemy?.GetEquippedWeapon();
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
