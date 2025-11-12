using Turnroot.Graphics.Portrait;
using Turnroot.Graphics2D;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Graphics2D.Editor
{
    public abstract class StackedImageEditorWindow<TOwner, TStackedImage> : EditorWindow
        where TOwner : UnityEngine.Object
        where TStackedImage : StackedImage<TOwner>
    {
        protected TOwner _currentOwner;
        protected int _selectedImageIndex = 0;
        protected TStackedImage _currentImage;
        protected Vector2 _scrollPosition;
        protected Texture2D _previewTexture;
        protected bool _autoRefresh = true;
        protected int _selectedLayerIndex = -1;

        protected abstract string WindowTitle { get; }
        protected abstract string OwnerFieldLabel { get; }
        protected abstract TStackedImage[] GetImagesFromOwner(TOwner owner);

        protected virtual void SetImagesToOwner(TOwner owner, TStackedImage[] images)
        {
            // Optional override - not all implementations need to set images back to owner
        }

        protected virtual void OnGUI()
        {
            EditorGUILayout.LabelField($"Live {WindowTitle}", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Owner selection
            EditorGUI.BeginChangeCheck();
            _currentOwner =
                EditorGUILayout.ObjectField(OwnerFieldLabel, _currentOwner, typeof(TOwner), false)
                as TOwner;

            if (EditorGUI.EndChangeCheck())
            {
                _selectedImageIndex = 0;
                UpdateCurrentImage();
            }

            if (_currentOwner == null)
            {
                EditorGUILayout.HelpBox(
                    $"Select a {OwnerFieldLabel} to edit its images.",
                    MessageType.Info
                );
                return;
            }

            // Image selection dropdown
            var images = GetImagesFromOwner(_currentOwner);
            if (images != null && images.Length > 1)
            {
                EditorGUI.BeginChangeCheck();
                string[] imageNames = new string[images.Length];
                for (int i = 0; i < images.Length; i++)
                {
                    var image = images[i];
                    imageNames[i] =
                        image != null ? $"Image {i}: {image.Key}" : $"Image {i}: (null)";
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
            else if (images != null && images.Length == 1)
            {
                if (_currentImage == null)
                {
                    _currentImage = images[0];
                    RefreshPreview();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"This {OwnerFieldLabel} has no images. Add images first.",
                    MessageType.Warning
                );
                return;
            }

            if (_currentImage == null)
            {
                EditorGUILayout.HelpBox("Selected image is null.", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(10);

            // Horizontal layout for main content
            EditorGUILayout.BeginHorizontal();

            // Left panel - Controls
            EditorGUILayout.BeginVertical(GUILayout.Width(400));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawControlPanel();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right panel - Preview (constrained width)
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
            EditorGUILayout.LabelField("Image Stack", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newStack =
                EditorGUILayout.ObjectField(
                    "Image Stack",
                    _currentImage.ImageStack,
                    typeof(ImageStack),
                    false
                ) as ImageStack;

            if (EditorGUI.EndChangeCheck())
            {
                // Use public API instead of reflection
                _currentImage.SetImageStack(newStack);

                EditorUtility.SetDirty(_currentOwner);
                if (_autoRefresh)
                    RefreshPreview();
            }

            if (_currentImage.ImageStack == null)
            {
                EditorGUILayout.HelpBox(
                    "No ImageStack assigned. Assign one to see layers.",
                    MessageType.Warning
                );

                if (GUILayout.Button("+ Create New Image Stack"))
                {
                    // Create new ImageStack asset
                    var newImageStack = ScriptableObject.CreateInstance<ImageStack>();

                    // Save it to the project
                    string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                        "Create New Image Stack",
                        "NewImageStack",
                        "asset",
                        "Choose where to save the new ImageStack"
                    );

                    if (!string.IsNullOrEmpty(path))
                    {
                        UnityEditor.AssetDatabase.CreateAsset(newImageStack, path);
                        UnityEditor.AssetDatabase.SaveAssets();

                        // Assign it to the current image using public API
                        _currentImage.SetImageStack(newImageStack);

                        EditorUtility.SetDirty(_currentOwner);
                        if (_autoRefresh)
                            RefreshPreview();
                    }
                }
            }

            EditorGUILayout.Space(10);
        }

        protected void DrawOwnerSection()
        {
            EditorGUILayout.LabelField("Owner", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUILayout.ObjectField(
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
                    EditorUtility.SetDirty(_currentOwner);
                    if (_autoRefresh)
                        RefreshPreview();
                }
            }

            EditorGUILayout.Space(10);
        }

        protected void DrawTintingSection()
        {
            EditorGUILayout.LabelField("Tint Colors", EditorStyles.boldLabel);

            if (_currentImage.TintColors == null || _currentImage.TintColors.Length < 3)
            {
                EditorGUILayout.HelpBox("Tint colors are not initialized.", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < 3; i++)
            {
                _currentImage.TintColors[i] = EditorGUILayout.ColorField(
                    $"Tint Color {i + 1}",
                    _currentImage.TintColors[i]
                );
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentOwner);
                if (_autoRefresh)
                    RefreshPreview();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to White"))
            {
                for (int i = 0; i < 3; i++)
                {
                    _currentImage.TintColors[i] = Color.white;
                }
                EditorUtility.SetDirty(_currentOwner);
                if (_autoRefresh)
                    RefreshPreview();
            }

            if (GUILayout.Button("Update from Owner"))
            {
                _currentImage.UpdateTintColorsFromOwner();
                EditorUtility.SetDirty(_currentOwner);
                if (_autoRefresh)
                    RefreshPreview();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        protected void DrawLayerManagementSection()
        {
            EditorGUILayout.LabelField("Layer Management", EditorStyles.boldLabel);

            if (_currentImage.ImageStack == null)
            {
                EditorGUILayout.HelpBox("No ImageStack assigned.", MessageType.Info);
                return;
            }

            var layers = _currentImage.ImageStack.Layers;
            if (layers == null || layers.Count == 0)
            {
                EditorGUILayout.HelpBox("ImageStack has no layers.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Total Layers: {layers.Count}");

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer == null)
                    continue;

                EditorGUILayout.BeginVertical("box");

                bool isSelected = _selectedLayerIndex == i;
                Color originalColor = GUI.backgroundColor;
                if (isSelected)
                    GUI.backgroundColor = Color.cyan;

                if (GUILayout.Button($"Layer {i}: Order {layer.Order}"))
                {
                    _selectedLayerIndex = i;
                }

                GUI.backgroundColor = originalColor;

                if (isSelected)
                {
                    EditorGUI.indentLevel++;

                    // Safe access to sprite/mask names (handles destroyed Unity Objects)
                    string spriteName = "(none)";
                    if (layer.Sprite != null && layer.Sprite)
                        spriteName = layer.Sprite.name;

                    string maskName = "(none)";
                    if (layer.Mask != null && layer.Mask)
                        maskName = layer.Mask.name;

                    EditorGUILayout.LabelField("Sprite", spriteName);
                    EditorGUILayout.LabelField("Mask", maskName);
                    EditorGUILayout.LabelField("Order", layer.Order.ToString());
                    EditorGUILayout.LabelField("Offset", $"({layer.Offset.x}, {layer.Offset.y})");
                    EditorGUILayout.LabelField("Scale", layer.Scale.ToString());
                    EditorGUILayout.LabelField("Rotation", layer.Rotation.ToString());

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);
        }

        protected void DrawPreviewPanel()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Character Defaults"))
            {
                _currentOwner.SaveDefaults();
                EditorUtility.SetDirty(_currentOwner);
                // Confirmation dialog
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
                // Confirmation dialog
                EditorUtility.DisplayDialog(
                    "Defaults Loaded",
                    "Character defaults have been loaded.",
                    "OK"
                );
            }
            GUILayout.EndHorizontal();

            // Preview area - constrained to reasonable size
            if (_previewTexture != null)
            {
                // Calculate aspect-fit size for a max 300x300 display
                float maxSize = 300f;
                float aspect = (float)_previewTexture.width / _previewTexture.height;
                float displayWidth = maxSize;
                float displayHeight = maxSize;

                if (aspect > 1f)
                {
                    displayHeight = maxSize / aspect;
                }
                else if (aspect < 1f)
                {
                    displayWidth = maxSize * aspect;
                }

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

            if (_currentImage.SavedSprite == null) { }
            else
            {
                string spritePath =
                    $"Resources/GameContent/Graphics/Portraits/{_currentImage.Key}.png";
                EditorGUILayout.LabelField("Path", spritePath);
            }

            EditorGUILayout.Space(10);

            if (_currentImage.ImageStack == null)
            {
                EditorGUILayout.HelpBox(
                    "Cannot render: No ImageStack assigned.",
                    MessageType.Warning
                );
            }
            else
            {
                var layers = _currentImage.ImageStack.Layers;
                if (layers == null || layers.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "Cannot render: ImageStack has no layers.",
                        MessageType.Warning
                    );
                }
                else
                {
                    if (string.IsNullOrEmpty(_currentImage.Key))
                    {
                        EditorGUILayout.HelpBox(
                            "Warning: Key is empty. A key will be auto-generated.",
                            MessageType.Warning
                        );
                    }

                    if (GUILayout.Button("Render and Save to File", GUILayout.Height(40)))
                    {
                        Debug.Log($"Saving image with key: '{_currentImage.Key}'");
                        _currentImage.Render();
                        EditorUtility.DisplayDialog(
                            "Render Complete",
                            $"Image has been rendered and saved to:\nAssets/Resources/GameContent/Graphics/Portraits/{_currentImage.Key}.png",
                            "OK"
                        );
                        EditorUtility.SetDirty(_currentOwner);
                        AssetDatabase.SaveAssets();
                        RefreshPreview();
                    }
                }
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
                Debug.Log("RefreshPreview: _currentImage is null");
                _previewTexture = null;
                return;
            }

            if (_currentImage.ImageStack == null)
            {
                Debug.Log(
                    "RefreshPreview: ImageStack is null for image '" + (_currentImage?.Key) + "'"
                );
                _previewTexture = null;
                return;
            }

            Debug.Log(
                "RefreshPreview: Compositing image '"
                    + _currentImage.Key
                    + "' using ImageStack '"
                    + _currentImage.ImageStack.name
                    + "'"
            );
            _previewTexture = _currentImage.CompositeLayers();
            if (_previewTexture == null)
            {
                Debug.LogWarning(
                    "RefreshPreview: CompositeLayers returned null for '" + _currentImage.Key + "'"
                );

                // Fallback: if a saved sprite exists, use its texture for preview
                if (_currentImage.SavedSprite != null && _currentImage.SavedSprite.texture != null)
                {
                    Debug.Log("RefreshPreview: using SavedSprite.texture as fallback preview");
                    _previewTexture = _currentImage.SavedSprite.texture;
                }
            }
            Repaint();
        }
    }
}
