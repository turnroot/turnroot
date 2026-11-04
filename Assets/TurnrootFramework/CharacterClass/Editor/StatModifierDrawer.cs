using System;
using System.Linq;
using Turnroot.Characters.Stats;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Custom property drawer for UnboundedStatModifier that displays the stat type name as the label.
    /// Supports both unbounded and bounded (HP) entries.
    /// </summary>
    [CustomPropertyDrawer(typeof(UnboundedStatModifier))]
    public class UnboundedStatModifierDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var isBoundedProp = property.FindPropertyRelative("isBounded");
            var unboundedProp = property.FindPropertyRelative("unboundedStatType");
            var boundedProp = property.FindPropertyRelative("boundedStatType");
            var valueProp = property.FindPropertyRelative("value");

            float lineH = EditorGUIUtility.singleLineHeight;

            // Row 1: isBounded toggle + stat type enum
            var row1 = new Rect(position.x, position.y, position.width, lineH);
            float toggleWidth = 16f;
            float labelWidth = 70f;

            var toggleRect = new Rect(row1.x, row1.y, toggleWidth, lineH);
            var toggleLabelRect = new Rect(row1.x + toggleWidth + 2f, row1.y, labelWidth, lineH);
            var enumRect = new Rect(
                row1.x + toggleWidth + 2f + labelWidth + 2f,
                row1.y,
                row1.width - toggleWidth - 2f - labelWidth - 2f,
                lineH
            );

            EditorGUI.PropertyField(toggleRect, isBoundedProp, GUIContent.none);
            EditorGUI.LabelField(
                toggleLabelRect,
                isBoundedProp.boolValue ? "Bounded" : "Unbounded"
            );

            if (isBoundedProp.boolValue)
            {
                EditorGUI.PropertyField(enumRect, boundedProp, GUIContent.none);
            }
            else
            {
                var allowedTypes = Enum
                    .GetValues(typeof(UnboundedStatType))
                    .Cast<UnboundedStatType>()
                    .Where(type => type != UnboundedStatType.Movement)
                    .ToArray();
                var options = allowedTypes.Select(type => type.ToString()).ToArray();
                int selectedIndex = Array.IndexOf(
                    allowedTypes,
                    (UnboundedStatType)unboundedProp.intValue
                );
                int nextIndex = EditorGUI.Popup(enumRect, selectedIndex, options);
                if (nextIndex >= 0 && nextIndex < allowedTypes.Length && nextIndex != selectedIndex)
                {
                    unboundedProp.intValue = (int)allowedTypes[nextIndex];
                }
            }

            // Row 2: value
            var row2 = new Rect(position.x, position.y + lineH + Spacing, position.width, lineH);
            EditorGUI.PropertyField(row2, valueProp, new GUIContent("Value"));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * 2 + Spacing;
    }
}
