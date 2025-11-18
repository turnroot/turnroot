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
            var indoorNameProp = elem.FindPropertyRelative("_indoorName");
            var costWalkProp = elem.FindPropertyRelative("_costWalk");
            var costFlyProp = elem.FindPropertyRelative("_costFly");
            var costRideProp = elem.FindPropertyRelative("_costRide");
            var costMagicProp = elem.FindPropertyRelative("_costMagic");
            var costArmorProp = elem.FindPropertyRelative("_costArmor");
            var colorProp = elem.FindPropertyRelative("_editorColor");
            var healthProp = elem.FindPropertyRelative("_healthChangePerTurn");
            var defenseProp = elem.FindPropertyRelative("_defenseBonus");
            var avoidProp = elem.FindPropertyRelative("_avoidBonus");

            string header =
                nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue)
                    ? nameProp.stringValue
                    : $"Terrain {i}";
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            if (nameProp != null)
                EditorGUILayout.PropertyField(nameProp, new GUIContent("Name"));

            if (colorProp != null)
                EditorGUILayout.PropertyField(colorProp, new GUIContent("Editor Color"));

            if (costWalkProp != null)
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(costWalkProp, new GUIContent("Cost Walk"));

            if (costFlyProp != null)
                EditorGUILayout.PropertyField(costFlyProp, new GUIContent("Cost Fly"));
            EditorGUILayout.EndHorizontal();

            if (costRideProp != null)
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(costRideProp, new GUIContent("Cost Ride"));

            if (costMagicProp != null)
                EditorGUILayout.PropertyField(costMagicProp, new GUIContent("Cost Magic"));
            EditorGUILayout.EndHorizontal();

            if (costArmorProp != null)
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(costArmorProp, new GUIContent("Cost Armor"));
            if (healthProp != null)
                EditorGUILayout.PropertyField(healthProp, new GUIContent("Health +/-"));
            EditorGUILayout.EndHorizontal();

            if (defenseProp != null)
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(defenseProp, new GUIContent("Defense +/-"));
            if (avoidProp != null)
                EditorGUILayout.PropertyField(avoidProp, new GUIContent("Avoid +/-"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove"))
            {
                typesProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add New Terrain Type"))
        {
            int newIndex = typesProp.arraySize;
            typesProp.InsertArrayElementAtIndex(newIndex);
            var newElem = typesProp.GetArrayElementAtIndex(newIndex);
            // initialize fields
            var nid = newElem.FindPropertyRelative("_id");
            var nName = newElem.FindPropertyRelative("_name");
            var nCostWalk = newElem.FindPropertyRelative("_costWalk");
            var nCostFly = newElem.FindPropertyRelative("_costFly");
            var nCostRide = newElem.FindPropertyRelative("_costRide");
            var nCostMagic = newElem.FindPropertyRelative("_costMagic");
            var nCostArmor = newElem.FindPropertyRelative("_costArmor");
            var nColor = newElem.FindPropertyRelative("_editorColor");

            if (nid != null)
                nid.stringValue = System.Guid.NewGuid().ToString();
            if (nName != null)
                nName.stringValue = "New Terrain";
            if (nCostWalk != null)
                nCostWalk.floatValue = 1f;
            if (nCostFly != null)
                nCostFly.floatValue = 1f;
            if (nCostRide != null)
                nCostRide.floatValue = 1f;
            if (nCostMagic != null)
                nCostMagic.floatValue = 1f;
            if (nCostArmor != null)
                nCostArmor.floatValue = 1f;
            if (nColor != null)
                nColor.colorValue = Color.white;
        }

        EditorGUILayout.EndHorizontal();

        // Add convenience buttons that call the asset methods directly
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        var asset = target as TerrainTypes;
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
