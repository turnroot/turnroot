using UnityEditor;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Custom property drawer for StatModifier that displays the stat type name as the label.
    /// </summary>
    [CustomPropertyDrawer(typeof(StatModifier))]
    public class StatModifierDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var statTypeProp = property.FindPropertyRelative("boundedStatType");
            var valueProp = property.FindPropertyRelative("value");

            // Draw stat type name as label and value field
            var statName = statTypeProp.enumDisplayNames[statTypeProp.enumValueIndex];
            EditorGUI.PropertyField(position, valueProp, new GUIContent(statName));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;
    }

    /// <summary>
    /// Custom property drawer for UnboundedStatModifier that displays the stat type name as the label.
    /// Supports both unbounded and bounded (HP) entries.
    /// </summary>
    [CustomPropertyDrawer(typeof(UnboundedStatModifier))]
    public class UnboundedStatModifierDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var isBoundedProp = property.FindPropertyRelative("isBounded");
            var unboundedProp = property.FindPropertyRelative("unboundedStatType");
            var boundedProp = property.FindPropertyRelative("boundedStatType");
            var valueProp = property.FindPropertyRelative("value");

            string statName;
            if (isBoundedProp != null && isBoundedProp.boolValue)
            {
                statName = boundedProp.enumDisplayNames[boundedProp.enumValueIndex];
            }
            else
            {
                statName = unboundedProp.enumDisplayNames[unboundedProp.enumValueIndex];
            }

            EditorGUI.PropertyField(position, valueProp, new GUIContent(statName));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;
    }
}
