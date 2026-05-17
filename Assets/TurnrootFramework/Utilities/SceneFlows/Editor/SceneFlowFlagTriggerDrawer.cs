#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Turnroot.Utilities.AbstractScripts;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.SceneFlows.Editor
{
    [CustomPropertyDrawer(typeof(SceneFlowFlagTrigger))]
    public class SceneFlowFlagTriggerDrawer : PropertyDrawer
    {
        private static readonly string[] _existingFlagKeys = GetSceneFlowConditionKeys();

        private static string[] GetSceneFlowConditionKeys()
        {
            var type = typeof(SceneFlowConditionKeys);
            var fields = type.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
            );

            List<string> keys = fields
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .OrderBy(v => v)
                .ToList();

            if (keys.Count == 0)
            {
                keys.Add(string.Empty);
            }

            return keys.ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = 4;
            var keySourceProp = property.FindPropertyRelative("keySource");
            if ((SceneFlowFlagKeySource)keySourceProp.enumValueIndex == SceneFlowFlagKeySource.Custom)
            {
                lineCount = 4;
            }
            else
            {
                lineCount = 4;
            }

            return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float verticalSpacing = 2f;
            Rect row = new Rect(position.x, position.y, position.width, lineHeight);

            var timingProp = property.FindPropertyRelative("timing");
            var keySourceProp = property.FindPropertyRelative("keySource");
            var existingKeyProp = property.FindPropertyRelative("existingKey");
            var customKeyProp = property.FindPropertyRelative("customKey");
            var valueProp = property.FindPropertyRelative("value");

            EditorGUI.PropertyField(row, timingProp);

            row.y += lineHeight + verticalSpacing;
            EditorGUI.PropertyField(row, keySourceProp);

            row.y += lineHeight + verticalSpacing;
            var keySource = (SceneFlowFlagKeySource)keySourceProp.enumValueIndex;
            if (keySource == SceneFlowFlagKeySource.Custom)
            {
                EditorGUI.PropertyField(row, customKeyProp, new GUIContent("Custom Key"));
            }
            else
            {
                int selectedIndex = Mathf.Max(
                    0,
                    System.Array.IndexOf(_existingFlagKeys, existingKeyProp.stringValue)
                );
                int newIndex = EditorGUI.Popup(row, "Existing Key", selectedIndex, _existingFlagKeys);
                if (newIndex >= 0 && newIndex < _existingFlagKeys.Length)
                {
                    existingKeyProp.stringValue = _existingFlagKeys[newIndex];
                }
            }

            row.y += lineHeight + verticalSpacing;
            EditorGUI.PropertyField(row, valueProp, new GUIContent("Flag Value"));

            EditorGUI.EndProperty();
        }
    }
}
#endif
