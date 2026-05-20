using Turnroot.Utilities;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Editor.Utilities
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sceneAssetProp = property.FindPropertyRelative("_sceneAsset");
            SerializedProperty scenePathProp = property.FindPropertyRelative("_scenePath");

            EditorGUI.BeginChangeCheck();

            SceneAsset selected = (SceneAsset)
                EditorGUI.ObjectField(
                    position,
                    label,
                    sceneAssetProp.objectReferenceValue,
                    typeof(SceneAsset),
                    allowSceneObjects: false
                );

            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProp.objectReferenceValue = selected;
                scenePathProp.stringValue =
                    selected != null ? AssetDatabase.GetAssetPath(selected) : string.Empty;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
