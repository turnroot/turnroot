using Turnroot.Gameplay.Objects.Components;
using Turnroot.Utilities;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Represents the weapon matchup result between unit and enemy.
    /// This is mutually exclusive - only one state can be true at a time.
    /// </summary>
    public enum WeaponMatchup
    {
        /// <summary>Unit has weapon triangle advantage over enemy.</summary>
        Advantage = 0,

        /// <summary>Unit has weapon triangle disadvantage against enemy.</summary>
        Disadvantage = 1,

        /// <summary>Unit and enemy have the same weapon type.</summary>
        Neutral = 2,

        /// <summary>One or both weapons are not on the weapon triangle.</summary>
        NotOnTriangle = 3,
    }

    /// <summary>
    /// Struct to hold weapon matchup result for node output.
    /// </summary>
    [System.Serializable]
    public struct WeaponMatchupValue
    {
        public WeaponMatchup matchup;

        public bool IsAdvantage => matchup == WeaponMatchup.Advantage;
        public bool IsDisadvantage => matchup == WeaponMatchup.Disadvantage;
        public bool IsNeutral => matchup == WeaponMatchup.Neutral;
        public bool IsNotOnTriangle => matchup == WeaponMatchup.NotOnTriangle;
    }

    [CreateNodeMenu("Conditions/Weapon/Weapon Advantage")]
    [NodeLabel("Gets weapon advantage or same type")]
    public class WeaponAdvantageNode : SkillNode
    {
        /// <summary>
        /// The single, mutually exclusive matchup result.
        /// Use this output for new skill graphs instead of the individual bool outputs.
        /// </summary>
        [Output]
        public WeaponMatchupValue Matchup;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            var context = GetContextFromGraph(skillGraph);
            if (context == null || context.Unit.UnitInstance == null)
            {
                "WeaponAdvantage: Could not retrieve context or unit from graph".LogWarning();
                return port.fieldName == "Matchup"
                    ? new WeaponMatchupValue { matchup = WeaponMatchup.NotOnTriangle }
                    : new BoolValue { value = false };
            }

            // Calculate the matchup once
            var matchup = CalculateWeaponMatchup(context);

            return port.fieldName switch
            {
                "Matchup" => new WeaponMatchupValue { matchup = matchup },
                "UnitAdvantage" => new BoolValue { value = matchup == WeaponMatchup.Advantage },
                "EnemyAdvantage" => new BoolValue { value = matchup == WeaponMatchup.Disadvantage },
                "SameType" => new BoolValue { value = matchup == WeaponMatchup.Neutral },
                "NeitherOnTriangle" => new BoolValue
                {
                    value = matchup == WeaponMatchup.NotOnTriangle,
                },
                _ => new BoolValue { value = false },
            };
        }

        /// <summary>
        /// Calculates the weapon matchup between unit and enemy.
        /// Returns a single mutually exclusive result.
        /// </summary>
        private WeaponMatchup CalculateWeaponMatchup(
            Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
        )
        {
            // Get unit's equipped weapon type
            var unitWeaponType = GetEquippedWeaponType(context.Unit.UnitInstance);
            var unitTrianglePos = unitWeaponType?.TrianglePosition;

            // Get enemy's equipped weapon type
            var enemy =
                context.Participants.Targets != null && context.Participants.Targets.Count > 0
                    ? context.Participants.Targets[0]
                    : null;
            var enemyWeaponType = enemy != null ? GetEquippedWeaponType(enemy) : null;
            var enemyTrianglePos = enemyWeaponType?.TrianglePosition;

            // Check if either weapon is missing or not on triangle
            if (unitTrianglePos == null || enemyTrianglePos == null)
            {
                return WeaponMatchup.NotOnTriangle;
            }

            if (
                unitTrianglePos.Position == TrianglePositionEnum.NotOnTriangle
                || enemyTrianglePos.Position == TrianglePositionEnum.NotOnTriangle
            )
            {
                return WeaponMatchup.NotOnTriangle;
            }

            // Check for same type first
            if (unitTrianglePos.Equals(enemyTrianglePos))
            {
                return WeaponMatchup.Neutral;
            }

            // Check advantage/disadvantage
            if (unitTrianglePos.WinsAgainst(enemyTrianglePos))
            {
                return WeaponMatchup.Advantage;
            }

            if (unitTrianglePos.LosesTo(enemyTrianglePos))
            {
                return WeaponMatchup.Disadvantage;
            }

            // Fallback - shouldn't normally reach here
            return WeaponMatchup.Neutral;
        }

        private static WeaponType GetEquippedWeaponType(Characters.CharacterInstance character)
        {
            var inventory = character?.InventoryInstance;
            var weaponIndex = inventory?.GetEquippedWeaponIndex() ?? -1;
            return
                weaponIndex >= 0
                && inventory.InventoryItems != null
                && weaponIndex < inventory.InventoryItems.Count
                ? (inventory.InventoryItems[weaponIndex]?.Template?.WeaponType)
                : null;
        }
    }
}
