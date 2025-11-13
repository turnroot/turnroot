using UnityEditor;
using UnityEngine;

namespace Turnroot.Characters.Configuration.Editor
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 25f;
        private const float Spacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _ = EditorGUI.BeginProperty(position, label, property);

            var keysProperty = property.FindPropertyRelative("_keys");
            var valuesProperty = property.FindPropertyRelative("_values");

            if (keysProperty == null || valuesProperty == null)
            {
                EditorGUI.LabelField(position, label.text, "Invalid SerializableDictionary");
                EditorGUI.EndProperty();
                return;
            }

            // Draw foldout
            var foldoutRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float yPos = position.y + EditorGUIUtility.singleLineHeight + Spacing;

                // Draw each key-value pair
                for (int i = 0; i < keysProperty.arraySize; i++)
                {
                    var keyProp = keysProperty.GetArrayElementAtIndex(i);
                    var valueProp = valuesProperty.GetArrayElementAtIndex(i);

                    float lineHeight = Mathf.Max(
                        EditorGUI.GetPropertyHeight(keyProp),
                        EditorGUI.GetPropertyHeight(valueProp)
                    );

                    // Key field (40% width)
                    var keyRect = new Rect(
                        position.x + EditorGUIUtility.labelWidth,
                        yPos,
                        (position.width - EditorGUIUtility.labelWidth - ButtonWidth - Spacing * 2)
                            * 0.4f,
                        lineHeight
                    );

                    // Draw key field - handle Object references explicitly
                    if (keyProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        keyProp.objectReferenceValue = EditorGUI.ObjectField(
                            keyRect,
                            keyProp.objectReferenceValue,
                            typeof(UnityEngine.Object),
                            false
                        );
                    }
                    else
                    {
                        _ = EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
                    }

                    // Value field (60% width)
                    var valueRect = new Rect(
                        keyRect.xMax + Spacing,
                        yPos,
                        (position.width - EditorGUIUtility.labelWidth - ButtonWidth - Spacing * 2)
                            * 0.6f,
                        lineHeight
                    );

                    // Draw value field - handle different types explicitly
                    if (valueProp.propertyType == SerializedPropertyType.Enum)
                    {
                        valueProp.enumValueIndex = EditorGUI.Popup(
                            valueRect,
                            valueProp.enumValueIndex,
                            valueProp.enumDisplayNames
                        );
                    }
                    else if (valueProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // Get the actual type of TValue for proper object filtering
                        var fieldType = fieldInfo.FieldType;
                        var valueType = fieldType.GenericTypeArguments[1];

                        // For abstract types like Conversation, use ScriptableObject as base type
                        // so Unity can show concrete implementations
                        var pickerType = valueType.IsAbstract
                            ? typeof(ScriptableObject)
                            : valueType;

                        valueProp.objectReferenceValue = EditorGUI.ObjectField(
                            valueRect,
                            valueProp.objectReferenceValue,
                            pickerType,
                            false
                        );
                    }
                    else if (valueProp.propertyType == SerializedPropertyType.Integer)
                    {
                        valueProp.intValue = EditorGUI.IntField(valueRect, valueProp.intValue);
                    }
                    else if (valueProp.propertyType == SerializedPropertyType.String)
                    {
                        valueProp.stringValue = EditorGUI.TextField(
                            valueRect,
                            valueProp.stringValue
                        );
                    }
                    else
                    {
                        _ = EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                    }

                    // Remove button
                    var buttonRect = new Rect(
                        valueRect.xMax + Spacing,
                        yPos,
                        ButtonWidth,
                        EditorGUIUtility.singleLineHeight
                    );
                    if (GUI.Button(buttonRect, "-"))
                    {
                        keysProperty.DeleteArrayElementAtIndex(i);
                        valuesProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    yPos += lineHeight + Spacing;
                }

                // Add button
                var addButtonRect = new Rect(
                    position.x + EditorGUIUtility.labelWidth,
                    yPos,
                    position.width - EditorGUIUtility.labelWidth,
                    EditorGUIUtility.singleLineHeight
                );
                if (GUI.Button(addButtonRect, "+ Add Entry"))
                {
                    keysProperty.arraySize++;
                    valuesProperty.arraySize++;
                }

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

            var keysProperty = property.FindPropertyRelative("_keys");
            var valuesProperty = property.FindPropertyRelative("_values");

            if (keysProperty == null || valuesProperty == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + Spacing; // Foldout

            // Height for each entry
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                var keyProp = keysProperty.GetArrayElementAtIndex(i);
                var valueProp = valuesProperty.GetArrayElementAtIndex(i);

                float lineHeight = Mathf.Max(
                    EditorGUI.GetPropertyHeight(keyProp),
                    EditorGUI.GetPropertyHeight(valueProp)
                );

                height += lineHeight + Spacing;
            }

            // Add button
            height += EditorGUIUtility.singleLineHeight + Spacing;

            return height;
        }
    }
}
