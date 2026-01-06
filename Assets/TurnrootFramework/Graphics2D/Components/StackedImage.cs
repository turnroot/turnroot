using System;
using System.Collections.Generic;
using System.Linq;
using Assets.AbstractScripts.Graphics2D;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Graphics2D.Tags;
using UnityEngine;

namespace Turnroot.Graphics2D
{
    /// <summary>
    /// A stacked image is a group of ImageLayers
    /// with tint and layer order. It is rendered
    /// to a single sprite.
    /// Each concrete subclass can provide specific validation and ordering rules.
    /// </summary>
    [Serializable]
    public abstract class StackedImage<TOwner>
        where TOwner : UnityEngine.Object
    {
        [SerializeField]
        protected TOwner _owner;

        // Direct ownership of layers (new model)
        [SerializeField]
        private List<ImageStackLayer> _layers = new();

        [SerializeField, HideInInspector]
        private string _key;

        [NonSerialized]
        private Sprite _runtimeSprite;

        [SerializeField, HideInInspector]
        private Sprite _savedSprite;

        [SerializeField, HideInInspector]
        private string _idString;

        [SerializeField, HideInInspector]
        protected Color[] _tintColors = new Color[3] { Color.white, Color.white, Color.white };

        // Validation status for editor UI
        [NonSerialized]
        private string _validationMessage = "";

        [NonSerialized]
        private bool _hasValidationError = false;

        private Guid _id;

        // Editor-only fallback for Graphics2DSettings to avoid creating multiple temporary instances
#if UNITY_EDITOR
        [System.NonSerialized]
        private static Graphics2DSettings _editorFallbackGraphics2DSettings;
#endif

        public TOwner Owner => _owner;

        // Layers: direct ownership.
        public List<ImageStackLayer> Layers
        {
            get
            {
                EnsureMandatoryLayers();
                return _layers;
            }
        }

        public string Key => _key;

        public Sprite RuntimeSprite => _runtimeSprite;
        public Sprite SavedSprite => _savedSprite;
        public Guid Id => _id;
        public Color[] TintColors => _tintColors;

        // Editor UI validation status
        public string ValidationMessage => _validationMessage;
        public bool HasValidationError => _hasValidationError;

        public void SetOwner(TOwner owner)
        {
            _owner = owner;
            UpdateTintColorsFromOwner();
        }

        public void SetKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _key = key;
#if UNITY_EDITOR
                Debug.Log($"StackedImage key set to: {_key}");
#endif
            }
            else
            {
                // If empty/null key is passed, generate a new one
                EnsureKeyInitialized();
#if UNITY_EDITOR
                Debug.Log($"Generated new stackedImage key: {_key}");
#endif
            }
        }

        public void ClearValidationStatus()
        {
            _validationMessage = "";
            _hasValidationError = false;
        }

        private void SetValidationError(string message)
        {
            _validationMessage = message;
            _hasValidationError = true;
#if UNITY_EDITOR
            Debug.LogError(message);
#endif
        }

        private void EnsureKeyInitialized()
        {
            if (_id == Guid.Empty)
            {
                _id = Guid.NewGuid();
                _idString = _id.ToString();
            }
            _key = $"stackedImage_{_id}";
        }

        public StackedImage()
        {
            _id = Guid.NewGuid();
            _idString = _id.ToString();
            EnsureKeyInitialized();
        }

        // Called by Unity after deserialization
        private void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(_idString))
            {
                _id = Guid.Parse(_idString);
            }
            else
            {
                _id = Guid.NewGuid();
                _idString = _id.ToString();
            }

            // Auto-generate key if it's empty
            if (string.IsNullOrEmpty(_key))
            {
                EnsureKeyInitialized();
#if UNITY_EDITOR
                Debug.Log($"Generated new stackedImage key: {_key}");
#endif
            }

            // Initialize tint colors array if null
            if (_tintColors == null || _tintColors.Length < 3)
            {
                _tintColors = new Color[3] { Color.white, Color.white, Color.white };
            }

            // Update tint colors from owner character
            UpdateTintColorsFromOwner();
        }

        public abstract void UpdateTintColorsFromOwner();

        // Subclasses can provide their mandatory tags and ordering behavior
        protected virtual IEnumerable<ILayerTag> MandatoryTags() => Enumerable.Empty<ILayerTag>();

        protected virtual int GetLayerOrder(ImageStackLayer layer) => layer.Order;

        protected virtual ImageStackLayer CreateLayerForTag(ILayerTag tag)
        {
            var l = new UnmaskedImageStackLayer();
            l.Tag = tag.Name;
            l.Order = tag.Order;
            l.Sprite = null;
            l.Tint = Color.white;
            return l;
        }

        private void EnsureMandatoryLayers()
        {
            var mandatory = MandatoryTags();
            if (mandatory == null)
            {
                return;
            }

            foreach (var tag in mandatory)
            {
                if (tag == null)
                {
                    continue;
                }

                if (
                    _layers.Any(l =>
                        string.Equals(l.Tag, tag.Name, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    continue;
                }

                _layers.Add(CreateLayerForTag(tag));
            }

            SortLayers();
        }

        private void SortLayers() =>
            _layers.Sort((a, b) => GetLayerOrder(a).CompareTo(GetLayerOrder(b)));

        // Subclasses must provide the subdirectory name for saving files
        // e.g., "Portraits" for Portrait class, "ItemIcons" for ItemIcon class
        protected abstract string GetSaveSubdirectory();

        public override string ToString() => $"p{_id}";

        public string Identify()
        {
            string ownerName = _owner != null ? _owner.name : "null";
            return $"StackedImage(ID: {_id}, Owner: {ownerName}, Key: {_key})";
        }

        public void Render()
        {
            // Validate that we have layers to render
            if (Layers == null || Layers.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Cannot render stackedImage: No layers present.");
#endif
                return;
            }

            // Ensure key is valid
            if (string.IsNullOrEmpty(_key))
            {
                EnsureKeyInitialized();
#if UNITY_EDITOR
                Debug.LogWarning($"StackedImage key was empty, generated new key: {_key}");
#endif
            }

#if UNITY_EDITOR
            Debug.Log($"Rendering stackedImage with key: {_key}");
#endif

            // Use compositor to create the final texture
            Texture2D composited = CompositeLayers();
            if (composited == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Failed to composite layers.");
#endif
                return;
            }

            // Create sprite from composited texture
            _runtimeSprite = Sprite.Create(
                composited,
                new Rect(0, 0, composited.width, composited.height),
                new Vector2(0.5f, 0.5f)
            );

            // Save to file
            SaveToFile(composited);

            // Load the saved sprite asset and assign it
            LoadSavedSprite();
        }

        public Texture2D CompositeLayers()
        {
            if (Layers == null || Layers.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("CompositeLayers: no layers available to composite.");
#endif
                return null;
            }

            // Ensure key is set before compositing
            if (string.IsNullOrEmpty(_key))
            {
                EnsureKeyInitialized();
            }

            // Load Graphics2DSettings using the central GameSettingsLoader which
            // searches Resources/GameSettings/* and falls back to editor search.
            Graphics2DSettings settings = null;
            try
            {
                settings = Turnroot.Utilities.GameSettingsLoader.LoadFirst<Graphics2DSettings>(
                    "GameSettings"
                );
            }
            catch { }

            if (settings == null)
            {
                Debug.LogError(
                    "Graphics2DSettings not found in Resources/GameSettings (expected under Resources/GameSettings/*). Using default 512x512."
                );

                // Create a single cached fallback instance so we don't create multiple in-memory duplicates
                // which cause duplicate singleton OnEnable logs and confusion in the editor.
                if (_editorFallbackGraphics2DSettings == null)
                {
                    _editorFallbackGraphics2DSettings =
                        ScriptableObject.CreateInstance<Graphics2DSettings>();
                }
                settings = _editorFallbackGraphics2DSettings;
            }

            int width = settings.portraitRenderWidth;
            int height = settings.portraitRenderHeight;

            // Create base texture with transparent pixels
            Texture2D baseTexture = new(width, height, TextureFormat.RGBA32, false);
            Color[] clearPixels = new Color[width * height];
            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = new Color(0, 0, 0, 0);
            }
            baseTexture.SetPixels(clearPixels);
            baseTexture.Apply();

            // Composite the layers using static method
            ImageStackLayer[] originalLayers = Layers.ToArray();

            // Defensive: create a temporary normalized copy of layers so we don't
            // mutate the stored asset if any layer has an invalid Scale (e.g. 0).
            // This prevents blank previews when existing ImageStack assets contain
            // layers with Scale <= 0.
            ImageStackLayer[] layers = new ImageStackLayer[originalLayers.Length];
            bool correctedScale = false;
            for (int i = 0; i < originalLayers.Length; i++)
            {
                var src = originalLayers[i];
                if (src == null)
                {
                    layers[i] = null;
                    continue;
                }

                // Create a shallow copy for safe editing (do not change asset)
                var copy = new ImageStackLayer
                {
                    Sprite = src.Sprite,
                    Mask = src.Mask,
                    Offset = src.Offset,
                    Scale = src.Scale,
                    Rotation = src.Rotation,
                    Order = src.Order,
                };
                // Preserve Tag if present on source
                try
                {
                    copy.Tag = src.Tag;
                }
                catch { }
                // Preserve per-layer Tint if the source has one (e.g., UnmaskedImageStackLayer)
                try
                {
                    var srcType = src.GetType();
                    var tintField = srcType.GetField("Tint");
                    if (tintField != null)
                    {
                        var tintVal = tintField.GetValue(src);
                        if (tintVal is Color c)
                        {
                            // Use reflection to set Tint on the copy if available
                            var copyType = copy.GetType();
                            var copyTintField = copyType.GetField("Tint");
                            copyTintField?.SetValue(copy, c);
                        }
                    }
                }
                catch
                {
                    // ignore any reflection issues
                }

                // Normalize invalid scale values to 1.0 to avoid zero-sized compositing
                if (copy.Scale <= 0f)
                {
                    copy.Scale = 1f;
                    correctedScale = true;
                }

                layers[i] = copy;
            }

            if (correctedScale)
            {
                Debug.LogWarning(
                    $"CompositeLayers: One or more layers in image '{_key}' had non-positive Scale and were temporarily normalized to 1.0 for preview/compositing. Consider fixing the layer data."
                );
            }

            // Extract masks from the (normalized) layers array
            Sprite[] masks = new Sprite[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                masks[i] = layers[i]?.Mask;
            }

            Texture2D result = ImageCompositor.CompositeImageStackLayers(
                baseTexture,
                layers,
                masks,
                _tintColors
            );

            return result;
        }

        private void SaveToFile(Texture2D texture)
        {
            ClearValidationStatus();

            // Find GamePackageSettings to determine the correct save location
            var gamePackageSettings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<Turnroot.GamePackage.GamePackageSettings>();
            if (gamePackageSettings == null)
            {
                SetValidationError(
                    "Portrait rendering failed: Could not find GamePackageSettings in Resources. Please create a GamePackageSettings asset in your project."
                );
                return;
            }

#if UNITY_EDITOR
            // Get the asset path of the GamePackageSettings to determine the project structure
            string gamePackageSettingsPath = UnityEditor.AssetDatabase.GetAssetPath(
                gamePackageSettings
            );
            if (string.IsNullOrEmpty(gamePackageSettingsPath))
            {
                SetValidationError(
                    "Portrait rendering failed: Could not determine GamePackageSettings location."
                );
                return;
            }

            // Extract the base path (e.g., "Assets/Demos/Resources/GameSettings" -> "Assets/Demos/Resources")
            string resourcesPath = gamePackageSettingsPath.Substring(
                0,
                gamePackageSettingsPath.LastIndexOf("/GameSettings")
            );
            if (!resourcesPath.EndsWith("/Resources"))
            {
                SetValidationError(
                    $"Portrait rendering failed: GamePackageSettings is not in a Resources folder. Expected path ending with '/Resources/GameSettings/', but found: {gamePackageSettingsPath}"
                );
                return;
            }

            // Build the portrait save path: {ResourcesPath}/Components/Characters/Portraits
            string portraitSavePath = System.IO.Path.Combine(
                resourcesPath.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()),
                "Components",
                "Characters",
                "Portraits"
            );
#endif

            string fileName = $"{_key}.png";
            string fullPath = System.IO.Path.Combine(portraitSavePath, fileName);

            // Create directory if it doesn't exist
            if (!System.IO.Directory.Exists(portraitSavePath))
            {
                System.IO.Directory.CreateDirectory(portraitSavePath);
#if UNITY_EDITOR
                Debug.Log($"Created directory: {portraitSavePath}");
#endif
            }

            // Save the texture as PNG
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, pngData);
#if UNITY_EDITOR
            Debug.Log($"Successfully saved portrait texture: {fileName} to {fullPath}");
