using UnityEditor;
using UnityEngine;

/// <summary>
/// Property drawer for CharacterInventoryInstance to provide a nice inspector UI.
/// This allows editing inventory instances in the Inspector (on CharacterData, etc.)
/// without needing a separate ScriptableObject.
/// </summary>
[CustomPropertyDrawer(typeof(CharacterInventoryInstance))]
public class CharacterInventoryInstanceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            label,
            true
        );

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            float y = position.y + EditorGUIUtility.singleLineHeight + 2;

            // Get properties
            SerializedProperty capacity = property.FindPropertyRelative("_capacity");
            SerializedProperty inventoryItems = property.FindPropertyRelative("_inventoryItems");

            // Draw capacity
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                capacity,
                new GUIContent("Capacity")
            );
            y += EditorGUIUtility.singleLineHeight + 4;

            // Info label
            EditorGUI.LabelField(
                new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
                $"Items: {inventoryItems.arraySize}/{capacity.intValue}"
            );
            y += EditorGUIUtility.singleLineHeight + 4;

            // Draw inventory items list
            float itemsHeight = EditorGUI.GetPropertyHeight(inventoryItems, true);
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, itemsHeight),
                inventoryItems,
                new GUIContent("Items"),
                true
            );

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        float height = EditorGUIUtility.singleLineHeight + 2; // Foldout

        SerializedProperty inventoryItems = property.FindPropertyRelative("_inventoryItems");

        height += EditorGUIUtility.singleLineHeight + 4; // Capacity
        height += EditorGUIUtility.singleLineHeight + 4; // Info label
        height += EditorGUI.GetPropertyHeight(inventoryItems, true) + 2; // Items list

        return height;
    }
}
