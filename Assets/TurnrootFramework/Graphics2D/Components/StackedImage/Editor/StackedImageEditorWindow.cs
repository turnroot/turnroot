using UnityEditor;
using UnityEngine;

namespace Turnroot.Graphics2D.Editor
{
    /// <summary>
    /// Base class for editor windows that provide live editing of stacked images.
    /// Provides common functionality for managing layers, tint colors, and preview rendering.
    /// </summary>
    /// <typeparam name="TOwner">The Unity Object type that owns the stacked images</typeparam>
    /// <typeparam name="TStackedImage">The stacked image type being edited</typeparam>
    public abstract class StackedImageEditorWindow<TOwner, TStackedImage> : EditorWindow
        where TOwner : Object
        where TStackedImage : StackedImage<TOwner>
    {
        protected TOwner _currentOwner;
        protected int _selectedImageIndex = 0;
        protected TStackedImage _currentImage;
        protected Vector2 _scrollPosition;
        protected Texture2D _previewTexture;
        protected bool _autoRefresh = true;
        protected int _selectedLayerIndex = -1;

        // Tracks a simple hash of the current image's layer state to detect external changes
        private int _lastLayerHash = 0;

        protected abstract string WindowTitle { get; }
        protected abstract string OwnerFieldLabel { get; }
        protected abstract TStackedImage[] GetImagesFromOwner(TOwner owner);

        protected virtual void SetImagesToOwner(TOwner owner, TStackedImage[] images) { }

        protected virtual void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;

        protected virtual void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        private void OnUndoRedo()
        {
            _lastLayerHash = 0; // Force next check to trigger refresh
            Repaint();
        }

        protected virtual void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                if (_currentImage != null && _autoRefresh && ShouldRefreshPreview())
                {
                    RefreshPreview();
                }
            }

            EditorGUILayout.LabelField($"Live {WindowTitle}", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            DrawOwnerSelection();
            if (_currentOwner == null)
            {
                EditorGUILayout.HelpBox(
                    $"Select a {OwnerFieldLabel} to edit its images.",
                    MessageType.Info
                );
                return;
            }

            if (!DrawImageSelection())
            {
                return;
            }

            if (_currentImage == null)
            {
                EditorGUILayout.HelpBox("Selected image is null.", MessageType.Error);
                return;
            }

            // Show validation status if there are any issues
            if (_currentImage.HasValidationError)
            {
                EditorGUILayout.HelpBox(_currentImage.ValidationMessage, MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space(10);
            DrawMainLayout();
        }

        private void DrawOwnerSelection()
        {
            EditorGUI.BeginChangeCheck();
            _currentOwner =
                EditorGUILayout.ObjectField(OwnerFieldLabel, _currentOwner, typeof(TOwner), false)
                as TOwner;
            if (EditorGUI.EndChangeCheck())
            {
                _selectedImageIndex = 0;
                UpdateCurrentImage();
            }
        }

        private bool DrawImageSelection()
        {
            var images = GetImagesFromOwner(_currentOwner);
            if (images == null || images.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"This {OwnerFieldLabel} has no images. Add images first.",
                    MessageType.Warning
                );
                return false;
            }

            if (images.Length > 1)
            {
                EditorGUI.BeginChangeCheck();
                string[] imageNames = new string[images.Length];
                for (int i = 0; i < images.Length; i++)
                {
                    imageNames[i] =
                        images[i] != null ? $"Image {i}: {images[i].Key}" : $"Image {i}: (null)";
                }

                _selectedImageIndex = EditorGUILayout.Popup(
                    "Select Image",
                    _selectedImageIndex,
                    imageNames
                );
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateCurrentImage();
                }
            }
            else if (_currentImage == null)
            {
                _currentImage = images[0];
                RefreshPreview();
            }

            return true;
        }

