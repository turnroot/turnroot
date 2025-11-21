using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGridFeatureProperties))]
public class MapGridFeaturePropertiesEditor : Editor
{
    SerializedProperty featureIdProp;
    SerializedProperty featureNameProp;

    void OnEnable()
    {
        featureIdProp = serializedObject.FindProperty("featureId");
        featureNameProp = serializedObject.FindProperty("featureName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Build a list of available feature types (skip the None entry)
        var enumValues = Enum.GetValues(typeof(MapGridPointFeature.FeatureType));
        var names = new List<string>();
        var ids = new List<string>();

        foreach (MapGridPointFeature.FeatureType t in enumValues)
        {
            if (t == MapGridPointFeature.FeatureType.None)
                continue;
            names.Add(t.ToString());
            ids.Add(MapGridPointFeature.IdFromType(t));
        }

        // Determine current selection index
        string curId = featureIdProp.stringValue ?? string.Empty;
        int curIndex = ids.IndexOf(curId);
        if (curIndex < 0)
            curIndex = 0; // default to first option if current id not found

        // Popup to choose feature type
        int chosen = EditorGUILayout.Popup("Feature Type", curIndex, names.ToArray());
        featureIdProp.stringValue = ids[Mathf.Clamp(chosen, 0, ids.Count - 1)];

        // Friendly name field
        EditorGUILayout.PropertyField(featureNameProp, new GUIContent("Feature Name"));

        // Draw remaining properties (exclude these so they aren't duplicated)
        DrawPropertiesExcluding(serializedObject, "m_Script", "featureId", "featureName");

        // Show validation: this implementation requires a featureId
        if (string.IsNullOrEmpty(featureIdProp.stringValue))
        {
            EditorGUILayout.HelpBox(
                "Feature ID is required for this asset. Please select a Feature Type.",
                MessageType.Error
            );
        }

        serializedObject.ApplyModifiedProperties();
    }
}
