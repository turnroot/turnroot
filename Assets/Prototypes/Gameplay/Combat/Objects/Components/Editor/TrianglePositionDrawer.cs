using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TrianglePosition))]
public class TrianglePositionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _ = EditorGUI.BeginProperty(position, label, property);

        // Get the _position field
        SerializedProperty positionProp = property.FindPropertyRelative("_position");

        if (positionProp != null)
        {
            // Draw the enum dropdown
            _ = EditorGUI.PropertyField(position, positionProp, label);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Error: _position field not found");
        }

        EditorGUI.EndProperty();
    }
}
