using Turnroot.Graphics.Portrait;
using Turnroot.Graphics2D.Tags;
using Turnroot.Graphics2D.Utilities;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Graphics.Portrait.Editor
{
    [CustomEditor(typeof(ImageStack))]
    public class ImageStackEditor : UnityEditor.Editor
    {
        private SerializedProperty _layersProp;

        private void OnEnable()
        {
            _layersProp = serializedObject.FindProperty("_layers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Image Stack", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

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
                    var imageStack = serializedObject.targetObject as ImageStack;
                    var layerObj = imageStack.Layers[i];

                    // Draw properties vertically
                    EditorGUILayout.BeginVertical();

                    var spriteProp = layerProp.FindPropertyRelative("Sprite");
                    EditorGUILayout.PropertyField(spriteProp, new GUIContent("Sprite"));

                    var offsetProp = layerProp.FindPropertyRelative("Offset");
                    EditorGUILayout.PropertyField(offsetProp, new GUIContent("Offset"));

                    var scaleProp = layerProp.FindPropertyRelative("Scale");
                    EditorGUILayout.PropertyField(scaleProp, new GUIContent("Scale"));

                    var rotationProp = layerProp.FindPropertyRelative("Rotation");
                    EditorGUILayout.PropertyField(rotationProp, new GUIContent("Rotation"));

                    var tagProp = layerProp.FindPropertyRelative("Tag");
                    if (tagProp != null)
                    {
                        EditorGUILayout.PropertyField(tagProp, new GUIContent("Tag"));
                    }

                    if (layerObj is UnmaskedImageStackLayer)
                    {
                        var tintProp = layerProp.FindPropertyRelative("Tint");
                        var tex = layerObj.Sprite?.texture;
                        bool isGrayscale = TextureValidator.IsGrayscalePNG(tex);
                        GUI.enabled = isGrayscale;
                        EditorGUILayout.PropertyField(tintProp, new GUIContent("Tint"));
                        GUI.enabled = true;
                    }

                    EditorGUILayout.EndVertical();

                    // Move up button
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        _ = _layersProp.MoveArrayElement(i, i - 1);
                        _ = serializedObject.ApplyModifiedProperties();

                        // Renumber orders to match the list
                        RenumberLayerOrders();
                        return;
                    }
                    GUI.enabled = true;

                    // Move down button
                    GUI.enabled = i < _layersProp.arraySize - 1;
                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        _ = _layersProp.MoveArrayElement(i, i + 1);
                        _ = serializedObject.ApplyModifiedProperties();

                        // Renumber orders to match the list
                        RenumberLayerOrders();
                        return;
                    }
                    GUI.enabled = true;

                    // Delete button
                    if (GUILayout.Button("✕", GUILayout.Width(25)))
                    {
                        _layersProp.DeleteArrayElementAtIndex(i);
                        _ = serializedObject.ApplyModifiedProperties();

                        // Renumber orders to match the list
                        RenumberLayerOrders();
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
                // Assign Order so that 0 represents the bottom layer
                newLayer.FindPropertyRelative("Order").intValue = 0;

                _ = serializedObject.ApplyModifiedProperties();

                // Renumber to be safe
                RenumberLayerOrders();
            }

            EditorGUILayout.Space(10);

            // Info about rendering
            EditorGUILayout.HelpBox(
                "To render this ImageStack, use the Portrait Editor (for characters) or Skill Badge Editor (for skills).",
                MessageType.Info
            );

            _ = serializedObject.ApplyModifiedProperties();
        }

        private void RenumberLayerOrders()
        {
            for (int i = 0; i < _layersProp.arraySize; i++)
            {
                var el = _layersProp.GetArrayElementAtIndex(i);
                var orderProp = el.FindPropertyRelative("Order");
                if (orderProp == null)
                {
                    continue;
                }

                var tagProp = el.FindPropertyRelative("Tag");
                if (tagProp != null && !string.IsNullOrEmpty(tagProp.stringValue))
                {
                    if (PortraitLayerTags.TryGetOrder(tagProp.stringValue, out var tagOrder))
                    {
                        orderProp.intValue = tagOrder;
                        continue;
                    }
                }
            }

            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
