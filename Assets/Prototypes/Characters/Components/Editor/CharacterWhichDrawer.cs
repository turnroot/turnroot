using Assets.Prototypes.Characters.Components;
using UnityEditor;
using UnityEngine;

namespace Assets.Prototypes.Characters.Components.Editor
{
    [CustomPropertyDrawer(typeof(CharacterWhich))]
    public class CharacterWhichDrawer : PropertyDrawer
    {
        private static readonly string[] options = new string[]
        {
            CharacterWhich.AVATAR,
            CharacterWhich.ENEMY,
            CharacterWhich.ALLY,
            CharacterWhich.NPC,
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the _value field
            SerializedProperty valueProperty = property.FindPropertyRelative("_value");

            if (valueProperty != null)
            {
                // Find current index
                string currentValue = valueProperty.stringValue;
                int currentIndex = System.Array.IndexOf(options, currentValue);
                if (currentIndex < 0)
                    currentIndex = 1; // Default to Enemy

                // Draw dropdown
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);

                // Update value if changed
                if (newIndex != currentIndex)
                {
                    valueProperty.stringValue = options[newIndex];
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Error: _value field not found");
            }

            EditorGUI.EndProperty();
        }
    }
}
