using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Unit Terrain Type")]
[NodeLabel("Gets the terrain type the unit is currently on")]
public class UnitTerrainType : SkillNode
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
        if (port.fieldName == "Ground")
        {
            BoolValue ground = new();
            // TODO: Implement runtime retrieval of terrain type
            return ground;
        }
        else if (port.fieldName == "ShallowWater")
        {
            BoolValue shallowWater = new();
            // TODO: Implement runtime retrieval of terrain type
            return shallowWater;
        }
        else if (port.fieldName == "DeepWater")
        {
            BoolValue deepWater = new();
            // TODO: Implement runtime retrieval of terrain type
            return deepWater;
        }
        else if (port.fieldName == "Sand")
        {
            BoolValue sand = new();
            // TODO: Implement runtime retrieval of terrain type
            return sand;
        }
        else if (port.fieldName == "Snow")
        {
            BoolValue snow = new();
            // TODO: Implement runtime retrieval of terrain type
            return snow;
        }
        else if (port.fieldName == "Forest")
        {
            BoolValue forest = new();
            // TODO: Implement runtime retrieval of terrain type
            return forest;
        }
        else if (port.fieldName == "Bushes")
        {
            BoolValue bushes = new();
            // TODO: Implement runtime retrieval of terrain type
            return bushes;
        }
        else if (port.fieldName == "Lava")
        {
            BoolValue lava = new();
            // TODO: Implement runtime retrieval of terrain type
            return lava;
        }
        else if (port.fieldName == "Bridge")
        {
            BoolValue bridge = new();
            // TODO: Implement runtime retrieval of terrain type
            return bridge;
        }
        else if (port.fieldName == "Stairs")
        {
            BoolValue stairs = new();
            // TODO: Implement runtime retrieval of terrain type
            return stairs;
        }
        return null;
    }
}
