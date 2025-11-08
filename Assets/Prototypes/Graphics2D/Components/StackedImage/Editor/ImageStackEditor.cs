using Assets.Prototypes.Graphics.Portrait;
using UnityEditor;
using UnityEngine;

namespace Assets.Prototypes.Graphics.Portrait.Editor
{
    [CustomEditor(typeof(ImageStack))]
    public class ImageStackEditor : UnityEditor.Editor
    {
        private SerializedProperty _ownerCharacterProp;
        private SerializedProperty _layersProp;

        private void OnEnable()
        {
            _ownerCharacterProp = serializedObject.FindProperty("_ownerCharacter");
            _layersProp = serializedObject.FindProperty("_layers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Auto-assign owner character if null
            // This automatically sets the owner to the Character that contains a Portrait with this ImageStack
            if (_ownerCharacterProp != null && _ownerCharacterProp.objectReferenceValue == null)
            {
                var character = FindOwnerCharacter();
                if (character != null)
                {
                    _ownerCharacterProp.objectReferenceValue = character;
                    _ = serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Image Stack", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Draw owner field
            _ = EditorGUILayout.PropertyField(
                _ownerCharacterProp,
                new GUIContent("Owner Character")
            );

            EditorGUILayout.Space(10);

            // Draw layers list with custom header
            EditorGUILayout.LabelField($"Layers ({_layersProp.arraySize})", EditorStyles.boldLabel);

            if (_layersProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No layers in this stack. Click '+ Add Layer' to add one.",
                    MessageType.Info
                );
            }
            else
            {
                // Draw each layer
                for (int i = 0; i < _layersProp.arraySize; i++)
                {
                    _ = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    _ = EditorGUILayout.BeginHorizontal();

                    var layerProp = _layersProp.GetArrayElementAtIndex(i);
                    _ = EditorGUILayout.PropertyField(
                        layerProp,
                        new GUIContent($"Layer {i}"),
                        true
                    );

                    // Move up button
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        _ = _layersProp.MoveArrayElement(i, i - 1);
                        _ = serializedObject.ApplyModifiedProperties();
                        return;
                    }
                    GUI.enabled = true;

                    // Move down button
                    GUI.enabled = i < _layersProp.arraySize - 1;
                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        _ = _layersProp.MoveArrayElement(i, i + 1);
                        _ = serializedObject.ApplyModifiedProperties();
                        return;
                    }
                    GUI.enabled = true;

                    // Delete button
                    if (GUILayout.Button("✕", GUILayout.Width(25)))
                    {
                        _layersProp.DeleteArrayElementAtIndex(i);
                        _ = serializedObject.ApplyModifiedProperties();
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3);
                }
            }

            EditorGUILayout.Space(5);

            // Add layer button
            if (GUILayout.Button("+ Add Layer", GUILayout.Height(25)))
            {
                _layersProp.arraySize++;
                var newLayer = _layersProp.GetArrayElementAtIndex(_layersProp.arraySize - 1);

                // Initialize new layer with defaults
                newLayer.FindPropertyRelative("Sprite").objectReferenceValue = null;
                newLayer.FindPropertyRelative("Offset").vector2Value = Vector2.zero;
                newLayer.FindPropertyRelative("Scale").floatValue = 1f;
                newLayer.FindPropertyRelative("Rotation").floatValue = 0f;
                newLayer.FindPropertyRelative("Order").intValue = _layersProp.arraySize - 1;

                _ = serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(10);

            // Info about rendering
            EditorGUILayout.HelpBox(
                "To render this ImageStack, use the Portrait Editor (for characters) or Skill Badge Editor (for skills).",
                MessageType.Info
            );

            _ = serializedObject.ApplyModifiedProperties();
        }

        private Object FindOwnerCharacter()
        {
            // Search through all Character instances to find one with a Portrait that references this ImageStack
            string assetPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(assetPath))
                return null;

            // Get all Character assets in the project
            string[] guids = AssetDatabase.FindAssets("t:Character");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);

                if (obj != null)
                {
                    SerializedObject so = new SerializedObject(obj);
                    SerializedProperty portraitsProp = so.FindProperty("_portraits");

                    if (portraitsProp != null && portraitsProp.isArray)
                    {
                        // Check each portrait in the array
                        for (int i = 0; i < portraitsProp.arraySize; i++)
                        {
                            SerializedProperty portraitProp = portraitsProp.GetArrayElementAtIndex(
                                i
                            );
                            SerializedProperty imageStackProp = portraitProp.FindPropertyRelative(
                                "_imageStack"
                            );

                            if (
                                imageStackProp != null
                                && imageStackProp.objectReferenceValue != null
                                && imageStackProp.objectReferenceValue == target
                            )
                            {
                                return obj;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
