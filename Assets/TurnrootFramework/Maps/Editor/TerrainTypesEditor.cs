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
            var costWalkProp = elem.FindPropertyRelative("_costWalk");
            var costFlyProp = elem.FindPropertyRelative("_costFly");
            var costRideProp = elem.FindPropertyRelative("_costRide");
            var costMagicProp = elem.FindPropertyRelative("_costMagic");
            var costArmorProp = elem.FindPropertyRelative("_costArmor");
            var colorProp = elem.FindPropertyRelative("_editorColor");
            var healthWalkProp = elem.FindPropertyRelative("_healthChangePerTurnWalk");
            var healthRidingProp = elem.FindPropertyRelative("_healthChangePerTurnRiding");
            var healthFlyingProp = elem.FindPropertyRelative("_healthChangePerTurnFlying");
            var defenseWalkProp = elem.FindPropertyRelative("_defenseBonusWalk");
            var defenseRidingProp = elem.FindPropertyRelative("_defenseBonusRiding");
            var defenseFlyingProp = elem.FindPropertyRelative("_defenseBonusFlying");
            var avoidWalkProp = elem.FindPropertyRelative("_avoidBonusWalk");
            var avoidRidingProp = elem.FindPropertyRelative("_avoidBonusRiding");
            var avoidFlyingProp = elem.FindPropertyRelative("_avoidBonusFlying");

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

            // Costs and per-movement sliders on their own rows
            if (costArmorProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(costArmorProp, new GUIContent("Cost Armor"));
                EditorGUILayout.EndHorizontal();
            }

            if (healthWalkProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    healthWalkProp,
                    -20,
                    20,
                    new GUIContent("Health +/- Walk")
                );
                EditorGUILayout.EndHorizontal();
            }

            if (healthRidingProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    healthRidingProp,
                    -20,
                    20,
                    new GUIContent("Health +/- Riding")
                );
                EditorGUILayout.EndHorizontal();
            }

            if (healthFlyingProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    healthFlyingProp,
                    -20,
                    20,
                    new GUIContent("Health +/- Flying")
                );
                EditorGUILayout.EndHorizontal();
            }

            if (defenseWalkProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    defenseWalkProp,
                    -40,
                    40,
                    new GUIContent("Defense +/- Walk")
                );
                EditorGUILayout.EndHorizontal();
            }

            if (defenseRidingProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    defenseRidingProp,
                    -40,
                    40,
                    new GUIContent("Defense +/- Riding")
                );
                EditorGUILayout.EndHorizontal();
            }

            if (defenseFlyingProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    defenseFlyingProp,
                    -40,
                    40,
                    new GUIContent("Defense +/- Flying")
                );
                EditorGUILayout.EndHorizontal();
            }

            if (avoidWalkProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(avoidWalkProp, -40, 40, new GUIContent("Avoid +/- Walk"));
                EditorGUILayout.EndHorizontal();
            }

            if (avoidRidingProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    avoidRidingProp,
                    -40,
                    40,
                    new GUIContent("Avoid +/- Riding")
                );
                EditorGUILayout.EndHorizontal();
            }

            if (avoidFlyingProp != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.IntSlider(
                    avoidFlyingProp,
                    -40,
                    40,
                    new GUIContent("Avoid +/- Flying")
                );
                EditorGUILayout.EndHorizontal();
            }

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
            var nHealthWalk = newElem.FindPropertyRelative("_healthChangePerTurnWalk");
            var nHealthRiding = newElem.FindPropertyRelative("_healthChangePerTurnRiding");
            var nHealthFlying = newElem.FindPropertyRelative("_healthChangePerTurnFlying");
            var nDefenseWalk = newElem.FindPropertyRelative("_defenseBonusWalk");
            var nDefenseRiding = newElem.FindPropertyRelative("_defenseBonusRiding");
            var nDefenseFlying = newElem.FindPropertyRelative("_defenseBonusFlying");
            var nAvoidWalk = newElem.FindPropertyRelative("_avoidBonusWalk");
            var nAvoidRiding = newElem.FindPropertyRelative("_avoidBonusRiding");
            var nAvoidFlying = newElem.FindPropertyRelative("_avoidBonusFlying");

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
            if (nHealthWalk != null)
                nHealthWalk.intValue = 0;
            if (nHealthRiding != null)
                nHealthRiding.intValue = 0;
            if (nHealthFlying != null)
                nHealthFlying.intValue = 0;
            if (nDefenseWalk != null)
                nDefenseWalk.intValue = 0;
            if (nDefenseRiding != null)
                nDefenseRiding.intValue = 0;
            if (nDefenseFlying != null)
                nDefenseFlying.intValue = 0;
            if (nAvoidWalk != null)
                nAvoidWalk.intValue = 0;
            if (nAvoidRiding != null)
                nAvoidRiding.intValue = 0;
            if (nAvoidFlying != null)
                nAvoidFlying.intValue = 0;
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
