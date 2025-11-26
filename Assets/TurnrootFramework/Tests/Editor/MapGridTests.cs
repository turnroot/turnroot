using NUnit.Framework;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine;

public class MapGridTests
{
    [Test]
    public void CreateChildrenPoints_PopulatesGridPoints()
    {
        var go = new GameObject("test-mapgrid");
        var mg = go.AddComponent<MapGrid>();

        // Ensure clean
        mg.ClearGrid();

        mg.CreateChildrenPoints();

        int expected = mg.GridWidth * mg.GridHeight;
        Assert.That(
            go.transform.childCount,
            Is.EqualTo(expected),
            "Should create width*height child points"
        );

        // Check a sample grid point exists and has MapGridPoint component
        var child = go.transform.GetChild(0).gameObject;
        Assert.IsNotNull(child.GetComponent<MapGridPoint>());

        // Cleanup
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SaveLoadFeatureLayer_RoundTripProperties()
    {
        var go = new GameObject("mapgrid-rt");
        var mg = go.AddComponent<MapGrid>();
        mg.CreateChildrenPoints();

        var p00 = mg.GetGridPoint(0, 0);
        Assert.IsNotNull(p00);

        // Assign a feature and feature properties
        p00.SetFeatureTypeId("chest");
        // string property removed — use float property instead for this test
        p00.SetFloatFeatureProperty("value", 12.5f);
        p00.SetBoolFeatureProperty("opened", false);

        // Unit and object item feature properties
        var charTemplate = ScriptableObject.CreateInstance<CharacterData>();
        var charInstance = Turnroot.Characters.CharacterInstance.Create(charTemplate);
        p00.SetUnitFeatureProperty("spawn", charInstance);

        var item = ScriptableObject.CreateInstance<Turnroot.Gameplay.Objects.ObjectItem>();
        var itemInstance = new Turnroot.Gameplay.Objects.ObjectItemInstance(item);
        p00.SetObjectItemFeatureProperty("reward", itemInstance);

        mg.SaveFeatureLayer();

        // Mutate properties to ensure load actually restores
        p00.SetFloatFeatureProperty("value", 0f);
        p00.SetBoolFeatureProperty("opened", true);

        mg.LoadFeatureLayer();

        Assert.AreEqual(12.5f, p00.GetFloatFeatureProperty("value"));
        Assert.AreEqual(false, p00.GetBoolFeatureProperty("opened"));
        // Check unit and object item restore
        Assert.AreEqual(charInstance, p00.GetUnitFeatureProperty("spawn"));
        Assert.AreEqual(itemInstance, p00.GetObjectItemFeatureProperty("reward"));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AddRemoveRowsAndColumns_AdjustsChildCounts()
    {
        var go = new GameObject("mapgrid-mod");
        var mg = go.AddComponent<MapGrid>();
        mg.ClearGrid();
        mg.CreateChildrenPoints();

        int initial = go.transform.childCount;

        mg.AddRow();
        Assert.Greater(go.transform.childCount, initial);

        int afterAddRow = go.transform.childCount;
        mg.AddColumn();
        Assert.Greater(go.transform.childCount, afterAddRow);

        // Remove operations should decrease or keep counts safe
        mg.RemoveColumn();
        mg.RemoveRow();

        // Cleanup
        Object.DestroyImmediate(go);
    }
}