#endif

#if UNITY_EDITOR
            // Refresh the asset database so Unity sees the new file
            UnityEditor.AssetDatabase.Refresh();

            // Force Unity to import the asset immediately
            string assetImportPath = fullPath
                .Replace(Application.dataPath, "Assets")
                .Replace("\\", "/");
            UnityEditor.AssetDatabase.ImportAsset(assetImportPath);
#endif
        }

        private void LoadSavedSprite()
        {
            ClearValidationStatus();

            // Find GamePackageSettings to determine the correct load location
            var gamePackageSettings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<Turnroot.GamePackage.GamePackageSettings>();
            if (gamePackageSettings == null)
            {
                SetValidationError(
                    "Portrait loading failed: Could not find GamePackageSettings in Resources."
                );
                return;
            }

#if UNITY_EDITOR
            // Wait for asset database to finish importing
            UnityEditor.AssetDatabase.Refresh();

            // Get the asset path of the GamePackageSettings to determine the project structure
            string gamePackageSettingsPath = UnityEditor.AssetDatabase.GetAssetPath(
                gamePackageSettings
            );
            if (string.IsNullOrEmpty(gamePackageSettingsPath))
            {
                SetValidationError(
                    "Portrait loading failed: Could not determine GamePackageSettings location."
                );
                return;
            }

            // Extract the base path (e.g., "Assets/Demos/Resources/GameSettings" -> "Assets/Demos/Resources")
            string resourcesPath = gamePackageSettingsPath.Substring(
                0,
                gamePackageSettingsPath.LastIndexOf("/GameSettings")
            );
            if (!resourcesPath.EndsWith("/Resources"))
            {
                SetValidationError(
                    $"Portrait loading failed: GamePackageSettings is not in a Resources folder. Expected path ending with '/Resources/GameSettings/', but found: {gamePackageSettingsPath}"
                );
                return;
            }

            // Build the portrait load path: {ResourcesPath}/Components/Characters/Portraits
            string assetPath = $"{resourcesPath}/Components/Characters/Portraits/{_key}.png";

            Debug.Log($"Attempting to load sprite from: {assetPath}");

            // Import the texture with sprite settings
            UnityEditor.TextureImporter importer =
                UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;

            if (importer == null)
            {
                Debug.LogError($"Could not get TextureImporter for: {assetPath}");
                return;
            }

            // Set texture to sprite mode
            if (importer.textureType != UnityEditor.TextureImporterType.Sprite)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;

                // Save and reimport
                UnityEditor.AssetDatabase.ImportAsset(
                    assetPath,
                    UnityEditor.ImportAssetOptions.ForceUpdate
                );
                UnityEditor.AssetDatabase.SaveAssets();

                Debug.Log($"Configured texture as sprite: {assetPath}");
            }

            // Load the sprite
            _savedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (_savedSprite != null)
            {
                Debug.Log($"Successfully loaded saved sprite: {_savedSprite.name}");

                if (_owner != null)
                {
                    var ownerPath = UnityEditor.AssetDatabase.GetAssetPath(
                        _owner as UnityEngine.Object
                    );
                    if (!string.IsNullOrEmpty(ownerPath))
                    {
                        UnityEditor.EditorUtility.SetDirty(_owner);
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                }
            }
            else
            {
                Debug.LogError($"Failed to load sprite from: {assetPath}");
            }
#endif
        }
    }
}
