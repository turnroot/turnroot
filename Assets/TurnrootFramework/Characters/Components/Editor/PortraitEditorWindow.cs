using System.Linq;
using Turnroot.Graphics2D.Editor;
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
        private Turnroot.Graphics.Portrait.ImageStack _lastListImageStack;
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

        protected override void OnGUI()
        {
            EditorGUILayout.LabelField($"Live {WindowTitle}", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Owner selection
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
                // add a button and a text field (with validation) to add a new portrait key
                GUILayout.BeginHorizontal();
                _newPortraitName = GUILayout.TextField(_newPortraitName);
                if (GUILayout.Button("Create"))
                {
                    string newKey = _newPortraitName;
                    if (string.IsNullOrWhiteSpace(newKey))
                    {
                        newKey = _currentOwner.FullName + "_Portrait";
                    }
                    string baseKey = newKey;
                    int suffix = 1;
                    while (portraitsDict.ContainsKey(newKey))
                    {
                        newKey = baseKey + "_" + suffix;
                        suffix++;
                    }

                    var p = new Portrait();
                    p.SetOwner(_currentOwner);
                    p.SetKey(newKey);
                    portraitsDict[newKey] = p;
                    _currentOwner.InvalidatePortraitArrayCache();
                    EditorUtility.SetDirty(_currentOwner);
                    _selectedImageIndex = portraitsDict.Count - 1; // select the new portrait
                    UpdateCurrentImage();

                    // Create new ImageStack for the portrait
                    var newImageStack =
                        ScriptableObject.CreateInstance<Turnroot.Graphics.Portrait.ImageStack>();
                    string defaultName = newKey + ".asset";
                    string path = EditorUtility.SaveFilePanelInProject(
                        "Create New Image Stack for Portrait",
                        defaultName,
                        "asset",
                        "Choose where to save the new ImageStack"
                    );

                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(newImageStack, path);
                        AssetDatabase.SaveAssets();

                        // Assign it to the current portrait
                        _currentImage.SetImageStack(newImageStack);
                    }

                    RefreshPreview();
                }
                GUILayout.EndHorizontal();
                return;
            }

            var keys = portraitsDict.Keys.ToArray();
            string[] portraitNames = keys.Select(k => k).ToArray();

            GUILayout.BeginHorizontal();
            int newIndex = EditorGUILayout.Popup(
                "Select Portrait",
                _selectedImageIndex,
                portraitNames
            );
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
                string newKey = _quickPortraitName;
                if (string.IsNullOrWhiteSpace(newKey))
                {
                    newKey = _currentOwner.FullName + "_Portrait";
                }
                string baseKey = newKey;
                int suffix = 1;
                while (portraitsDict.ContainsKey(newKey))
                {
                    newKey = baseKey + "_" + suffix;
                    suffix++;
                }

                var p = new Portrait();
                p.SetOwner(_currentOwner);
                p.SetKey(newKey);
                portraitsDict[newKey] = p;
                _currentOwner.InvalidatePortraitArrayCache();
                EditorUtility.SetDirty(_currentOwner);
                _selectedImageIndex = keys.Length; // select the new portrait
                UpdateCurrentImage();

                // Create new ImageStack for the portrait
                var newImageStack =
                    ScriptableObject.CreateInstance<Turnroot.Graphics.Portrait.ImageStack>();
                string defaultName = newKey + ".asset";
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create New Image Stack for Portrait",
                    defaultName,
                    "asset",
                    "Choose where to save the new ImageStack"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(newImageStack, path);
                    AssetDatabase.SaveAssets();

                    // Assign it to the current portrait
                    _currentImage.SetImageStack(newImageStack);
                }

                RefreshPreview();
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

            var imageStack = _currentImage.ImageStack;
            if (imageStack == null)
            {
                EditorGUILayout.HelpBox(
                    "No ImageStack assigned. Use the right column to create or assign one.",
                    MessageType.Info
                );
            }
            else
            {
                EnsureLayersReorderList(imageStack);

                if (_layersReorderList != null)
                {
                    _layersSerializedObject?.Update();
                    _layersReorderList.DoLayoutList();

                    if (
                        _layersSerializedObject != null
                        && _layersSerializedObject.ApplyModifiedProperties()
                    )
                    {
                        EditorUtility.SetDirty(imageStack);
                        if (_autoRefresh)
                            RefreshPreview();
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

        private void EnsureLayersReorderList(Turnroot.Graphics.Portrait.ImageStack imageStack)
        {
            if (imageStack == null)
            {
                _layersReorderList = null;
                _layersSerializedObject = null;
                _lastListImageStack = null;
                return;
            }

            if (_lastListImageStack == imageStack && _layersReorderList != null)
                return;

            _layersSerializedObject = new SerializedObject(imageStack);
            var layersProp = _layersSerializedObject.FindProperty("_layers");

            // Ensure mandatory portrait layers exist. Add any missing mandatory tags.
            var existingTags = new System.Collections.Generic.HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase
            );
            for (int i = 0; i < imageStack.Layers.Count; i++)
            {
                var l = imageStack.Layers[i];
                if (l != null && !string.IsNullOrEmpty(l.Tag))
                    existingTags.Add(l.Tag);
            }

            bool addedAny = false;
            foreach (var t in Turnroot.Characters.PortraitLayerTags.Mandatory)
            {
                if (!existingTags.Contains(t))
                {
                    // Any named portrait tag is represented as an unmasked layer so it can
                    // receive per-layer tinting and be composited appropriately.
                    var layer = new UnmaskedImageStackLayer()
                    {
                        Sprite = null,
                        Mask = null,
                        Offset = Vector2.zero,
                        Scale = 1f,
                        Rotation = 0f,
                        Order = 0,
                        Tag = t,
                        Tint = Color.white,
                    };
                    imageStack.Layers.Add(layer);
                    addedAny = true;
                }
            }

            if (addedAny)
            {
                EditorUtility.SetDirty(imageStack);
                // Recreate the serialized object so layersProp reflects the inserted elements
                _layersSerializedObject = new SerializedObject(imageStack);
                layersProp = _layersSerializedObject.FindProperty("_layers");
            }

            // Convert any tagged layer to UnmaskedImageStackLayer so it carries the per-layer
            // Tint used by the compositor and editor. We'll also enforce canonical ordering
            // for mandatory tags (front-to-back) so that the UI always shows a predictable
            // stacking order.
            for (int i = 0; i < imageStack.Layers.Count; i++)
            {
                var l = imageStack.Layers[i];
                if (l != null && !string.IsNullOrEmpty(l.Tag))
                {
                    if (!(l is UnmaskedImageStackLayer))
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

                        imageStack.Layers[i] = converted;
                        EditorUtility.SetDirty(imageStack);
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
            var canonical = Turnroot.Characters.PortraitLayerTags.CanonicalFrontToBackMandatory;
            var original = imageStack.Layers.ToList();
            var result = new System.Collections.Generic.List<ImageStackLayer>();

            // 1) Untagged layers (preserve original order) - keep new layers at front
            foreach (var l in original)
            {
                if (l == null)
                    continue;
                if (string.IsNullOrEmpty(l.Tag) && !result.Contains(l))
                    result.Add(l);
            }

            // 2) Optional tagged layers (non-mandatory), preserving original order
            foreach (var l in original)
            {
                if (l == null)
                    continue;
                if (
                    !string.IsNullOrEmpty(l.Tag)
                    && !Turnroot.Characters.PortraitLayerTags.IsMandatory(l.Tag)
                    && !result.Contains(l)
                )
                    result.Add(l);
            }

            // 3) Mandatory tags in canonical front-to-back order
            foreach (var tag in canonical)
            {
                var found = original.FirstOrDefault(x =>
                    x != null
                    && string.Equals(x.Tag, tag, System.StringComparison.OrdinalIgnoreCase)
                );
                if (found != null && !result.Contains(found))
                    result.Add(found);
            }

            // Replace the layers list contents with the ordered result
            imageStack.Layers.Clear();
            foreach (var r in result)
                imageStack.Layers.Add(r);

            // Assign Order values: Face (back) gets 0, others incrementing from 1 (front highest)
            int orderCounter = 1;
            foreach (var l in imageStack.Layers)
            {
                if (l == null)
                    continue;
                if (
                    !string.IsNullOrEmpty(l.Tag)
                    && string.Equals(l.Tag, "Face", System.StringComparison.OrdinalIgnoreCase)
                )
                    l.Order = 0;
                else
                    l.Order = orderCounter++;
            }

            // We draw our own per-element remove button so we can hide removal for mandatory layers
            _layersReorderList = new ReorderableList(
                _layersSerializedObject,
                layersProp,
                true,
                true,
                true,
                false
            );
            _layersReorderList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, $"Layers ({layersProp.arraySize})");
            _layersReorderList.elementHeightCallback = index =>
                EditorGUI.GetPropertyHeight(layersProp.GetArrayElementAtIndex(index), true) + 12;
            _layersReorderList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var el = layersProp.GetArrayElementAtIndex(index);
                rect.y += 2;
                // Draw the element property field
                Rect fieldRect = new Rect(
                    rect.x,
                    rect.y,
                    rect.width - 60,
                    EditorGUI.GetPropertyHeight(el, true)
                );
                EditorGUI.PropertyField(fieldRect, el, new GUIContent($"Layer {index}"), true);

                // Draw a per-element remove button unless the layer is tagged as mandatory
                var tagProp = el.FindPropertyRelative("Tag");
                string tag = tagProp != null ? tagProp.stringValue : string.Empty;
                if (
                    string.IsNullOrEmpty(tag)
                    || !Turnroot.Characters.PortraitLayerTags.IsMandatory(tag)
                )
                {
                    Rect btnRect = new Rect(fieldRect.xMax + 4, fieldRect.y, 56, 18);
                    if (GUI.Button(btnRect, "Remove"))
                    {
                        layersProp.DeleteArrayElementAtIndex(index);
                        _layersSerializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(imageStack);
                        if (_autoRefresh)
                            RefreshPreview();
                    }
                }
            };

            var stackRef = imageStack;

            _layersReorderList.onAddCallback = list =>
            {
                // Increase the array size, then move the newly-created slot to index 0
                layersProp.arraySize++;
                int srcIndex = layersProp.arraySize - 1;
                int newIndex = 0;
                if (srcIndex != newIndex)
                    layersProp.MoveArrayElement(srcIndex, newIndex);

                var newEl = layersProp.GetArrayElementAtIndex(newIndex);
                if (newEl != null)
                {
                    var spriteProp = newEl.FindPropertyRelative("Sprite");
                    var maskProp = newEl.FindPropertyRelative("Mask");
                    var offsetProp = newEl.FindPropertyRelative("Offset");
                    var scaleProp = newEl.FindPropertyRelative("Scale");
                    var rotationProp = newEl.FindPropertyRelative("Rotation");
                    var orderProp = newEl.FindPropertyRelative("Order");
                    var tagProp = newEl.FindPropertyRelative("Tag");
                    var tintProp = newEl.FindPropertyRelative("Tint");

                    if (spriteProp != null)
                        spriteProp.objectReferenceValue = null;
                    if (maskProp != null)
                        maskProp.objectReferenceValue = null;
                    if (offsetProp != null)
                        offsetProp.vector2Value = Vector2.zero;
                    if (scaleProp != null)
                        scaleProp.floatValue = 1f;
                    if (rotationProp != null)
                        rotationProp.floatValue = 0f;

                    // Put new layer at the front: give it a high Order so it renders on top.
                    if (orderProp != null)
                    {
                        int maxOrder = int.MinValue;
                        for (int i = 0; i < layersProp.arraySize; i++)
                        {
                            if (i == newIndex)
                                continue;
                            var o = layersProp
                                .GetArrayElementAtIndex(i)
                                .FindPropertyRelative("Order");
                            if (o != null)
                                maxOrder = Mathf.Max(maxOrder, o.intValue);
                        }
                        orderProp.intValue = (maxOrder == int.MinValue) ? 1 : (maxOrder + 1);
                    }

                    // Ensure new layers are normal (no tag) and have default tint/manual settings
                    if (tagProp != null)
                        tagProp.stringValue = string.Empty;
                    if (tintProp != null)
                        tintProp.colorValue = Color.white;
                }

                _layersSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(stackRef);
                if (_autoRefresh)
                    RefreshPreview();
            };

            _layersReorderList.onRemoveCallback = list =>
            {
                int removeIndex = list.index;

                // Prevent removal of mandatory portrait layers identified by Tag (e.g., "Hair")
                var elToRemove = layersProp.GetArrayElementAtIndex(removeIndex);
                if (elToRemove != null)
                {
                    var tagProp = elToRemove.FindPropertyRelative("Tag");
                    if (tagProp != null && !string.IsNullOrEmpty(tagProp.stringValue))
                    {
                        string tag = tagProp.stringValue;
                        if (Turnroot.Characters.PortraitLayerTags.IsMandatory(tag))
                        {
                            EditorUtility.DisplayDialog(
                                "Cannot remove layer",
                                $"The '{tag}' layer is mandatory for portraits and cannot be removed.",
                                "OK"
                            );
                            return;
                        }
                    }
                }

                layersProp.DeleteArrayElementAtIndex(removeIndex);
                _layersSerializedObject.ApplyModifiedProperties();

                for (int i = 0; i < layersProp.arraySize; i++)
                {
                    var el = layersProp.GetArrayElementAtIndex(i);
                    var orderProp = el.FindPropertyRelative("Order");
                    if (orderProp != null)
                        orderProp.intValue = (layersProp.arraySize - 1) - i;
                }

                _layersSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(stackRef);
                if (_autoRefresh)
                    RefreshPreview();
            };

            _layersReorderList.onChangedCallback = list =>
            {
                for (int i = 0; i < layersProp.arraySize; i++)
                {
                    var el = layersProp.GetArrayElementAtIndex(i);
                    var orderProp = el.FindPropertyRelative("Order");
                    if (orderProp != null)
                        orderProp.intValue = (layersProp.arraySize - 1) - i;
                }

                _layersSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(stackRef);
                if (_autoRefresh)
                    RefreshPreview();
            };

            _lastListImageStack = imageStack;
        }

        protected override Portrait[] GetImagesFromOwner(CharacterData owner)
        {
            if (owner?.Portraits == null)
                return null;

            var arr = owner?.PortraitArray;
            if (arr == null || arr.Length == 0)
                return null;

            var nonNull = arr.Where(p => p != null).ToArray();
            if (nonNull.Length == 0)
                return null;

            foreach (var p in nonNull)
                p.SetOwner(owner);
            return nonNull;
        }
    }
}
