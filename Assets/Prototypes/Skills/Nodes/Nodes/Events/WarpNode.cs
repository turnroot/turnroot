using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Warp")]
    [NodeLabel("Teleport ally to caster's position")]
    public class WarpNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Tooltip("Warp mode: bring ally to caster or send caster to ally")]
        public WarpMode mode = WarpMode.AllyToCaster;

        [Tooltip("Maximum distance in tiles (0 = unlimited)")]
        [Range(0, 20)]
        public int maxDistance = 0;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("Warp: No context provided");
                return;
            }

            // Get the direction from custom data (set by player during gameplay)
            Direction allyDirection = context.GetCustomData<Direction>(
                "SelectedDirection",
                Direction.Center
            );

            // Get the unit in the specified direction
            if (context.AdjacentUnits == null)
            {
                Debug.LogWarning("Warp: No adjacent units data");
                return;
            }

            var ally = context.AdjacentUnits.GetUnit(allyDirection);
            if (ally == null)
            {
                Debug.LogWarning($"Warp: No unit at {allyDirection}");
                return;
            }

            // Store warp command in CustomData
            var warpData = new
            {
                AllyId = ally.Id,
                CasterId = context.UnitInstance.Id,
                Mode = mode,
                MaxDistance = maxDistance,
            };

            context.SetCustomData("Warp", warpData);

            string modeText = mode == WarpMode.AllyToCaster ? "to caster" : "to ally";
            string distanceText =
                maxDistance > 0 ? $" (max {maxDistance} tiles)" : " (unlimited range)";
            Debug.Log($"Warp: Will warp {modeText}{distanceText}");
        }
    }

    public enum WarpMode
    {
        AllyToCaster, // Bring ally to caster's position (Rescue)
        CasterToAlly, // Send caster to ally's position (Warp staff target)
    }
}
