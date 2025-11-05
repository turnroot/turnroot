using Assets.Prototypes.Characters.Subclasses;
using UnityEditor;
using UnityEngine;

namespace Assets.Prototypes.Characters.Subclasses.Editor
{
    public class PortraitEditorWindow : EditorWindow
    {
        private Character _currentCharacter;
        private int _selectedPortraitIndex = 0;
        private Portrait _currentPortrait;
        private Vector2 _scrollPosition;
        private Texture2D _previewTexture;
        private bool _autoRefresh = true;
        private int _selectedLayerIndex = -1;

        [MenuItem("Window/Portrait Editor")]
        public static void ShowWindow()
        {
            GetWindow<PortraitEditorWindow>("Portrait Editor");
        }

        public static void OpenPortrait(Character character, int portraitIndex = 0)
        {
            var window = GetWindow<PortraitEditorWindow>("Portrait Editor");
            window._currentCharacter = character;
            window._selectedPortraitIndex = portraitIndex;
            if (
                character != null
                && character.Portraits != null
                && portraitIndex < character.Portraits.Length
            )
            {
                window._currentPortrait = character.Portraits[portraitIndex];
                window.RefreshPreview();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Live Portrait Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Character selection
            EditorGUI.BeginChangeCheck();
            _currentCharacter =
                EditorGUILayout.ObjectField(
                    "Character",
                    _currentCharacter,
                    typeof(Character),
                    false
                ) as Character;

            if (EditorGUI.EndChangeCheck())
            {
                _selectedPortraitIndex = 0;
                UpdateCurrentPortrait();
            }

            if (_currentCharacter == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a Character to edit its portraits.",
                    MessageType.Info
                );
                return;
            }

            // Portrait selection dropdown
            if (_currentCharacter.Portraits != null && _currentCharacter.Portraits.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                string[] portraitNames = new string[_currentCharacter.Portraits.Length];
                for (int i = 0; i < _currentCharacter.Portraits.Length; i++)
                {
                    var portrait = _currentCharacter.Portraits[i];
                    portraitNames[i] =
                        portrait != null
                            ? $"Portrait {i}: {portrait.Key}"
                            : $"Portrait {i}: (null)";
                }
                _selectedPortraitIndex = EditorGUILayout.Popup(
                    "Select Portrait",
                    _selectedPortraitIndex,
                    portraitNames
                );

                if (EditorGUI.EndChangeCheck())
                {
                    UpdateCurrentPortrait();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "This character has no portraits. Add portraits to the character first.",
                    MessageType.Warning
                );
                return;
            }

            if (_currentPortrait == null)
            {
                EditorGUILayout.HelpBox("Selected portrait is null.", MessageType.Warning);

                if (_currentCharacter != null && _currentCharacter.Portraits != null)
                {
                    EditorGUILayout.HelpBox(
                        $"The portrait array has {_currentCharacter.Portraits.Length} entries. "
                            + $"The portrait at index {_selectedPortraitIndex} is null. "
                            + "Click the button below to initialize this portrait.",
                        MessageType.Info
                    );

                    if (GUILayout.Button("Initialize This Portrait", GUILayout.Height(40)))
                    {
                        InitializePortraitAtIndex(_selectedPortraitIndex);
                    }
                }

                return;
            }

            EditorGUILayout.Space(5);

            // Auto-refresh toggle
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh Preview", _autoRefresh);

            if (GUILayout.Button("Manual Refresh Preview", GUILayout.Height(30)))
            {
                RefreshPreview();
            }

            EditorGUILayout.Space(10);

            // Split view: Preview on left, controls on right
            EditorGUILayout.BeginHorizontal();

            // Left side - Preview
            EditorGUILayout.BeginVertical(GUILayout.Width(280));
            DrawPreview();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Right side - Controls
            EditorGUILayout.BeginVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawControls();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (_previewTexture != null)
            {
                GUILayout.Label(_previewTexture, GUILayout.Width(256), GUILayout.Height(256));
            }
            else
            {
                // Draw a placeholder box
                Rect previewRect = GUILayoutUtility.GetRect(
                    256,
                    256,
                    GUILayout.Width(256),
                    GUILayout.Height(256)
                );
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 1f));

                GUIStyle centeredStyle = new GUIStyle(GUI.skin.label);
                centeredStyle.alignment = TextAnchor.MiddleCenter;
                centeredStyle.normal.textColor = Color.gray;
                GUI.Label(
                    previewRect,
                    "No Preview\n\nAdd layers with sprites\nthen click 'Manual Refresh'",
                    centeredStyle
                );
            }

            EditorGUILayout.Space(10);

            // Show saved sprite info
            EditorGUILayout.LabelField("Saved Sprite", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(
                "Saved Sprite Asset",
                _currentPortrait.SavedSprite,
                typeof(Sprite),
                false
            );
            EditorGUI.EndDisabledGroup();

            if (_currentPortrait.SavedSprite == null)
            {
                EditorGUILayout.HelpBox(
                    "No saved sprite loaded. Click 'Save Portrait' to create and save.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    "Path",
                    $"Resources/GameContent/Graphics/Portraits/{_currentPortrait.Key}.png",
                    EditorStyles.miniLabel
                );
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Save Portrait", GUILayout.Height(35)))
            {
                if (_currentPortrait == null)
                {
                    EditorUtility.DisplayDialog("Error", "No portrait selected.", "OK");
                    return;
                }

                if (_currentPortrait.ImageStack == null)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "Portrait has no ImageStack assigned.",
                        "OK"
                    );
                    return;
                }

                var layers = _currentPortrait.ImageStack.Layers;
                if (layers == null || layers.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "ImageStack has no layers.", "OK");
                    return;
                }

                bool hasSprites = false;
                foreach (var layer in layers)
                {
                    if (layer != null && layer.Sprite != null)
                    {
                        hasSprites = true;
                        break;
                    }
                }

