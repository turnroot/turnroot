using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

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
            Debug.LogWarning("UnitTerrainType: Could not retrieve unit from context");
            return new BoolValue { value = false };
        }

        // TODO: Implement terrain type retrieval when positioning/map system is added
        // Future implementation:
        // var position = context.GetUnitPosition();
        // var terrainType = context.Map.GetTerrainAt(position);
        // Then return new BoolValue { value = terrainType == TerrainType.Ground/Forest/etc };
        // For now, return false for all terrain types

        return new BoolValue { value = false };
    }
}
