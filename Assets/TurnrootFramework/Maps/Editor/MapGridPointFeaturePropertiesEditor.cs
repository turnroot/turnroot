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

        // String and Int properties have been removed (no-op)

        if (GUILayout.Button("Add Bool"))
        {
            AddProperty(
                src,
                "Add Bool Property",
                () =>
                    src.boolProperties.Add(
                        new MapGridPropertyBase.BoolProperty
                        {
                            key = "new_bool",
                            value = false,
                        }
                    )
            );
        }

        // Int type removed. Expose other useful add buttons below.
        if (GUILayout.Button("Add Event"))
        {
            AddProperty(
                src,
                "Add Event Property",
                () =>
                    src.eventProperties.Add(
                        new MapGridPropertyBase.EventProperty
                        {
                            key = "new_event",
                            value = new UnityEngine.Events.UnityEvent(),
                        }
                    )
            );
        }

        if (GUILayout.Button("Add Unit"))
        {
            AddProperty(
                src,
                "Add Unit Property",
                () =>
                    src.unitProperties.Add(
                        new MapGridPropertyBase.UnitProperty { key = "new_unit", value = null }
                    )
            );
        }

        if (GUILayout.Button("Add ObjectItem"))
        {
            AddProperty(
                src,
                "Add ObjectItem Property",
                () =>
                    src.objectItemProperties.Add(
                        new MapGridPropertyBase.ObjectItemProperty
                        {
                            key = "new_object",
                            value = null,
                        }
                    )
            );
        }

        if (GUILayout.Button("Add Float"))
        {
            AddProperty(
                src,
                "Add Float Property",
                () =>
                    src.floatProperties.Add(
                        new MapGridPropertyBase.FloatProperty
                        {
                            key = "new_float",
                            value = 0f,
                        }
                    )
            );
        }

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
            {
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }
    }
}
