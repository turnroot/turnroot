using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TurnrootFramework.CommonAncestors.LeveledLetteredField), true)]
public class LeveledLetteredFieldDrawer : PropertyDrawer
{
    private readonly string[] _options = { "S", "A", "B", "C", "D", "E" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _ = EditorGUI.BeginProperty(position, label, property);

        // Get the _value field from the base LeveledLetteredField
        SerializedProperty valueProp = property.FindPropertyRelative("_value");

        if (valueProp != null)
        {
            // Find the current index
            int currentIndex = System.Array.IndexOf(_options, valueProp.stringValue);
            if (currentIndex == -1)
                currentIndex = 0; // Default to first option if invalid

            // Draw the dropdown
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, _options);

            // Update the value if changed
            if (newIndex != currentIndex)
            {
                valueProp.stringValue = _options[newIndex];
            }
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Error: _value field not found");
        }

        EditorGUI.EndProperty();
    }
}
