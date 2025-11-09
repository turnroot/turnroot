#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for ObjectSubtype that shows a dropdown with only enabled types.
/// </summary>
[CustomPropertyDrawer(typeof(ObjectSubtype))]
public class ObjectSubtypeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get the _value field
        var valueProperty = property.FindPropertyRelative("_value");
        if (valueProperty == null)
        {
            EditorGUI.LabelField(position, label.text, "Error: _value property not found");
            EditorGUI.EndProperty();
            return;
        }

        // Get valid values based on settings
        string[] validValues = ObjectSubtype.GetValidValues();
        string currentValue = valueProperty.stringValue;

        // Find current index
        int currentIndex = System.Array.IndexOf(validValues, currentValue);
        if (currentIndex < 0)
        {
            // Current value not in valid list, default to first option
            currentIndex = 0;
            if (validValues.Length > 0)
                valueProperty.stringValue = validValues[0];
        }

        // Draw dropdown
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, validValues);
        if (newIndex != currentIndex && newIndex >= 0 && newIndex < validValues.Length)
        {
            valueProperty.stringValue = validValues[newIndex];
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif
