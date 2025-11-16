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

        protected virtual void SetImagesToOwner(TOwner owner, TStackedImage[] images) { }

        protected virtual void OnGUI()
        {
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
                return;
            if (_currentImage == null)
            {
                EditorGUILayout.HelpBox("Selected image is null.", MessageType.Error);
                return;
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
                    imageNames[i] =
                        images[i] != null ? $"Image {i}: {images[i].Key}" : $"Image {i}: (null)";

                _selectedImageIndex = EditorGUILayout.Popup(
                    "Select Image",
                    _selectedImageIndex,
                    imageNames
                );
                if (EditorGUI.EndChangeCheck())
                    UpdateCurrentImage();
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
                EditorGUILayout.HelpBox(
                    "Warning: Key is empty. It will be auto-generated when rendering.",
                    MessageType.Warning
                );
            EditorGUILayout.Space(10);
        }

        protected void DrawImageStackSection()
        {
            if (_currentImage.ImageStack == null)
            {
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
                    _currentImage.SetImageStack(newStack);
                    MarkDirtyAndRefresh();
                }
            }

            if (_currentImage.ImageStack == null)
            {
                EditorGUILayout.HelpBox(
                    "No ImageStack assigned. Assign one to see layers.",
                    MessageType.Warning
                );
                if (GUILayout.Button("+ Create New Image Stack"))
                    CreateNewImageStack();
            }

            EditorGUILayout.Space(10);
        }

        private void CreateNewImageStack()
        {
            var newImageStack = ScriptableObject.CreateInstance<ImageStack>();
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Image Stack",
                "NewImageStack",
                "asset",
                "Choose where to save the new ImageStack"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(newImageStack, path);
                AssetDatabase.SaveAssets();
                _currentImage.SetImageStack(newImageStack);
                MarkDirtyAndRefresh();
            }
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

            if (_currentImage.TintColors == null || _currentImage.TintColors.Length < 3)
            {
                EditorGUILayout.HelpBox("Tint colors are not initialized.", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 3; i++)
                _currentImage.TintColors[i] = EditorGUILayout.ColorField(
                    $"Tint Color {i + 1}",
                    _currentImage.TintColors[i]
                );
            if (EditorGUI.EndChangeCheck())
                MarkDirtyAndRefresh();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to White"))
            {
                for (int i = 0; i < 3; i++)
                    _currentImage.TintColors[i] = Color.white;
                MarkDirtyAndRefresh();
            }
            if (GUILayout.Button("Update from Owner"))
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
                DrawLayerButton(i, layer);
                if (_selectedLayerIndex == i)
                    DrawLayerDetails(layer);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);
        }

        private void DrawLayerButton(int index, ImageStackLayer layer)
        {
            bool isSelected = _selectedLayerIndex == index;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button($"Layer {index}: Order {layer.Order}"))
                _selectedLayerIndex = index;

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
            EditorGUILayout.LabelField("Rotation", layer.Rotation.ToString());

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
            if (_currentImage.ImageStack == null)
            {
                EditorGUILayout.HelpBox(
                    "Cannot render: No ImageStack assigned.",
                    MessageType.Warning
                );
                return;
            }

            var layers = _currentImage.ImageStack.Layers;
            if (layers == null || layers.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Cannot render: ImageStack has no layers.",
                    MessageType.Warning
                );
                return;
            }

            if (string.IsNullOrEmpty(_currentImage.Key))
                EditorGUILayout.HelpBox(
                    "Warning: Key is empty. A key will be auto-generated.",
                    MessageType.Warning
                );

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
                Debug.Log($"RefreshPreview: ImageStack is null for image '{_currentImage?.Key}'");
                _previewTexture = null;
                return;
            }

            Debug.Log(
                $"RefreshPreview: Compositing image '{_currentImage.Key}' using ImageStack '{_currentImage.ImageStack.name}'"
            );
            _previewTexture = _currentImage.CompositeLayers();

            if (_previewTexture == null)
            {
                Debug.LogWarning(
                    $"RefreshPreview: CompositeLayers returned null for '{_currentImage.Key}'"
                );
                if (_currentImage.SavedSprite?.texture != null)
                {
                    Debug.Log("RefreshPreview: using SavedSprite.texture as fallback preview");
                    _previewTexture = _currentImage.SavedSprite.texture;
                }
            }

            Repaint();
        }

        private void MarkDirtyAndRefresh()
        {
            EditorUtility.SetDirty(_currentOwner);
            if (_autoRefresh)
                RefreshPreview();
        }
    }
}
