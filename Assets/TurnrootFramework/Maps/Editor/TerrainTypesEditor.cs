using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainTypes))]
public class TerrainTypesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var typesProp = serializedObject.FindProperty("_types");

        EditorGUILayout.LabelField("Terrain Types", EditorStyles.boldLabel);

        if (typesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No terrain types defined.", MessageType.Info);
        }

        for (int i = 0; i < typesProp.arraySize; i++)
        {
            var elem = typesProp.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(GUI.skin.box);

            var nameProp = elem.FindPropertyRelative("_name");
            string header = !string.IsNullOrEmpty(nameProp?.stringValue)
                ? nameProp.stringValue
                : $"Terrain {i}";

            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nameProp, new GUIContent("Name"));
            EditorGUILayout.PropertyField(
                elem.FindPropertyRelative("_editorColor"),
                new GUIContent("Editor Color")
            );

            DrawKnobRow(
                "Costs",
                elem,
                new[]
                {
                    ("_costWalk", "Walk", 1f, 5f, 0.1f, new Color(1f, 0.6f, 0f)),
                    ("_costFly", "Fly", 1f, 5f, 0.1f, new Color(1f, 0.6f, 0f)),
                    ("_costRide", "Ride", 1f, 5f, 0.1f, new Color(1f, 0.6f, 0f)),
                    ("_costMagic", "Magic", 1f, 5f, 0.1f, new Color(1f, 0.6f, 0f)),
                    ("_costArmor", "Armor", 1f, 5f, 0.1f, new Color(1f, 0.6f, 0f)),
                },
                true
            );

            DrawKnobRow(
                "Health",
                elem,
                new[]
                {
                    ("_healthChangePerTurnWalk", "Walk", -20f, 20f, 1f, Color.green),
                    ("_healthChangePerTurnRiding", "Ride", -20f, 20f, 1f, Color.green),
                    ("_healthChangePerTurnFlying", "Fly", -20f, 20f, 1f, Color.green),
                },
                false
            );

            DrawKnobRow(
                "Defense",
                elem,
                new[]
                {
                    ("_defenseBonusWalk", "Walk", -40f, 40f, 1f, Color.cyan),
                    ("_defenseBonusRiding", "Ride", -40f, 40f, 1f, Color.cyan),
                    ("_defenseBonusFlying", "Fly", -40f, 40f, 1f, Color.cyan),
                },
                false
            );

            DrawKnobRow(
                "Avoid",
                elem,
                new[]
                {
                    ("_avoidBonusWalk", "Walk", -40f, 40f, 1f, Color.yellow),
                    ("_avoidBonusRiding", "Ride", -40f, 40f, 1f, Color.yellow),
                    ("_avoidBonusFlying", "Fly", -40f, 40f, 1f, Color.yellow),
                },
                false
            );

            EditorGUILayout.Space();
            if (GUILayout.Button("Remove"))
            {
                typesProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Add New Terrain Type"))
        {
            AddNewTerrainType(typesProp);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawKnobRow(
        string label,
        SerializedProperty elem,
        (string prop, string label, float min, float max, float step, Color color)[] knobs,
        bool isFloat
    )
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        foreach (var (propName, knobLabel, min, max, step, color) in knobs)
        {
            var prop = elem.FindPropertyRelative(propName);
            if (prop == null)
                continue;

            EditorGUILayout.BeginVertical(GUILayout.Width(64));
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(48));
            float value = isFloat ? prop.floatValue : prop.intValue;
            float newValue = KnobGUILayout.Knob(
                new Vector2(48, 48),
                value,
                min,
                max,
                "",
                Color.gray,
                color,
                false,
                isFloat ? "0.0" : "0",
                step,
                -90f,
                180f
            );

            if (isFloat)
                prop.floatValue = Mathf.Round(newValue * 10f) / 10f;
            else
                prop.intValue = Mathf.RoundToInt(newValue);
            EditorGUILayout.EndVertical();

            GUILayout.Space(4);
            EditorGUILayout.BeginVertical();
            string displayValue = isFloat
                ? prop.floatValue.ToString("0.0")
                : prop.intValue.ToString();
            EditorGUILayout.LabelField(
                displayValue,
                EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(36)
            );
            EditorGUILayout.LabelField(
                knobLabel,
                EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(36)
            );
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void AddNewTerrainType(SerializedProperty typesProp)
    {
        int newIndex = typesProp.arraySize;
        typesProp.InsertArrayElementAtIndex(newIndex);
        var newElem = typesProp.GetArrayElementAtIndex(newIndex);

        SetPropertyValue(newElem, "_id", System.Guid.NewGuid().ToString());
        SetPropertyValue(newElem, "_name", "New Terrain");
        SetPropertyValue(newElem, "_editorColor", Color.white);

        foreach (
            var prop in new[] { "_costWalk", "_costFly", "_costRide", "_costMagic", "_costArmor" }
        )
            SetPropertyValue(newElem, prop, 1f);

        foreach (
            var prop in new[]
            {
                "_healthChangePerTurnWalk",
                "_healthChangePerTurnRiding",
                "_healthChangePerTurnFlying",
                "_defenseBonusWalk",
                "_defenseBonusRiding",
                "_defenseBonusFlying",
                "_avoidBonusWalk",
                "_avoidBonusRiding",
                "_avoidBonusFlying",
            }
        )
            SetPropertyValue(newElem, prop, 0);
    }

    private void SetPropertyValue(SerializedProperty parent, string propName, object value)
    {
        var prop = parent.FindPropertyRelative(propName);
        if (prop == null)
            return;

        switch (value)
        {
            case string s:
                prop.stringValue = s;
                break;
            case float f:
                prop.floatValue = f;
                break;
            case int i:
                prop.intValue = i;
                break;
            case Color c:
                prop.colorValue = c;
                break;
        }
    }
}