        private void DrawMainLayout()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawControlPanel();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(350));
            DrawPreviewPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        protected virtual void DrawControlPanel()
        {
            DrawImageMetadataSection();
            DrawImageStackSection();
            DrawOwnerSection();
            DrawTintingSection();
            DrawLayerManagementSection();
        }

        protected void DrawImageMetadataSection()
        {
            if (string.IsNullOrEmpty(_currentImage.Key))
            {
                EditorGUILayout.HelpBox(
                    "Warning: Key is empty. It will be auto-generated when rendering.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space(10);
        }

        protected void DrawImageStackSection()
        {
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            if (_currentImage.Layers == null || _currentImage.Layers.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No layers defined. Use the Layer Management section to add layers.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space(10);
        }

        protected void DrawOwnerSection()
        {
            EditorGUILayout.LabelField("Owner", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(
                "Current Owner",
                _currentImage.Owner,
                typeof(TOwner),
                false
            );
            EditorGUI.EndDisabledGroup();

            if (_currentImage.Owner == null)
            {
                EditorGUILayout.HelpBox(
                    "Warning: Owner is not set. This may cause issues with tint colors.",
                    MessageType.Warning
                );
                if (GUILayout.Button($"Set Owner to Current {OwnerFieldLabel}"))
                {
                    _currentImage.SetOwner(_currentOwner);
                    MarkDirtyAndRefresh();
                }
            }

            EditorGUILayout.Space(10);
        }

        protected void DrawTintingSection()
        {
            EditorGUILayout.LabelField("Tint Colors", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Global Tint Colors are used for mask-based tinting (mask RGB channels map to Tint Color 1/2/3).\n"
                    + "Use per-layer 'Layer Color' (shown on unmasked layers) to colorize grayscale sprites like hair.",
                MessageType.Info
            );

            if (_currentImage.TintColors == null || _currentImage.TintColors.Length < 3)
            {
                EditorGUILayout.HelpBox("Tint colors are not initialized.", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 3; i++)
            {
                _currentImage.TintColors[i] = EditorGUILayout.ColorField(
                    new GUIContent(
                        $"Tint Color {i + 1}",
                        "Global tint color used by mask-based tinting. Red = Tint Color 1, Green = Tint Color 2, Blue = Tint Color 3."
                    ),
                    _currentImage.TintColors[i]
                );
            }

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirtyAndRefresh();
            }

            EditorGUILayout.BeginHorizontal();
            if (
                GUILayout.Button(
                    new GUIContent("Reset to White", "Reset all tint colors to white (no tint).")
                )
            )
            {
                for (int i = 0; i < 3; i++)
                {
                    _currentImage.TintColors[i] = Color.white;
                }

                MarkDirtyAndRefresh();
            }
            if (
                GUILayout.Button(
                    new GUIContent(
                        "Update from Owner",
                        "Copy tint colors from the owner (e.g., character accent colors)."
                    )
                )
            )
            {
                _currentImage.UpdateTintColorsFromOwner();
                MarkDirtyAndRefresh();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        protected void DrawLayerManagementSection()
        {
            EditorGUILayout.LabelField("Layer Management", EditorStyles.boldLabel);

            if (_currentImage.Layers == null)
            {
                EditorGUILayout.HelpBox("No layers defined.", MessageType.Info);
                return;
            }

            var layers = _currentImage.Layers;
            if (layers == null || layers.Count == 0)
            {
                EditorGUILayout.HelpBox("No layers present.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Total Layers: {layers.Count}");

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer == null)
                {
                    continue;
                }

                EditorGUILayout.BeginVertical("box");
                DrawLayerButton(i, layer);
                if (_selectedLayerIndex == i)
                {
                    DrawLayerDetails(layer);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawLayerButton(int index, ImageStackLayer layer)
        {
            bool isSelected = _selectedLayerIndex == index;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = Color.cyan;
            }

            if (GUILayout.Button($"Layer {index}: Order {layer.Order}"))
            {
                _selectedLayerIndex = index;
            }

            GUI.backgroundColor = originalColor;
        }

        private void DrawLayerDetails(ImageStackLayer layer)
        {
            EditorGUI.indentLevel++;

            string spriteName =
                (layer.Sprite != null && layer.Sprite) ? layer.Sprite.name : "(none)";
            string maskName = (layer.Mask != null && layer.Mask) ? layer.Mask.name : "(none)";

            EditorGUILayout.LabelField("Sprite", spriteName);
            EditorGUILayout.LabelField("Mask", maskName);
            EditorGUILayout.LabelField("Order", layer.Order.ToString());
            EditorGUILayout.LabelField("Offset", $"({layer.Offset.x}, {layer.Offset.y})");
            EditorGUILayout.LabelField("Scale", layer.Scale.ToString());

            EditorGUI.indentLevel--;
        }

        protected void DrawPreviewPanel()
        {
            DrawDefaultButtons();
            DrawPreviewTexture();
            DrawRenderSection();
        }

        private void DrawDefaultButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Character Defaults"))
            {
                _currentOwner.SaveDefaults();
                EditorUtility.SetDirty(_currentOwner);
                EditorUtility.DisplayDialog(
                    "Defaults Saved",
                    "Character defaults have been saved.",
                    "OK"
                );
            }
            if (GUILayout.Button("Load Defaults"))
            {
                _currentOwner.LoadDefaults();
                EditorUtility.SetDirty(_currentOwner);
                RefreshPreview();
                EditorUtility.DisplayDialog(
                    "Defaults Loaded",
                    "Character defaults have been loaded.",
                    "OK"
                );
            }
            GUILayout.EndHorizontal();
        }

        private void DrawPreviewTexture()
        {
            if (_previewTexture != null)
            {
                float maxSize = 300f;
                float aspect = (float)_previewTexture.width / _previewTexture.height;
                float displayWidth = aspect > 1f ? maxSize : maxSize * aspect;
                float displayHeight = aspect > 1f ? maxSize / aspect : maxSize;

                GUILayout.Label(
                    _previewTexture,
                    GUILayout.Width(displayWidth),
                    GUILayout.Height(displayHeight)
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No preview available. Click 'Refresh Preview'.",
                    MessageType.Info
                );
            }

            if (_currentImage.SavedSprite != null)
            {
                string spritePath =
                    $"Resources/GameContent/Graphics/Portraits/{_currentImage.Key}.png";
                EditorGUILayout.LabelField("Path", spritePath);
            }

            EditorGUILayout.Space(10);
        }

        private void DrawRenderSection()
        {
            var layers = _currentImage.Layers;
            if (layers == null || layers.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Cannot render: No layers defined on this image.",
                    MessageType.Warning
                );
                return;
            }

            if (string.IsNullOrEmpty(_currentImage.Key))
            {
                EditorGUILayout.HelpBox(
                    "Warning: Key is empty. A key will be auto-generated.",
                    MessageType.Warning
                );
            }

            if (GUILayout.Button("Render and Save to File", GUILayout.Height(40)))
            {
#if UNITY_EDITOR
                Debug.Log($"Saving image with key: '{_currentImage.Key}'");
#endif
                _currentImage.Render();

                // Show appropriate message based on whether there were validation errors
                if (_currentImage.HasValidationError)
                {
                    EditorUtility.DisplayDialog(
                        "Render Complete - Warning",
                        $"Image rendered but there were issues:\n{_currentImage.ValidationMessage}",
                        "OK"
                    );
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Render Complete",
                        $"Image has been rendered and saved successfully.",
                        "OK"
                    );
                }

                EditorUtility.SetDirty(_currentOwner);
                AssetDatabase.SaveAssets();
                RefreshPreview();
                Repaint(); // Force UI refresh to show any validation warnings
            }
        }

        protected void UpdateCurrentImage()
        {
            if (_currentOwner == null)
            {
                _currentImage = null;
                return;
            }

            var images = GetImagesFromOwner(_currentOwner);
            if (images != null && _selectedImageIndex >= 0 && _selectedImageIndex < images.Length)
            {
                _currentImage = images[_selectedImageIndex];

                // Clear any previous validation status when switching images
                _currentImage.ClearValidationStatus();

                // Ensure mandatory layers are present before generating a preview
                _ = _currentImage.Layers;

                RefreshPreview();
            }
            else
            {
                _currentImage = null;
            }
        }

        protected void RefreshPreview()
        {
            if (_currentImage == null)
            {
#if UNITY_EDITOR
                Debug.Log("RefreshPreview: _currentImage is null");
#endif
                _previewTexture = null;
                return;
            }

            var layers = _currentImage.Layers;
            if (layers == null || layers.Count == 0)
            {
#if UNITY_EDITOR
                Debug.Log($"RefreshPreview: No layers defined for image '{_currentImage?.Key}'");
#endif
                _previewTexture = null;
                return;
            }

            Debug.Log(
                $"RefreshPreview: Compositing image '{_currentImage.Key}' using in-object layers"
            );
            _previewTexture = _currentImage.CompositeLayers();

            if (_previewTexture == null)
            {
                Debug.LogWarning(
                    $"RefreshPreview: CompositeLayers returned null for '{_currentImage.Key}'"
                );
                if (_currentImage.SavedSprite?.texture != null)
                {
#if UNITY_EDITOR
                    Debug.Log("RefreshPreview: using SavedSprite.texture as fallback preview");
#endif
                    _previewTexture = _currentImage.SavedSprite.texture;
                }
            }

            Repaint();
        }

        private void MarkDirtyAndRefresh()
        {
            EditorUtility.SetDirty(_currentOwner);
            if (_autoRefresh)
            {
                RefreshPreview();
            }
        }

        private bool ShouldRefreshPreview()
        {
            if (_currentImage == null || _currentImage.Layers == null)
            {
                return false;
            }

            unchecked
            {
                int hash = 17;
                var layers = _currentImage.Layers;
                hash = hash * 23 + (layers?.Count ?? 0);
                if (layers != null)
                {
                    for (int i = 0; i < layers.Count; i++)
                    {
                        var l = layers[i];
                        if (l == null)
                        {
                            continue;
                        }

                        hash = hash * 23 + (l.Tag ?? string.Empty).GetHashCode();
                        hash = hash * 23 + l.Order.GetHashCode();
                        hash = hash * 23 + (l.Sprite != null ? l.Sprite.GetInstanceID() : 0);
                        hash = hash * 23 + (l.Mask != null ? l.Mask.GetInstanceID() : 0);
                    }
                }

                if (hash != _lastLayerHash)
                {
                    _lastLayerHash = hash;
                    return true;
                }

                return false;
            }
        }
    }
}
