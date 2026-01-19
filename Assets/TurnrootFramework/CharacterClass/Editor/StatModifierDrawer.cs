using UnityEditor;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
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

    [CustomPropertyDrawer(typeof(UnboundedStatModifier))]
    public class UnboundedStatModifierDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var statTypeProp = property.FindPropertyRelative("unboundedStatType");
            var valueProp = property.FindPropertyRelative("value");

            // Draw stat type name as label and value field
            var statName = statTypeProp.enumDisplayNames[statTypeProp.enumValueIndex];
            EditorGUI.PropertyField(position, valueProp, new GUIContent(statName));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;
    }
}
