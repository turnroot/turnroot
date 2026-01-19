using System;
using System.Linq;
using Turnroot.Graphics2D;
using Turnroot.Graphics2D.Editor;
using Turnroot.Graphics2D.Tags;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Turnroot.Characters.Subclasses.Editor
{
    public class PortraitEditorWindow : StackedImageEditorWindow<CharacterData, Portrait>
    {
        protected override string WindowTitle => "Portrait Editor";
        protected override string OwnerFieldLabel => "Character";

        private ReorderableList _layersReorderList;

        private SerializedObject _layersSerializedObject;
        private string _newPortraitName = "";
        private string _quickPortraitName = "";

        [MenuItem("/Turnroot/Editors/Portrait Editor")]
        public static void ShowWindow() => GetWindow<PortraitEditorWindow>("Portrait Editor");

        public static void OpenPortrait(CharacterData character, string portraitKey)
        {
            var window = GetWindow<PortraitEditorWindow>("Portrait Editor");
            window._currentOwner = character;
            window._selectedImageIndex = 0;
            window.UpdateCurrentImage();
            window.RefreshPreview();
        }

        private void CreateNewPortrait(string proposedName)
        {
            var portraitsDict = _currentOwner.Portraits;
            string newKey = string.IsNullOrWhiteSpace(proposedName)
                ? _currentOwner.FullName + "_Portrait"
                : proposedName;

            string baseKey = newKey;
            int suffix = 1;
            while (portraitsDict.ContainsKey(newKey))
            {
                newKey = baseKey + "_" + suffix++;
            }

            var p = new Portrait();
            p.SetOwner(_currentOwner);
            p.SetKey(newKey);
            portraitsDict[newKey] = p;
            _currentOwner.InvalidatePortraitArrayCache();
            EditorUtility.SetDirty(_currentOwner);
            _selectedImageIndex = portraitsDict.Count - 1;
            UpdateCurrentImage();

            // The ImageStack ScriptableObject has been removed.
            // Portraits now own their layers directly; ensure mandatory layers are present immediately.
            _ = _currentImage.Layers; // ensure mandatory layers are created

            EditorUtility.SetDirty(_currentOwner);
            RefreshPreview();
        }

        protected override void OnGUI()
        {
            EditorGUILayout.LabelField($"Live {WindowTitle}", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUI.BeginChangeCheck();
            _currentOwner =
                EditorGUILayout.ObjectField(
                    OwnerFieldLabel,
                    _currentOwner,
                    typeof(CharacterData),
                    false
                ) as CharacterData;
            if (EditorGUI.EndChangeCheck())
            {
                _selectedImageIndex = 0;
                UpdateCurrentImage();
                _newPortraitName = _currentOwner.FullName + "_Portrait";
                _quickPortraitName =
                    $"{_currentOwner.FullName}_Portrait{(_currentOwner.Portraits?.Count ?? 0) + 1}";
            }

            if (_currentOwner == null)
            {
                EditorGUILayout.HelpBox(
                    $"Select a {OwnerFieldLabel} to edit their portraits.",
                    MessageType.Info
                );
                return;
            }

            var portraitsDict = _currentOwner.Portraits;
            if (portraitsDict == null || portraitsDict.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"This {OwnerFieldLabel} has no portraits.",
                    MessageType.Info
                );
                GUILayout.BeginHorizontal();
                _newPortraitName = GUILayout.TextField(_newPortraitName);
                if (GUILayout.Button("Create"))
                {
                    CreateNewPortrait(_newPortraitName);
                }

                GUILayout.EndHorizontal();
                return;
            }

            var keys = portraitsDict.Keys.ToArray();

            GUILayout.BeginHorizontal();
            int newIndex = EditorGUILayout.Popup("Select Portrait", _selectedImageIndex, keys);
            if (newIndex != _selectedImageIndex)
            {
                _selectedImageIndex = newIndex;
                var arr = _currentOwner.PortraitArray;
                _currentImage =
                    (arr != null && _selectedImageIndex < arr.Length)
                        ? arr[_selectedImageIndex]
                        : null;
                RefreshPreview();
            }

            _quickPortraitName = GUILayout.TextField(_quickPortraitName, GUILayout.Width(120));
            if (GUILayout.Button("New +", EditorStyles.miniButton))
            {
                CreateNewPortrait(_quickPortraitName);
            }

            GUILayout.EndHorizontal();

            if (_currentImage == null)
            {
                EditorGUILayout.HelpBox(
                    $"No Portrait asset for key '{keys[_selectedImageIndex]}'.",
                    MessageType.Info
                );
                if (GUILayout.Button("Create Portrait for this key"))
                {
                    var p = new Portrait();
                    p.SetOwner(_currentOwner);
                    p.SetKey(keys[_selectedImageIndex]);
                    _currentOwner.Portraits[keys[_selectedImageIndex]] = p;
                    _currentOwner.InvalidatePortraitArrayCache();
                    EditorUtility.SetDirty(_currentOwner);
                    var arr = _currentOwner.PortraitArray;
                    _currentImage =
                        (arr != null && _selectedImageIndex < arr.Length)
                            ? arr[_selectedImageIndex]
                            : null;
                    RefreshPreview();
                }

                return;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            // Left: layers list
            EditorGUILayout.BeginVertical(GUILayout.Width(600));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var layers = _currentImage.Layers;
            if (layers == null || layers.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No layers defined. Use the right column to add layers.",
                    MessageType.Info
                );
            }
            else
            {
                EnsureLayersReorderList();

                if (_layersReorderList != null)
                {
                    _layersSerializedObject?.Update();
                    _layersReorderList.DoLayoutList();

                    if (
                        _layersSerializedObject != null
                        && _layersSerializedObject.ApplyModifiedProperties()
                    )
                    {
                        EditorUtility.SetDirty(_currentOwner);
                        if (_autoRefresh)
                        {
                            RefreshPreview();
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right: assign, preview, metadata, tinting
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(420));
            DrawImageStackSection();
            EditorGUILayout.Space(8);
            DrawPreviewPanel();
            EditorGUILayout.Space(10);
            DrawImageMetadataSection();
            EditorGUILayout.Space(8);
            DrawTintingSection();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void EnsureLayersReorderList()
        {
            if (_currentImage == null)
            {
                _layersReorderList = null;
                _layersSerializedObject = null;
                return;
            }

            // No serialized backing available for in-object Layers; we'll operate directly on the list.
            _layersSerializedObject = null;

            // Mandatory layer creation is handled by the StackedImage/Portrait implementation
            // via the Portrait.MandatoryTags override, so we don't duplicate that work here.

            // Convert any tagged layer to UnmaskedImageStackLayer so it carries the per-layer
            // Tint used by the compositor and editor. We'll also enforce canonical ordering
            // for mandatory tags (front-to-back) so that the UI always shows a predictable
            // stacking order.
            for (int i = 0; i < _currentImage.Layers.Count; i++)
            {
                var l = _currentImage.Layers[i];
                if (l != null && !string.IsNullOrEmpty(l.Tag))
                {
                    if (l is not UnmaskedImageStackLayer)
                    {
                        var converted = new UnmaskedImageStackLayer();
                        converted.Sprite = l.Sprite;
                        converted.Mask = null; // unmasked
                        converted.Offset = l.Offset;
                        converted.Scale = l.Scale;
                        converted.Rotation = l.Rotation;
                        converted.Order = l.Order;
                        converted.Tag = l.Tag;
                        converted.Tint = Color.white;

                        _currentImage.Layers[i] = converted;
                        EditorUtility.SetDirty(_currentOwner);
                        Debug.Log(
                            "Converted ImageStack layer at index "
                                + i
                                + " to UnmaskedImageStackLayer for tag '"
                                + l.Tag
                                + "'."
                        );
                    }
                }
            }

            // Enforce canonical ordering for mandatory tags among other layers, but
            // keep newly-added (untagged) layers at the front so they remain where
            // the user placed them. Algorithm:
            // 1. Add untagged layers first (preserve original order) so new layers
            //    inserted at the top stay there after reopen.
            // 2. Add any remaining optional tagged layers in their original order.
            // 3. Append mandatory tagged layers in the configured canonical
            //    front-to-back order if present.
            var canonical = PortraitLayerTags.MandatoryTags();
            var original = _currentImage.Layers.ToList();
            var result = new System.Collections.Generic.List<ImageStackLayer>();

            // 1) Untagged layers (preserve original order) - keep new layers at front
            foreach (var l in original)
            {
                if (l == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(l.Tag) && !result.Contains(l))
                {
                    result.Add(l);
                }
            }

            // 2) Optional tagged layers (non-mandatory), preserving original order
            foreach (var l in original)
            {
                if (l == null)
                {
                    continue;
                }

                if (
                    !string.IsNullOrEmpty(l.Tag)
                    && !PortraitLayerTags.IsMandatory(l.Tag)
                    && !result.Contains(l)
                )
                {
                    result.Add(l);
                }
            }

            // 3) Mandatory tags in canonical front-to-back order
            foreach (var tag in canonical)
            {
                var found = original.FirstOrDefault(x =>
                    x != null
                    && string.Equals(x.Tag, tag.Name, System.StringComparison.OrdinalIgnoreCase)
                );
                if (found != null && !result.Contains(found))
                {
                    result.Add(found);
                }
            }

            // Replace the layers list contents with the ordered result
            _currentImage.Layers.Clear();
            foreach (var r in result)
            {
                _currentImage.Layers.Add(r);
            }

            // Assign Order values: Face (back) gets 0, others incrementing from 1 (front highest)
            int orderCounter = 1;
            foreach (var l in _currentImage.Layers)
            {
                if (l == null)
                {
                    continue;
                }

                l.Order =
                    !string.IsNullOrEmpty(l.Tag)
                    && string.Equals(l.Tag, "Face", System.StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : orderCounter++;
            }

            // We draw our own per-element remove button so we can hide removal for mandatory layers
            var layersList = _currentImage.Layers;
            _layersReorderList = new ReorderableList(
                layersList,
                typeof(ImageStackLayer),
                true,
                true,
                true,
                true
            );
            _layersReorderList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, $"Layers ({layersList?.Count ?? 0})");
            _layersReorderList.elementHeightCallback = index =>
            {
                var l = (index >= 0 && index < layersList.Count) ? layersList[index] : null;
                if (l == null)
                    return 48f;
                float height = 20f; // header row
                // one compact row for color/offset/scale
                height += 22f;
                // tag row
                height += 20f;
                // optional mask row if untagged
                if (string.IsNullOrEmpty(l.Tag))
                    height += 20f;
                // padding
                height += 6f;
                return height;
            };
            _layersReorderList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.y += 2;
                var layer = (index >= 0 && index < layersList.Count) ? layersList[index] : null;
                if (layer == null)
                    return;

                // Layout constants
                const float previewSize = 48f;
                const float pad = 6f;
                float x = rect.x;
                float y = rect.y;
                float width = rect.width;

                // Header row: small foldout-like label with layer name or tag + sprite name
                Rect headerRect = new Rect(x, y, width - previewSize - pad, 18f);
                string headerLabel = !string.IsNullOrEmpty(layer.Tag)
                    ? layer.Tag
                    : $"Layer {index}";
                EditorGUI.LabelField(headerRect, headerLabel, EditorStyles.boldLabel);

                // Sprite selector (compact) to the right of the header label
                Rect selectRect = new Rect(
                    headerRect.xMax - 88f,
                    headerRect.y,
                    86f,
                    headerRect.height
                );
                if (!string.IsNullOrEmpty(layer.Tag))
                {
                    var sprites = PortraitLayerSpriteCache.GetSprites(layer.Tag);
                    var names = PortraitLayerSpriteCache.GetNames(layer.Tag);
                    int currentIndex = -1;
                    if (sprites != null)
                    {
                        for (int i = 0; i < sprites.Length; i++)
                        {
                            if (sprites[i] == layer.Sprite)
                            {
                                currentIndex = i;
                                break;
                            }
                        }
                    }

                    string btnLabel =
                        currentIndex >= 0 && names != null ? names[currentIndex] : "Select...";
                    var btnContent = new GUIContent(
                        btnLabel,
                        $"Open sprite picker for tag '{layer.Tag}'."
                    );
                    if (GUI.Button(selectRect, btnContent))
                    {
                        var popup = new FilteredSpritePicker(
                            layer.Tag,
                            s =>
                            {
                                Undo.RecordObject(_currentOwner, "Portrait layer sprite");
                                layer.Sprite = s;
                                EditorUtility.SetDirty(_currentOwner);
                                if (_autoRefresh)
                                    RefreshPreview();
                            },
                            currentIndex >= 0 && sprites != null ? sprites[currentIndex] : null
                        );
                        PopupWindow.Show(selectRect, popup);
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    var spr = (Sprite)
                        EditorGUI.ObjectField(
                            selectRect,
                            GUIContent.none,
                            layer.Sprite,
                            typeof(Sprite),
                            false
                        );
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_currentOwner, "Change layer sprite");
                        layer.Sprite = spr;
                        EditorUtility.SetDirty(_currentOwner);
                        if (_autoRefresh)
                            RefreshPreview();
                    }
                }

                // Preview on the right (top-aligned)
                Rect previewRect = new Rect(x + width - previewSize, y, previewSize, previewSize);
                if (layer.Sprite != null)
                {
                    EditorGUI.DrawPreviewTexture(
                        previewRect,
                        AssetPreview.GetAssetPreview(layer.Sprite) ?? layer.Sprite.texture
                    );
                    // Draw order badge
                    Rect orderRect = new Rect(previewRect.x + 2, previewRect.y + 2, 18, 18);
                    EditorGUI.DrawRect(orderRect, new Color(0f, 0f, 0f, 0.7f));
                    GUIStyle orderStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white },
                        fontStyle = FontStyle.Bold,
                        fontSize = 10,
                    };
                    GUI.Label(orderRect, layer.Order.ToString(), orderStyle);
                }

                y += 20f;

                // Compact row: Layer Color (if unmasked), Offset X/Y, Scale
                float leftAreaWidth = width - previewSize - pad;
                float cursorX = x;

                // Layer Color (for Unmasked layers)
                if (layer is UnmaskedImageStackLayer u)
                {
                    // Compact label + small color field to avoid overlap with the Offset label
                    Rect colorLabelRect = new Rect(cursorX, y, 70f, 18f);
                    EditorGUI.LabelField(
                        colorLabelRect,
                        new GUIContent(
                            "Layer Color",
                            "Per-layer tint: applied to unmasked grayscale sprites (e.g., hair). Not applied if sprite already contains color."
                        )
                    );
                    Rect colorFieldRect = new Rect(colorLabelRect.xMax + 6f, y, 36f, 18f);
                    EditorGUI.BeginChangeCheck();
                    var newCol = EditorGUI.ColorField(colorFieldRect, GUIContent.none, u.Tint);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_currentOwner, "Change layer tint");
                        u.Tint = newCol;
                        EditorUtility.SetDirty(_currentOwner);
                        if (_autoRefresh)
                            RefreshPreview();
                    }
                    cursorX += colorLabelRect.width + 6f + colorFieldRect.width + 8f;
                }

                // Offset X / Y fields (inline) - draw small label then unlabeled float fields to avoid internal label overlap
                Rect offsetLabelRect = new Rect(cursorX, y, 36f, 18f);
                EditorGUI.LabelField(
                    offsetLabelRect,
                    new GUIContent(
                        "Offset",
                        "Pixel offset applied to the layer when compositing (X, Y)."
                    )
                );
                Rect offXRect = new Rect(offsetLabelRect.xMax + 6f, y, 52f, 18f);
                Rect offYRect = new Rect(offXRect.xMax + 6f, y, 52f, 18f);
                Rect scaleLabelRect = new Rect(offYRect.xMax + 8f, y, 40f, 18f);
                Rect scaleValRect = new Rect(scaleLabelRect.xMax + 6f, y, 48f, 18f);

                EditorGUI.BeginChangeCheck();
                float newOffX = EditorGUI.FloatField(offXRect, GUIContent.none, layer.Offset.x);
                float newOffY = EditorGUI.FloatField(offYRect, GUIContent.none, layer.Offset.y);
                EditorGUI.LabelField(
                    scaleLabelRect,
                    new GUIContent("Scale", "Scale multiplier applied to the layer sprite.")
                );
                float newScale = EditorGUI.FloatField(scaleValRect, GUIContent.none, layer.Scale);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_currentOwner, "Change layer transform");
                    layer.Offset = new Vector2(newOffX, newOffY);
                    layer.Scale = newScale;
                    EditorUtility.SetDirty(_currentOwner);
                    if (_autoRefresh)
                        RefreshPreview();
                }

                y += 22f;

                // Tag row / Mask field (for untagged layers show Mask)
                if (string.IsNullOrEmpty(layer.Tag))
                {
                    EditorGUI.BeginChangeCheck();
                    var mask = (Sprite)
                        EditorGUI.ObjectField(
                            new Rect(x, y, width - previewSize - pad, 18f),
                            new GUIContent(
                                "Mask",
                                "Mask sprite whose RGB channels control the global Tint Colors when present."
                            ),
                            layer.Mask,
                            typeof(Sprite),
                            false
                        );
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_currentOwner, "Change layer mask");
                        layer.Mask = mask;
                        EditorUtility.SetDirty(_currentOwner);
                        if (_autoRefresh)
                            RefreshPreview();
                    }
                }
                else
                {
                    // Tag selector (compact)
                    string[] all = PortraitLayerTags.Names();
                    string[] popupOptions = new string[all.Length + 1];
                    popupOptions[0] = "<none>";
                    for (int i = 0; i < all.Length; i++)
                        popupOptions[i + 1] = all[i];

                    int tagIdx = 0;
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (string.Equals(all[i], layer.Tag, StringComparison.OrdinalIgnoreCase))
                        {
                            tagIdx = i + 1;
                            break;
                        }
                    }

                    // Tag label + popup (compact)
                    EditorGUI.LabelField(
                        new Rect(x, y, 48f, 18f),
                        new GUIContent(
                            "Tag",
                            "Select a portrait layer tag. Mandatory tags are locked and only one layer per tag is allowed."
                        )
                    );
                    int newTagSel = EditorGUI.Popup(
                        new Rect(x + 52f, y, width - previewSize - pad - 52f, 18f),
                        tagIdx,
                        popupOptions
                    );
                    string newTag = newTagSel == 0 ? string.Empty : all[newTagSel - 1];
                    if (
                        !string.Equals(
                            layer.Tag ?? string.Empty,
                            newTag,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        // enforce uniqueness
                        bool duplicate = false;
                        for (int _i = 0; _i < layersList.Count; _i++)
                            if (
                                _i != index
                                && string.Equals(
                                    layersList[_i]?.Tag,
                                    newTag,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                duplicate = true;
                                break;
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
                            Undo.RecordObject(_currentOwner, "Change layer tag");
                            layer.Tag = newTag;
                            if (!string.IsNullOrEmpty(newTag))
                                layer.Mask = null;
                            if (
                                !string.IsNullOrEmpty(newTag)
                                && string.Equals(newTag, "Face", StringComparison.OrdinalIgnoreCase)
                            )
                                layer.Order = 0;
                            EditorUtility.SetDirty(_currentOwner);
                            if (_autoRefresh)
                                RefreshPreview();
                        }
                    }
                }
            };

            _layersReorderList.onAddCallback = list =>
            {
                var newLayer = new ImageStackLayer()
                {
                    Sprite = null,
                    Mask = null,
                    Offset = Vector2.zero,
                    Scale = 1f,
                    Rotation = 0f,
                    Order = layersList.Count > 0 ? layersList.Max(l => l?.Order ?? 0) + 1 : 1,
                    Tag = string.Empty,
                    Tint = Color.white,
                };
                Undo.RecordObject(_currentOwner, "Add layer");
                layersList.Insert(0, newLayer);
                EditorUtility.SetDirty(_currentOwner);
                if (_autoRefresh)
                    RefreshPreview();
            };

            _layersReorderList.onRemoveCallback = list =>
            {
                int removeIndex = list.index;
                if (removeIndex < 0 || removeIndex >= layersList.Count)
                    return;

                var layer = layersList[removeIndex];
                if (!string.IsNullOrEmpty(layer.Tag) && PortraitLayerTags.IsMandatory(layer.Tag))
                {
                    EditorUtility.DisplayDialog(
                        "Cannot remove layer",
                        $"The '{layer.Tag}' layer is mandatory for portraits and cannot be removed.",
                        "OK"
                    );
                    return;
                }

                Undo.RecordObject(_currentOwner, "Remove layer");
                layersList.RemoveAt(removeIndex);
                // Reassign orders
                for (int i = 0; i < layersList.Count; i++)
                {
                    layersList[i].Order = (layersList.Count - 1) - i;
                }
                EditorUtility.SetDirty(_currentOwner);
                if (_autoRefresh)
                    RefreshPreview();
            };

            _layersReorderList.onChangedCallback = list =>
            {
                // Recompute orders: front-most (index 0) gets highest order
                for (int i = 0; i < layersList.Count; i++)
                {
                    layersList[i].Order = (layersList.Count - 1) - i;
                }
                Undo.RecordObject(_currentOwner, "Reorder layers");
                EditorUtility.SetDirty(_currentOwner);
                if (_autoRefresh)
                    RefreshPreview();
            };
        }

        protected override Portrait[] GetImagesFromOwner(CharacterData owner)
        {
            if (owner?.Portraits == null)
            {
                return null;
            }

            var arr = owner?.PortraitArray;
            if (arr == null || arr.Length == 0)
            {
                return null;
            }

            var nonNull = arr.Where(p => p != null).ToArray();
            if (nonNull.Length == 0)
            {
                return null;
            }

            foreach (var p in nonNull)
            {
                p.SetOwner(owner);
            }

            return nonNull;
        }
    }
}
