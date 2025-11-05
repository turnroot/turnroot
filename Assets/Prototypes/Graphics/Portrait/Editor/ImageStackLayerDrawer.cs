using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ImageStackLayer))]
public class ImageStackLayerDrawer : PropertyDrawer
{
    private const float PreviewSize = 48f;
    private const float Spacing = 4f;
    private const float FieldHeight = 18f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var spriteProp = property.FindPropertyRelative("Sprite");
        var maskProp = property.FindPropertyRelative("Mask");
        var offsetProp = property.FindPropertyRelative("Offset");
        var scaleProp = property.FindPropertyRelative("Scale");
        var rotationProp = property.FindPropertyRelative("Rotation");
        var orderProp = property.FindPropertyRelative("Order");

        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, FieldHeight),
            property.isExpanded,
            label,
            true
        );

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float yPos = position.y + FieldHeight + Spacing;

            // Calculate positions for sprite preview on the right
            bool hasSprite = spriteProp.objectReferenceValue != null;
            float fieldWidth = hasSprite
                ? position.width - PreviewSize - Spacing * 2
                : position.width;

            // Draw fields on the left
            Rect fieldRect = new Rect(position.x, yPos, fieldWidth, FieldHeight);

            EditorGUI.PropertyField(fieldRect, spriteProp, new GUIContent("Sprite"));
            yPos += FieldHeight + Spacing;

            fieldRect.y = yPos;
            EditorGUI.PropertyField(fieldRect, maskProp, new GUIContent("Mask"));
            yPos += FieldHeight + Spacing;

            fieldRect.y = yPos;
            EditorGUI.PropertyField(fieldRect, offsetProp, new GUIContent("Offset"));
            yPos += FieldHeight + Spacing;

            fieldRect.y = yPos;
            EditorGUI.PropertyField(fieldRect, scaleProp, new GUIContent("Scale"));
            yPos += FieldHeight + Spacing;

            fieldRect.y = yPos;
            EditorGUI.PropertyField(fieldRect, rotationProp, new GUIContent("Rotation"));
            yPos += FieldHeight + Spacing;

            fieldRect.y = yPos;
            EditorGUI.PropertyField(fieldRect, orderProp, new GUIContent("Order"));

            // Draw sprite preview on the right if available
            if (hasSprite)
            {
                Sprite sprite = spriteProp.objectReferenceValue as Sprite;
                if (sprite != null)
                {
                    Rect previewRect = new Rect(
                        position.x + fieldWidth + Spacing,
                        position.y + FieldHeight + Spacing,
                        PreviewSize,
                        PreviewSize
                    );

                    // Draw background
                    EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 1f));

                    // Draw sprite
                    Texture2D texture = AssetPreview.GetAssetPreview(sprite);
                    if (texture != null)
                    {
                        GUI.DrawTexture(previewRect, texture, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        // Fallback to sprite texture if preview isn't ready
                        if (sprite.texture != null)
                        {
                            GUI.DrawTextureWithTexCoords(
                                previewRect,
                                sprite.texture,
                                GetSpriteTextureCoords(sprite),
                                true
                            );
                        }
                    }

                    // Draw border
                    EditorGUI.DrawRect(
                        new Rect(previewRect.x, previewRect.y, previewRect.width, 1),
                        Color.gray
                    );
                    EditorGUI.DrawRect(
                        new Rect(previewRect.x, previewRect.yMax - 1, previewRect.width, 1),
                        Color.gray
                    );
                    EditorGUI.DrawRect(
                        new Rect(previewRect.x, previewRect.y, 1, previewRect.height),
                        Color.gray
                    );
                    EditorGUI.DrawRect(
                        new Rect(previewRect.xMax - 1, previewRect.y, 1, previewRect.height),
                        Color.gray
                    );

                    // Draw order badge
                    GUIStyle orderStyle = new GUIStyle(GUI.skin.label);
                    orderStyle.alignment = TextAnchor.MiddleCenter;
                    orderStyle.normal.textColor = Color.white;
                    orderStyle.fontSize = 10;
                    orderStyle.fontStyle = FontStyle.Bold;

                    Rect orderRect = new Rect(previewRect.x + 2, previewRect.y + 2, 16, 16);
                    EditorGUI.DrawRect(orderRect, new Color(0f, 0f, 0f, 0.7f));
                    GUI.Label(orderRect, orderProp.intValue.ToString(), orderStyle);
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return FieldHeight;
        }

        // Height includes: foldout + 6 fields with spacing (Sprite, Mask, Offset, Scale, Rotation, Order)
        float height = FieldHeight + Spacing; // Foldout
        height += (FieldHeight + Spacing) * 6; // 6 fields

        var spriteProp = property.FindPropertyRelative("Sprite");
        if (spriteProp != null && spriteProp.objectReferenceValue != null)
        {
            // If we have a sprite, make sure there's enough space for the preview
            float fieldsHeight = (FieldHeight + Spacing) * 6;
            if (PreviewSize > fieldsHeight)
            {
                height += (PreviewSize - fieldsHeight);
            }
        }

        return height;
    }

    private Rect GetSpriteTextureCoords(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return new Rect(0, 0, 1, 1);

        float x = sprite.textureRect.x / sprite.texture.width;
        float y = sprite.textureRect.y / sprite.texture.height;
        float width = sprite.textureRect.width / sprite.texture.width;
        float height = sprite.textureRect.height / sprite.texture.height;

        return new Rect(x, y, width, height);
    }
}
