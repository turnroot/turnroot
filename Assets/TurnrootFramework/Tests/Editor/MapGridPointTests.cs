using NUnit.Framework;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine;

public class MapGridPointTests
{
    [Test]
    public void PointAndFeaturePropertySetGet_WorksForTypes()
    {
        var go = new GameObject("map-grid-point");
        var p = go.AddComponent<MapGridPoint>();
        p.Initialize(0, 0);

        // Float/Bool point
        p.SetFloatPointProperty("f", 3.14f);
        Assert.AreEqual(3.14f, p.GetFloatPointProperty("f"));

        p.SetBoolPointProperty("flag", true);
        Assert.AreEqual(true, p.GetBoolPointProperty("flag"));

        // Unit and object item properties
        var character = CharacterInstance.Create(ScriptableObject.CreateInstance<CharacterData>());
        p.SetUnitPointProperty("unit", character);
        Assert.AreEqual(character, p.GetUnitPointProperty("unit"));

        var item = ScriptableObject.CreateInstance<ObjectItem>();
        var instance = new ObjectItemInstance(item);
        p.SetObjectItemPointProperty("item", instance);
        Assert.AreEqual(instance, p.GetObjectItemPointProperty("item"));

        // Feature-level properties (string/int no longer supported)
        p.SetFeatureTypeId("treasure");
        p.SetFloatFeatureProperty("lootValue", 1.5f);
        p.SetBoolFeatureProperty("opened", false);

        Assert.AreEqual(1.5f, p.GetFloatFeatureProperty("lootValue"));
        Assert.AreEqual(false, p.GetBoolFeatureProperty("opened"));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TerrainAndNeighbors_Behavior()
    {
        var gridGo = new GameObject("grid-parent");
        var mg = gridGo.AddComponent<MapGrid>();
        mg.CreateChildrenPoints();

        // Test terrain setter/getter (no TerrainTypes asset => SelectedTerrainType may be null)
        var point = mg.GetGridPoint(0, 0);
        Assert.IsNotNull(point);

        point.SetTerrainTypeId("nonexistent");
        // Should not throw and property remains set
        Assert.AreEqual("nonexistent", point.TerrainTypeId);

        // Test neighbor retrieval (cardinal and full)
        var neighborsAll = point.GetNeighbors(cardinal: false);
        Assert.IsNotNull(neighborsAll);

        var neighborsCardinal = point.GetNeighbors(cardinal: true);
        Assert.IsNotNull(neighborsCardinal);

        // Test that neighbors are correct (0,0 should have 2 cardinal neighbors in a 5x5 grid)
        Assert.AreEqual(2, neighborsCardinal.Count);

        // Test that all neighbors are correct (0,0 should have 3 total neighbors in a 5x5 grid)
        Assert.AreEqual(3, neighborsAll.Count);

        // Test that Cardinal and All neighbors are different
        Assert.AreNotEqual(neighborsCardinal.Count, neighborsAll.Count);

        // ConnectTo3DMapObject should be OK when no 3D object is assigned
        Assert.DoesNotThrow(() => mg.ConnectTo3DMapObject());

        Object.DestroyImmediate(gridGo);
    }
}
