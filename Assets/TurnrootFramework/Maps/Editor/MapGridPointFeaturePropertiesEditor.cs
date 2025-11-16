using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGridPointFeatureProperties))]
public class MapGridPointFeaturePropertiesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var src = (MapGridPointFeatureProperties)target;
        serializedObject.Update();

        DrawFeatureIdentitySection(src);
        EditorGUILayout.Space();
        DrawAddPropertyButtons(src);
        DrawRemainingProperties();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawFeatureIdentitySection(MapGridPointFeatureProperties src)
    {
        EditorGUILayout.LabelField("Feature identity", EditorStyles.boldLabel);

        var featureTypes = System
            .Enum.GetValues(typeof(MapGridPointFeature.FeatureType))
            .Cast<MapGridPointFeature.FeatureType>()
            .Where(t => t != MapGridPointFeature.FeatureType.None)
            .ToList();

        var currentType = MapGridPointFeature.TypeFromId(src.featureId);
        int selectedIndex = featureTypes.IndexOf(currentType);
        int newIndex = EditorGUILayout.Popup(
            "Feature Type",
            Mathf.Max(0, selectedIndex),
            featureTypes.Select(t => t.ToString()).ToArray()
        );

        if (newIndex != selectedIndex && newIndex >= 0 && newIndex < featureTypes.Count)
        {
            Undo.RecordObject(src, "Change Feature Type");
            src.featureId = MapGridPointFeature.IdFromType(featureTypes[newIndex]);
            EditorUtility.SetDirty(src);
        }

        src.featureName = EditorGUILayout.TextField("Friendly Name", src.featureName);
    }

    private void DrawAddPropertyButtons(MapGridPointFeatureProperties src)
    {
        EditorGUILayout.LabelField("Add Property", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add String"))
            AddProperty(
                src,
                "Add String Property",
                () =>
                    src.stringProperties.Add(
                        new MapGridPointFeatureProperties.StringProperty
                        {
                            key = "new_key",
                            value = "",
                        }
                    )
            );

        if (GUILayout.Button("Add Bool"))
            AddProperty(
                src,
                "Add Bool Property",
                () =>
                    src.boolProperties.Add(
                        new MapGridPointFeatureProperties.BoolProperty
                        {
                            key = "new_bool",
                            value = false,
                        }
                    )
            );

        if (GUILayout.Button("Add Int"))
            AddProperty(
                src,
                "Add Int Property",
                () =>
                    src.intProperties.Add(
                        new MapGridPointFeatureProperties.IntProperty { key = "new_int", value = 0 }
                    )
            );

        if (GUILayout.Button("Add Float"))
            AddProperty(
                src,
                "Add Float Property",
                () =>
                    src.floatProperties.Add(
                        new MapGridPointFeatureProperties.FloatProperty
                        {
                            key = "new_float",
                            value = 0f,
                        }
                    )
            );

        EditorGUILayout.EndHorizontal();
    }

    private void AddProperty(
        MapGridPointFeatureProperties src,
        string undoLabel,
        System.Action addAction
    )
    {
        Undo.RecordObject(src, undoLabel);
        addAction();
        EditorUtility.SetDirty(src);
    }

    private void DrawRemainingProperties()
    {
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "featureId" || prop.name == "featureName")
                continue;
            EditorGUILayout.PropertyField(prop, true);
        }
    }
}
