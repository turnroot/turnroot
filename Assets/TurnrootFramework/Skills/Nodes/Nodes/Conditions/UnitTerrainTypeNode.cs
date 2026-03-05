using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Evaluates the terrain type the unit is currently standing on.
    /// </summary>
    [CreateNodeMenu("Conditions/Position/Unit Terrain Type")]
    [NodeLabel("Gets the terrain type the unit is currently on")]
    public class UnitTerrainTypeNode : SkillNode
    {
        [Output]
        private BoolValue Ground;

        [Output]
        private BoolValue ShallowWater;

        [Output]
        private BoolValue DeepWater;

        [Output]
        private BoolValue Sand;

        [Output]
        private BoolValue Snow;

        [Output]
        private BoolValue Forest;

        [Output]
        private BoolValue Bushes;

        [Output]
        private BoolValue Lava;

        [Output]
        private BoolValue Bridge;

        [Output]
        private BoolValue Stairs;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                // Default to Ground in editor mode
                return new BoolValue { value = port.fieldName == "Ground" };
            }

            var context = GetContextFromGraph(skillGraph);
            var unit = ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Unit
            );

            if (unit == null)
            {
                "UnitTerrainType: Could not retrieve unit from context".LogWarning();
                return new BoolValue { value = false };
            }

            // Get terrain type from map grid at unit's position
            var terrainTypeName = GetTerrainTypeNameAtPosition(context, unit.MapGridPosition);

            if (string.IsNullOrEmpty(terrainTypeName))
            {
                return new BoolValue { value = port.fieldName == "Ground" }; // Default to ground
            }

            var cleanTerrain = terrainTypeName.Replace(" ", "");
            if (SkillDebug.VerboseExecutionLogs)
            {
                $"UnitTerrainTypeNode evaluated port {port.fieldName} against '{cleanTerrain}'".LogInfo();
            }
            // Compare terrain type name with the requested port
            return new BoolValue
            {
                value = cleanTerrain.Equals(
                    port.fieldName,
                    System.StringComparison.OrdinalIgnoreCase
                ),
            };
        }

        private static string GetTerrainTypeNameAtPosition(
            Gameplay.Combat.FundamentalComponents.Battles.BattleContext context,
            Vector2Int position
        )
        {
            var mapGrid = context?.MapGrid;
            if (mapGrid == null)
            {
                return null;
            }

            var gridPoint = mapGrid.GetGridPoint(position.x, position.y);
            if (gridPoint == null)
            {
                return null;
            }

            var terrainType = gridPoint.GetCachedTerrainType();
            return terrainType?.Name;
        }
    }
}
