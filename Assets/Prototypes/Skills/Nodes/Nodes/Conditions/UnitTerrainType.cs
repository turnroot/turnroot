using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conditions/Position/Unit Terrain Type")]
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

    private static readonly System.Collections.Generic.Dictionary<
        string,
        System.Func<BoolValue>
    > terrainTypeFactories = new System.Collections.Generic.Dictionary<
        string,
        System.Func<BoolValue>
    >()
    {
        {
            "Ground",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "ShallowWater",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "DeepWater",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "Sand",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "Snow",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "Forest",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "Bushes",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "Lava",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "Bridge",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
        {
            "Stairs",
            () =>
            {
                var v = new BoolValue(); /* TODO: Implement runtime retrieval of terrain type */
                return v;
            }
        },
    };

    public override object GetValue(NodePort port)
    {
        if (terrainTypeFactories.TryGetValue(port.fieldName, out var factory))
        {
            return factory();
        }
        return null;
    }
}
