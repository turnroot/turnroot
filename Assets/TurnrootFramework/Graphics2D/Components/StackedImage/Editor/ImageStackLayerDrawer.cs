using System;
using Turnroot.Characters;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ImageStackLayer))]
public class ImageStackLayerDrawer : PropertyDrawer
{
    private const float PreviewSize = 48f;
    private const float Spacing = 4f;
    private const float FieldHeight = 18f;

    // Use PortraitLayerSpriteCache for portrait-specific sprite lists per tag.

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _ = EditorGUI.BeginProperty(position, label, property);

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

            // Draw fields on the left using dynamic heights per property to avoid overlap
            Rect fieldRect = new Rect(
                position.x,
                yPos,
                fieldWidth,
                EditorGUI.GetPropertyHeight(spriteProp, true)
            );

            // Sprite
            var tagProp = property.FindPropertyRelative("Tag");
            string tag = tagProp != null ? tagProp.stringValue : string.Empty;

            if (!string.IsNullOrEmpty(tag))
            {
                // Use portrait cache to obtain sprites/names for this tag
                var sprites = PortraitLayerSpriteCache.GetSprites(tag);
                var names = PortraitLayerSpriteCache.GetNames(tag);

                // Popup rect
                Rect popupRect = new Rect(
                    fieldRect.x,
                    fieldRect.y,
                    fieldWidth - 28,
                    fieldRect.height
                );

                // Populate popup
                if (names == null || names.Length == 0)
                {
                    EditorGUI.LabelField(popupRect, "(no sprites)");
                }
                else
                {
                    int currentIndex = -1;
                    for (int i = 0; i < sprites.Length; i++)
                        if (sprites[i] == spriteProp.objectReferenceValue)
                        {
                            currentIndex = i;
                            break;
                        }

                    // Draw a button that opens a grid popup of sprite previews
                    string buttonLabel = currentIndex >= 0 ? names[currentIndex] : "Select...";
                    if (GUI.Button(popupRect, buttonLabel))
                    {
                        var popup = new FilteredSpritePicker(
                            tag,
                            s =>
                            {
                                spriteProp.objectReferenceValue = s;
                                property.serializedObject.ApplyModifiedProperties();
#if UNITY_EDITOR
                                if (
                                    !UnityEditor.EditorApplication.isCompiling
                                    && !UnityEditor.EditorApplication.isUpdating
                                )
                                {
                                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                                }
#endif
                            },
                            currentIndex >= 0 ? sprites[currentIndex] : null
                        );
                        PopupWindow.Show(popupRect, popup);
                    }
                }

                // Unmasked portrait layers show a color picker for per-layer tint.
                var tintProp = property.FindPropertyRelative("Tint");
                if (tintProp != null)
                {
                    Rect colorRect = new Rect(
                        fieldRect.x,
                        fieldRect.y + fieldRect.height + Spacing,
                        200,
                        FieldHeight
                    );
                    tintProp.colorValue = EditorGUI.ColorField(
                        colorRect,
                        "Layer Color",
                        tintProp.colorValue
                    );
                    yPos += FieldHeight + Spacing;
                }
            }
            else
            {
                _ = EditorGUI.PropertyField(fieldRect, spriteProp, new GUIContent("Sprite"));
            }
            float h = EditorGUI.GetPropertyHeight(spriteProp, true);
            yPos += h + Spacing;

            // Mask - skip for unmasked portrait layers (e.g., Tag == "Hair")
            if (string.IsNullOrEmpty(tag))
            {
                fieldRect.y = yPos;
                fieldRect.height = EditorGUI.GetPropertyHeight(maskProp, true);
                _ = EditorGUI.PropertyField(fieldRect, maskProp, new GUIContent("Mask"));
                h = EditorGUI.GetPropertyHeight(maskProp, true);
                yPos += h + Spacing;
            }
            else
            {
                // No mask for hair/unmasked layers; ensure spacing accounts for the absent field
                // (we already incremented yPos for sprite and the tint field if present)
            }

            // Offset (Vector2)
            fieldRect.y = yPos;
            fieldRect.height = EditorGUI.GetPropertyHeight(offsetProp, true);
            _ = EditorGUI.PropertyField(fieldRect, offsetProp, new GUIContent("Offset"));
            h = EditorGUI.GetPropertyHeight(offsetProp, true);
            yPos += h + Spacing;

            // Scale
            fieldRect.y = yPos;
            fieldRect.height = EditorGUI.GetPropertyHeight(scaleProp, true);
            _ = EditorGUI.PropertyField(fieldRect, scaleProp, new GUIContent("Scale"));
            h = EditorGUI.GetPropertyHeight(scaleProp, true);
            yPos += h + Spacing;

