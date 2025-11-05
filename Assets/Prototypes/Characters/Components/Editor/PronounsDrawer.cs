using Assets.Prototypes.Characters.Subclasses;
using UnityEditor;
using UnityEngine;

namespace Assets.Prototypes.Characters.Subclasses.Editor
{
    [CustomPropertyDrawer(typeof(Pronouns))]
    public class PronounsDrawer : PropertyDrawer
    {
        private static readonly string[] PronounOptions = { "they", "she", "he" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _ = EditorGUI.BeginProperty(position, label, property);

            // Get the current pronoun type by checking which array is selected
            int selectedIndex = GetCurrentPronounIndex(property);

            // Draw dropdown
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, PronounOptions);

            if (EditorGUI.EndChangeCheck())
            {
                // Update the pronoun type
                SetPronounType(property, PronounOptions[newIndex]);
            }

            EditorGUI.EndProperty();
        }

        private int GetCurrentPronounIndex(SerializedProperty property)
        {
            var selectedPronounsProperty = property.FindPropertyRelative("_selectedPronouns");

            if (selectedPronounsProperty == null || selectedPronounsProperty.arraySize == 0)
                return 0; // Default to "they"

            // Check the first element to determine which pronoun set
            string firstPronoun = selectedPronounsProperty.GetArrayElementAtIndex(0).stringValue;

            switch (firstPronoun?.ToLower())
            {
                case "she":
                    return 1;
                case "he":
                    return 2;
                case "they":
                default:
                    return 0;
            }
        }

        private void SetPronounType(SerializedProperty property, string pronounType)
        {
            var selectedPronounsProperty = property.FindPropertyRelative("_selectedPronouns");

            if (selectedPronounsProperty == null)
                return;

            string[] pronouns;
            switch (pronounType.ToLower())
            {
                case "she":
                    pronouns = new[] { "she", "her", "hers", "her" };
                    break;
                case "he":
                    pronouns = new[] { "he", "his", "his", "him" };
                    break;
                case "they":
                default:
                    pronouns = new[] { "they", "their", "theirs", "them" };
                    break;
            }

            selectedPronounsProperty.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                selectedPronounsProperty.GetArrayElementAtIndex(i).stringValue = pronouns[i];
            }

            _ = property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
