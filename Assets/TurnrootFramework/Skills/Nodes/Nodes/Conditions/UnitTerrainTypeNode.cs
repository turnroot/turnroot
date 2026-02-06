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
        BoolValue Ground;

        [Output]
        BoolValue ShallowWater;

        [Output]
        BoolValue DeepWater;

        [Output]
        BoolValue Sand;

        [Output]
        BoolValue Snow;

        [Output]
        BoolValue Forest;

        [Output]
        BoolValue Bushes;

        [Output]
        BoolValue Lava;

        [Output]
        BoolValue Bridge;

        [Output]
        BoolValue Stairs;

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
#if UNITY_EDITOR
                Debug.LogWarning("UnitTerrainType: Could not retrieve unit from context");
#endif
                return new BoolValue { value = false };
            }

            // Get terrain type from map grid at unit's position
            var terrainTypeName = GetTerrainTypeNameAtPosition(context, unit.MapGridPosition);
            if (string.IsNullOrEmpty(terrainTypeName))
            {
                return new BoolValue { value = port.fieldName == "Ground" }; // Default to ground
            }

            // Compare terrain type name with the requested port
            return new BoolValue
            {
                value = terrainTypeName.Equals(
                    port.fieldName,
                    System.StringComparison.OrdinalIgnoreCase
                ),
            };
        }

        /// <summary>
        /// Gets the terrain type name at the specified grid position.
        /// </summary>
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
