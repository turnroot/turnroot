using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Position/Enemy Terrain Type")]
    [NodeLabel("Gets the terrain type the enemy is currently on")]
    public class EnemyTerrainTypeNode : SkillNode
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
            var enemy = ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Enemy
            );

            if (enemy == null)
            {
                Debug.LogWarning("EnemyTerrainType: Could not retrieve enemy from context");
                return new BoolValue { value = false };
            }

            // Get terrain type from map grid at enemy's position
            var terrainTypeName = GetTerrainTypeNameAtPosition(context, enemy.MapGridPosition);
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
            var mapGrid = context?.mapGrid;
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
