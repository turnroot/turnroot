using UnityEditor;
using UnityEngine;

namespace Assets.Prototypes.Characters.Configuration.Editor
{
    [CustomEditor(typeof(CharacterData))]
    public class CharacterData : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all the default fields (BoxGroups will handle layout)
            _ = DrawDefaultInspector();

            // Add stats management section at the end
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Stats Management", EditorStyles.boldLabel);

            DrawStatsSection();

            _ = serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatsSection()
        {
            var boundedStatsProperty = serializedObject.FindProperty("_boundedStats");
            var unboundedStatsProperty = serializedObject.FindProperty("_unboundedStats");

            // Bounded Stats
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Bounded Stats", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (boundedStatsProperty != null && boundedStatsProperty.isArray)
            {
                for (int i = 0; i < boundedStatsProperty.arraySize; i++)
                {
                    _ = EditorGUILayout.BeginHorizontal();
                    _ = EditorGUILayout.PropertyField(
                        boundedStatsProperty.GetArrayElementAtIndex(i),
                        GUIContent.none
                    );

                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        boundedStatsProperty.DeleteArrayElementAtIndex(i);
                        _ = serializedObject.ApplyModifiedProperties();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }
            }

            if (GUILayout.Button("+ Bounded Stat", GUILayout.Height(25)))
            {
                boundedStatsProperty.arraySize++;
                _ = serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(15);

            // Unbounded Stats
            EditorGUILayout.LabelField("Unbounded Stats", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (unboundedStatsProperty != null && unboundedStatsProperty.isArray)
            {
                for (int i = 0; i < unboundedStatsProperty.arraySize; i++)
                {
                    _ = EditorGUILayout.BeginHorizontal();
                    _ = EditorGUILayout.PropertyField(
                        unboundedStatsProperty.GetArrayElementAtIndex(i),
                        GUIContent.none
                    );

                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        unboundedStatsProperty.DeleteArrayElementAtIndex(i);
                        _ = serializedObject.ApplyModifiedProperties();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }
            }

            if (GUILayout.Button("+ Unbounded Stat", GUILayout.Height(25)))
            {
                unboundedStatsProperty.arraySize++;
                _ = serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel--;
        }
    }
}
