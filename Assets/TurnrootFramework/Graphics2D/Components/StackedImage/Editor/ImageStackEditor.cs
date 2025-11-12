using Turnroot.Graphics.Portrait;
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

        private static bool IsGrayscalePNG(Texture2D texture)
        {
            if (texture == null)
                return false;
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path) || !path.ToLower().EndsWith(".png"))
                return false;
            try
            {
                byte[] data = System.IO.File.ReadAllBytes(path);
                if (
                    data.Length < 24
                    || data[0] != 0x89
                    || data[1] != 0x50
                    || data[2] != 0x4E
                    || data[3] != 0x47
                    || data[4] != 0x0D
                    || data[5] != 0x0A
                    || data[6] != 0x1A
                    || data[7] != 0x0A
                )
                    return false;
                int pos = 8;
                while (pos + 12 < data.Length)
                {
                    int length =
                        (data[pos] << 24)
                        | (data[pos + 1] << 16)
                        | (data[pos + 2] << 8)
                        | data[pos + 3];
                    string type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
                    if (type == "IHDR" && length >= 13)
                    {
                        int colorType = data[pos + 17];
                        return colorType == 0 || colorType == 4; // grayscale or grayscale+alpha
                    }
                    pos += length + 12;
                }
            }
            catch { }
            return false;
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
                        EditorGUILayout.PropertyField(tagProp, new GUIContent("Tag"));

                    if (layerObj is UnmaskedImageStackLayer)
                    {
                        var tintProp = layerProp.FindPropertyRelative("Tint");
                        bool isGrayscale = IsGrayscalePNG(layerObj.Sprite?.texture);
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
            // We want Order 0 to be the bottom of the stack, while index 0 is the top in the list.
            for (int i = 0; i < _layersProp.arraySize; i++)
            {
                var el = _layersProp.GetArrayElementAtIndex(i);
                var orderProp = el.FindPropertyRelative("Order");
                if (orderProp != null)
                    orderProp.intValue = (_layersProp.arraySize - 1) - i;
            }
            _ = serializedObject.ApplyModifiedProperties();
        }

        // Owner resolution removed: ImageStack is edited in the Portrait context. No auto-owner assignment.
    }
}
