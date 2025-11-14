using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGridPoint))]
public class MapGridPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw specific serialized fields (gizmo radius, terrain asset reference)
        var gizmoProp = serializedObject.FindProperty("_gizmoRadius");
        EditorGUILayout.PropertyField(gizmoProp);

        var point = (MapGridPoint)target;

        // Provide a nicer terrain selection UI if possible
        TerrainTypes asset = point != null ? GetAssignedOrFirstTerrainTypes(point) : null;

        if (asset == null)
        {
            EditorGUILayout.HelpBox(
                "No `TerrainTypes` asset found in the project. Create one via Create -> Turnroot -> Game Settings -> Terrain Types.",
                MessageType.Warning
            );
            serializedObject.ApplyModifiedProperties();
            return;
        }

        var types = asset.Types;
        if (types == null || types.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "The assigned TerrainTypes asset contains no types.",
                MessageType.Warning
            );
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // Build display names and id map
        string[] names = new string[types.Length];
        string[] ids = new string[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            names[i] = types[i] != null ? types[i].Name : "(null)";
            ids[i] = types[i] != null ? types[i].Id : string.Empty;
        }

        // Find the id property and show popup
        var idProp = serializedObject.FindProperty("_terrainTypeId");
        string curId = idProp.stringValue;
        int curIndex = 0;
        for (int i = 0; i < ids.Length; i++)
            if (ids[i] == curId)
            {
                curIndex = i;
                break;
            }

        int newIndex = EditorGUILayout.Popup("Terrain Type", curIndex, names);
        if (newIndex != curIndex)
            idProp.stringValue = ids[newIndex];

        // We load the TerrainTypes asset centrally (Resources or project asset). No per-point asset field.

        serializedObject.ApplyModifiedProperties();
    }

    private TerrainTypes GetAssignedOrFirstTerrainTypes(MapGridPoint point)
    {
        // Try assigned asset first
        var ty = typeof(MapGridPoint);
        var fi = ty.GetField(
            "_terrainTypesAsset",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );
        if (fi != null)
        {
            var val = fi.GetValue(point) as TerrainTypes;
            if (val != null)
                return val;
        }

        // Fallback: find first TerrainTypes asset in project
        var guids = AssetDatabase.FindAssets("t:TerrainTypes");
        if (guids != null && guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TerrainTypes>(path);
        }
        return null;
    }
}
