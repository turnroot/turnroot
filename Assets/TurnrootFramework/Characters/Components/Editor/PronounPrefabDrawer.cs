#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Turnroot.Characters.Subclasses;
using Turnroot.Characters.CharacterClass;

namespace Turnroot.Characters.CharacterClass.Editor
{
    [CustomPropertyDrawer(typeof(PronounPrefab))]
    public class PronounPrefabDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _ = EditorGUI.BeginProperty(position, label, property);

            var pronounProp = property.FindPropertyRelative("pronounKey");
            var prefabProp = property.FindPropertyRelative("prefab");

            // Get available pronoun keys
            var keys =
                Turnroot.Characters.Subclasses.Pronouns.GetAvailablePronounKeys() ?? new string[0];

            // Determine current index
            int currentIndex = 0;
            if (!string.IsNullOrEmpty(pronounProp.stringValue))
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    if (
                        string.Equals(
                            keys[i],
                            pronounProp.stringValue,
                            System.StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            // Calculate rects (compact single-line layout)
            var pronounRect = new Rect(
                position.x,
                position.y,
                120,
                EditorGUIUtility.singleLineHeight
            );
            var prefabRect = new Rect(
                position.x + 124,
                position.y,
                position.width - 124,
                EditorGUIUtility.singleLineHeight
            );

            // Pronoun dropdown (use overload taking selectedIndex + string[]). If no pronoun keys are defined,
            // fall back to a free-text field so the property remains editable in older projects.
            if (keys.Length > 0)
            {
                int newIndex = EditorGUI.Popup(pronounRect, currentIndex, keys);
                if (newIndex != currentIndex)
                {
                    pronounProp.stringValue = keys[newIndex];
                }
            }
            else
            {
                pronounProp.stringValue = EditorGUI.TextField(pronounRect, pronounProp.stringValue);
            }

            // Prefab field (no label)
            EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