            // Tag selection - use a dropdown of known portrait layer tags and enforce
            // uniqueness and immutability of mandatory tags.
            var tagFieldProp = property.FindPropertyRelative("Tag");
            fieldRect.y = yPos;
            string oldTag = tagFieldProp != null ? tagFieldProp.stringValue : string.Empty;

            // Build popup options with an initial empty option
            var all = PortraitLayerTags.All;
            string[] popupOptions = new string[all.Length + 1];
            popupOptions[0] = "<none>";
            for (int i = 0; i < all.Length; i++)
                popupOptions[i + 1] = all[i];

            int tagCurrentIndex = 0;
            if (!string.IsNullOrEmpty(oldTag))
            {
                for (int i = 0; i < all.Length; i++)
                    if (string.Equals(all[i], oldTag, StringComparison.OrdinalIgnoreCase))
                    {
                        tagCurrentIndex = i + 1;
                        break;
                    }
            }

            int tagSel = EditorGUI.Popup(
                new Rect(fieldRect.x, fieldRect.y, fieldRect.width, FieldHeight),
                "Tag",
                tagCurrentIndex,
                popupOptions
            );
            string newTag = tagSel == 0 ? string.Empty : all[tagSel - 1];

            // If this layer previously had a mandatory tag, don't allow changing it
            if (
                PortraitLayerTags.IsMandatory(oldTag)
                && !string.Equals(oldTag, newTag, StringComparison.OrdinalIgnoreCase)
            )
            {
                // revert and warn
                EditorUtility.DisplayDialog(
                    "Tag Locked",
                    $"The '{oldTag}' tag is mandatory and cannot be changed.",
                    "OK"
                );
                // ensure property is left unchanged
            }
            else if (
                tagFieldProp != null
                && !string.Equals(oldTag, newTag, StringComparison.OrdinalIgnoreCase)
            )
            {
                // Attempt to set new tag but ensure uniqueness within the parent ImageStack
                if (!string.IsNullOrEmpty(newTag))
                {
                    var so = property.serializedObject;
                    var layersProp = so.FindProperty("_layers");
                    bool duplicate = false;
                    if (layersProp != null)
                    {
                        for (int i = 0; i < layersProp.arraySize; i++)
                        {
                            var el = layersProp.GetArrayElementAtIndex(i);
                            if (el == null)
                                continue;
                            var t = el.FindPropertyRelative("Tag");
                            if (
                                t != null
                                && !string.IsNullOrEmpty(t.stringValue)
                                && string.Equals(
                                    t.stringValue,
                                    newTag,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                duplicate = true;
                                break;
                            }
                        }
                    }

                    if (duplicate)
                    {
                        EditorUtility.DisplayDialog(
                            "Duplicate Tag",
                            $"Only one layer may use the tag '{newTag}'. Reverting the change.",
                            "OK"
                        );
                    }
                    else
                    {
                        tagFieldProp.stringValue = newTag;

                        // Commit the property change then convert the runtime layer type
                        property.serializedObject.ApplyModifiedProperties();

                        // Determine array index from property path like '_layers.Array.data[3]'
                        var path = property.propertyPath;
                        int idx = -1;
                        var marker = "Array.data[";
                        int m = path.IndexOf(marker, StringComparison.Ordinal);
                        if (m >= 0)
                        {
                            int start = m + marker.Length;
                            int end = path.IndexOf(']', start);
                            if (end > start)
                            {
                                var num = path.Substring(start, end - start);
                                int.TryParse(num, out idx);
                            }
                        }

                        var stack =
                            property.serializedObject.targetObject
                            as Turnroot.Graphics.Portrait.ImageStack;
                        if (stack != null && idx >= 0 && idx < stack.Layers.Count)
                        {
                            var runtimeLayer = stack.Layers[idx];
                            if (!string.IsNullOrEmpty(newTag))
                            {
                                // Convert to unmasked if needed
                                if (!(runtimeLayer is UnmaskedImageStackLayer))
                                {
                                    var converted = new UnmaskedImageStackLayer();
                                    converted.Sprite = runtimeLayer.Sprite;
                                    converted.Mask = null;
                                    converted.Offset = runtimeLayer.Offset;
                                    converted.Scale = runtimeLayer.Scale;
                                    converted.Rotation = runtimeLayer.Rotation;
                                    converted.Order = runtimeLayer.Order;
                                    converted.Tag = newTag;
                                    converted.Tint = runtimeLayer.Tint;

                                    stack.Layers[idx] = converted;
#if UNITY_EDITOR
                                    if (
                                        !UnityEditor.EditorApplication.isCompiling
                                        && !UnityEditor.EditorApplication.isUpdating
                                    )
                                    {
                                        EditorUtility.SetDirty(stack);
                                    }
#endif
                                }
                            }
                            else
                            {
                                // Convert back to normal layer if currently unmasked
                                if (runtimeLayer is UnmaskedImageStackLayer)
                                {
                                    var converted = new ImageStackLayer();
                                    converted.Sprite = runtimeLayer.Sprite;
                                    converted.Mask = runtimeLayer.Mask;
                                    converted.Offset = runtimeLayer.Offset;
                                    converted.Scale = runtimeLayer.Scale;
                                    converted.Rotation = runtimeLayer.Rotation;
                                    converted.Order = runtimeLayer.Order;
                                    converted.Tag = newTag;
                                    converted.Tint = runtimeLayer.Tint;

                                    stack.Layers[idx] = converted;
#if UNITY_EDITOR
                                    if (
                                        !UnityEditor.EditorApplication.isCompiling
                                        && !UnityEditor.EditorApplication.isUpdating
                                    )
                                    {
                                        EditorUtility.SetDirty(stack);
                                    }
#endif
                                }
                            }

                            // Recompute order so Face is always order 0, others sequential
                            int orderCounter = 1;
                            for (int i = 0; i < stack.Layers.Count; i++)
                            {
                                var l = stack.Layers[i];
                                if (l == null)
                                    continue;
                                if (
                                    !string.IsNullOrEmpty(l.Tag)
                                    && string.Equals(
                                        l.Tag,
                                        "Face",
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                {
                                    l.Order = 0;
                                }
                                else
                                {
                                    l.Order = orderCounter;
                                    orderCounter++;
                                }
                            }
#if UNITY_EDITOR
                            if (
                                !UnityEditor.EditorApplication.isCompiling
                                && !UnityEditor.EditorApplication.isUpdating
                            )
                            {
                                EditorUtility.SetDirty(stack);
                            }
#endif
                            property.serializedObject.Update();
                        }
                    }
                }
                else
                {
                    tagFieldProp.stringValue = string.Empty;
                }
                h = EditorGUI.GetPropertyHeight(tagFieldProp, true);
                yPos += h + Spacing;
            }

            // Order
            // Order is handled by the container (reorderable list) and not editable here
            // but we still show it as a badge on the preview above.

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
        // If the property isn't expanded, only the foldout is shown.
        if (!property.isExpanded)
            return FieldHeight;

        float height = 0f;
        // Foldout line
        height += FieldHeight + Spacing;

        // Sum the exact heights of each sub-property to avoid under/over-estimates
        var spriteProp = property.FindPropertyRelative("Sprite");
        var maskProp = property.FindPropertyRelative("Mask");
        var offsetProp = property.FindPropertyRelative("Offset");
        var scaleProp = property.FindPropertyRelative("Scale");
        var rotationProp = property.FindPropertyRelative("Rotation");
        var tagProp = property.FindPropertyRelative("Tag");
        var tintProp = property.FindPropertyRelative("Tint");

        if (spriteProp != null)
            height += EditorGUI.GetPropertyHeight(spriteProp, true) + Spacing;

        // Mask only shown for non-hair layers; detect tag to decide
        string tag = string.Empty;
        if (tagProp != null)
            tag = tagProp.stringValue;

        if (string.IsNullOrEmpty(tag))
        {
            if (maskProp != null)
                height += EditorGUI.GetPropertyHeight(maskProp, true) + Spacing;
        }
        else
        {
            // For hair/unmasked layers, we also show the tint swatch
            if (tintProp != null)
                height += EditorGUI.GetPropertyHeight(tintProp, true) + Spacing;
        }

        if (offsetProp != null)
            height += EditorGUI.GetPropertyHeight(offsetProp, true) + Spacing;
        if (scaleProp != null)
            height += EditorGUI.GetPropertyHeight(scaleProp, true) + Spacing;

        // Tag field
        if (tagProp != null)
            height += EditorGUI.GetPropertyHeight(tagProp, true) + Spacing;

        // Ensure there's room for the preview on the right if a sprite exists
        if (spriteProp != null && spriteProp.objectReferenceValue != null)
        {
            float fieldsHeight = height;
            if (PreviewSize + FieldHeight + Spacing > fieldsHeight)
                height = PreviewSize + FieldHeight + Spacing;
        }

        // padding
        height += 2f;

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