                if (!hasSprites)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "No layers have sprites assigned. Add sprites to layers before saving.",
                        "OK"
                    );
                    return;
                }

                // Validate key
                if (string.IsNullOrEmpty(_currentPortrait.Key))
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "Portrait key is empty! Set a key in the 'Key (filename)' field before saving.",
                        "OK"
                    );
                    return;
                }

                try
                {
                    Debug.Log($"Saving portrait with key: '{_currentPortrait.Key}'");
                    _currentPortrait.Render();
                    EditorUtility.DisplayDialog(
                        "Portrait Saved",
                        $"Portrait has been rendered and saved to:\nAssets/Resources/GameContent/Graphics/Portraits/{_currentPortrait.Key}.png",
                        "OK"
                    );
                }
                catch (System.Exception ex)
                {
                    EditorUtility.DisplayDialog(
                        "Save Failed",
                        $"Failed to save portrait: {ex.Message}\n\nCheck the console for details.",
                        "OK"
                    );
                    Debug.LogError($"Portrait save failed: {ex}");
                }
            }
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("Portrait Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Debug info
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Portrait ID", _currentPortrait.Id.ToString());
            EditorGUI.EndDisabledGroup();

            // Key field (for filename)
            EditorGUI.BeginChangeCheck();
            string newKey = EditorGUILayout.TextField("Key (filename)", _currentPortrait.Key);
            if (EditorGUI.EndChangeCheck())
            {
                _currentPortrait.SetKey(newKey);
                EditorUtility.SetDirty(_currentCharacter);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate New Key", GUILayout.Width(150)))
            {
                _currentPortrait.SetKey(
                    $"{_currentCharacter.name}_portrait_{System.Guid.NewGuid().ToString().Substring(0, 8)}"
                );
                EditorUtility.SetDirty(_currentCharacter);
                Repaint();
            }
            if (GUILayout.Button("Generate GUID Key", GUILayout.Width(150)))
            {
                _currentPortrait.SetKey(null); // This will trigger auto-generation with full GUID
                EditorUtility.SetDirty(_currentCharacter);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(_currentPortrait.Key))
            {
                EditorGUILayout.HelpBox(
                    "WARNING: Key is empty! This will cause save errors.",
                    MessageType.Error
                );
            }

            EditorGUILayout.Space(5);

            // ImageStack assignment
            EditorGUILayout.LabelField("Image Stack", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newImageStack =
                EditorGUILayout.ObjectField(
                    "Image Stack",
                    _currentPortrait.ImageStack,
                    typeof(Assets.Prototypes.Graphics.Portrait.ImageStack),
                    false
                ) as Assets.Prototypes.Graphics.Portrait.ImageStack;

            if (EditorGUI.EndChangeCheck())
            {
                // Use reflection to set the ImageStack since it's read-only
                var imageStackField = typeof(Portrait).GetField(
                    "_imageStack",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );
                if (imageStackField != null)
                {
                    imageStackField.SetValue(_currentPortrait, newImageStack);
                    EditorUtility.SetDirty(_currentCharacter);
                    RefreshPreview();
                    Repaint();
                }
            }

            if (_currentPortrait.ImageStack == null)
            {
                EditorGUILayout.HelpBox(
                    "No ImageStack assigned. Create one or assign an existing ImageStack asset.",
                    MessageType.Warning
                );

                if (GUILayout.Button("Create New ImageStack"))
                {
                    CreateNewImageStack();
                }
            }

            EditorGUILayout.Space(10);

            // Owner info
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Owner", _currentPortrait.Owner, typeof(Character), false);
            EditorGUI.EndDisabledGroup();

            if (_currentPortrait.Owner == null)
            {
                EditorGUILayout.HelpBox(
                    "WARNING: Portrait has no owner assigned. The owner is needed to get accent colors. This may need to be set manually in the Character's Portrait array.",
                    MessageType.Warning
                );

                if (GUILayout.Button("Assign Current Character as Owner"))
                {
                    _currentPortrait.SetOwner(_currentCharacter);
                    EditorUtility.SetDirty(_currentCharacter);
                    Repaint();
                    Debug.Log($"Assigned {_currentCharacter.name} as owner of portrait.");
                }
            }

            EditorGUILayout.Space(10);

            // Tint colors
            EditorGUILayout.LabelField("Tint Colors (from Character)", EditorStyles.boldLabel);

            // Force initialization if needed
            if (_currentPortrait.TintColors == null || _currentPortrait.TintColors.Length < 3)
            {
                EditorGUILayout.HelpBox(
                    "Tint colors array is not properly initialized. Click 'Update from Character Colors' to initialize.",
                    MessageType.Warning
                );
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                Color[] tints = _currentPortrait.TintColors;
                tints[0] = EditorGUILayout.ColorField("Tint 1 (Red Channel)", tints[0]);
                tints[1] = EditorGUILayout.ColorField("Tint 2 (Green Channel)", tints[1]);
                tints[2] = EditorGUILayout.ColorField("Tint 3 (Blue Channel)", tints[2]);

                if (EditorGUI.EndChangeCheck())
                {
                    // Mark the character asset as dirty so changes are saved
                    EditorUtility.SetDirty(_currentCharacter);

                    if (_autoRefresh)
                    {
                        RefreshPreview();
                    }
                }
            }

            if (GUILayout.Button("Update from Character Colors"))
            {
                if (_currentPortrait.Owner == null)
                {
                    EditorUtility.DisplayDialog(
                        "No Owner",
                        "This portrait has no owner character assigned. Cannot update colors.",
                        "OK"
                    );
                }
                else
                {
                    _currentPortrait.UpdateTintColorsFromOwner();
                    EditorUtility.SetDirty(_currentCharacter);
                    RefreshPreview();
                    Repaint();

                    Debug.Log($"Updated tint colors from character: {_currentPortrait.Owner.name}");
                    Debug.Log(
                        $"Color 1: {_currentPortrait.TintColors[0]}, Color 2: {_currentPortrait.TintColors[1]}, Color 3: {_currentPortrait.TintColors[2]}"
                    );
                }
            }

            EditorGUILayout.Space(15);

            // Layers
            EditorGUILayout.LabelField("Image Stack Layers", EditorStyles.boldLabel);

            if (_currentPortrait.ImageStack == null)
            {
                EditorGUILayout.HelpBox(
                    "No ImageStack assigned. Assign an ImageStack above to add layers.",
                    MessageType.Info
                );
                return;
            }

            var layers = _currentPortrait.ImageStack.Layers;
            if (layers == null || layers.Count == 0)
            {
                EditorGUILayout.HelpBox("No layers in ImageStack.", MessageType.Info);
                EditorGUILayout.Space(5);

                if (GUILayout.Button("+ Add Layer", GUILayout.Height(30)))
                {
                    AddNewLayer();
                }
            }
            else
            {
                // Add layer button at the top
                if (GUILayout.Button("+ Add Layer", GUILayout.Height(30)))
                {
                    AddNewLayer();
                }

                EditorGUILayout.Space(10);

                for (int i = 0; i < layers.Count; i++)
                {
                    DrawLayerControl(i, layers[i]);
                }
            }
        }

        private void DrawLayerControl(int index, ImageStackLayer layer)
        {
            bool isSelected = _selectedLayerIndex == index;
            Color bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.3f) : Color.clear;

            var style = new GUIStyle(EditorStyles.helpBox);
            if (isSelected)
            {
                style.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.3f));
            }

            EditorGUILayout.BeginVertical(style);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Layer {index} (Order: {layer.Order})",
                EditorStyles.boldLabel
            );

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                _selectedLayerIndex = isSelected ? -1 : index;
            }

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Delete Layer",
                        $"Are you sure you want to delete Layer {index}?",
                        "Delete",
                        "Cancel"
                    )
                )
                {
                    DeleteLayer(index);
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            // Sprite
            layer.Sprite =
                EditorGUILayout.ObjectField("Sprite", layer.Sprite, typeof(Sprite), false)
                as Sprite;

            // Mask
            layer.Mask =
                EditorGUILayout.ObjectField("Mask", layer.Mask, typeof(Sprite), false) as Sprite;

            // Offset with draggable sliders
            EditorGUILayout.LabelField("Offset (Drag to move)", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            layer.Offset.x = EditorGUILayout.Slider("X", layer.Offset.x, -512, 512);
            layer.Offset.y = EditorGUILayout.Slider("Y", layer.Offset.y, -512, 512);
            EditorGUILayout.EndHorizontal();

            // Scale
            layer.Scale = EditorGUILayout.Slider("Scale", layer.Scale, 0.1f, 5f);

            // Order
            layer.Order = EditorGUILayout.IntField("Render Order", layer.Order);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentPortrait.ImageStack);
                if (_autoRefresh)
                {
                    RefreshPreview();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void UpdateCurrentPortrait()
        {
            if (
                _currentCharacter != null
                && _currentCharacter.Portraits != null
                && _selectedPortraitIndex >= 0
                && _selectedPortraitIndex < _currentCharacter.Portraits.Length
            )
            {
                _currentPortrait = _currentCharacter.Portraits[_selectedPortraitIndex];

                // Debug info
                if (_currentPortrait != null)
                {
                    Debug.Log(
                        $"Switched to portrait {_selectedPortraitIndex}: Key='{_currentPortrait.Key}', ImageStack={(_currentPortrait.ImageStack != null ? _currentPortrait.ImageStack.name : "null")}"
                    );
                }

                RefreshPreview();
                Repaint(); // Force UI update
            }
            else
            {
                _currentPortrait = null;
                _previewTexture = null;
                Repaint();
            }
        }

        private void RefreshPreview()
        {
            if (_currentPortrait == null || _currentPortrait.ImageStack == null)
            {
                Debug.LogWarning("Cannot refresh preview: Portrait or ImageStack is null");
                return;
            }

            var layers = _currentPortrait.ImageStack.Layers;
            if (layers == null || layers.Count == 0)
            {
                Debug.LogWarning("Cannot refresh preview: No layers in ImageStack");
                _previewTexture = null;
                Repaint();
                return;
            }

            // Check if any layers have sprites
            int spriteLayers = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].Sprite != null)
                {
                    spriteLayers++;
                    Debug.Log(
                        $"Layer {i}: Has sprite '{layers[i].Sprite.name}', Order: {layers[i].Order}"
                    );
                }
            }

            if (spriteLayers == 0)
            {
                Debug.LogWarning("Cannot refresh preview: No layers have sprites assigned");
                _previewTexture = null;
                Repaint();
                return;
            }

            Debug.Log($"Compositing {spriteLayers} layers with sprites...");
            Texture2D composited = _currentPortrait.CompositeLayers();
            if (composited != null)
            {
                _previewTexture = composited;
                Debug.Log(
                    $"Preview updated successfully. Texture size: {composited.width}x{composited.height}"
                );
                Repaint();
            }
            else
            {
                Debug.LogError("CompositeLayers returned null");
                _previewTexture = null;
                Repaint();
            }
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void CreateNewImageStack()
        {
            // Create the ImageStack asset
            var imageStack =
                ScriptableObject.CreateInstance<Assets.Prototypes.Graphics.Portrait.ImageStack>();

            // Save it to the project
            string path = EditorUtility.SaveFilePanelInProject(
                "Create ImageStack",
                $"ImageStack_{_currentCharacter.name}_{_selectedPortraitIndex}",
                "asset",
                "Create a new ImageStack asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(imageStack, path);
                AssetDatabase.SaveAssets();

                // Assign it to the portrait (this requires reflection since Portrait is a class)
                var portraitField = typeof(Portrait).GetField(
                    "_imageStack",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );
                if (portraitField != null)
                {
                    portraitField.SetValue(_currentPortrait, imageStack);
                    EditorUtility.SetDirty(_currentCharacter);
                    AssetDatabase.SaveAssets();
                }

                RefreshPreview();
                Repaint();
            }
        }

        private void AddNewLayer()
        {
            if (_currentPortrait == null || _currentPortrait.ImageStack == null)
                return;

            var newLayer = new ImageStackLayer();
            _currentPortrait.ImageStack.Layers.Add(newLayer);
            EditorUtility.SetDirty(_currentPortrait.ImageStack);

            if (_autoRefresh)
            {
                RefreshPreview();
            }

            Repaint();
        }

        private void DeleteLayer(int index)
        {
            if (_currentPortrait == null || _currentPortrait.ImageStack == null)
                return;

            if (index >= 0 && index < _currentPortrait.ImageStack.Layers.Count)
            {
                _currentPortrait.ImageStack.Layers.RemoveAt(index);
                EditorUtility.SetDirty(_currentPortrait.ImageStack);

                // Reset selected layer if it was the deleted one or after it
                if (_selectedLayerIndex >= index)
                {
                    _selectedLayerIndex = -1;
                }

                if (_autoRefresh)
                {
                    RefreshPreview();
                }

                Repaint();
            }
        }

        private void InitializePortraitAtIndex(int index)
        {
            if (_currentCharacter == null || _currentCharacter.Portraits == null)
                return;

            if (index < 0 || index >= _currentCharacter.Portraits.Length)
                return;

            // Create a new Portrait instance
            var newPortrait = new Portrait();
            newPortrait.SetOwner(_currentCharacter);

            // Assign it to the array
            _currentCharacter.Portraits[index] = newPortrait;

            // Mark as dirty
            EditorUtility.SetDirty(_currentCharacter);

            // Update the current portrait reference
            _currentPortrait = newPortrait;

            Debug.Log($"Initialized portrait at index {index} with key: {newPortrait.Key}");

            Repaint();
        }
    }
}
