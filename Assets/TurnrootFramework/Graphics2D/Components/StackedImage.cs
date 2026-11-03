using System;
using System.Collections.Generic;
using System.Linq;
using Assets.AbstractScripts.Graphics2D;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Graphics2D.Tags;
using Turnroot.Utilities;
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
        // kept at runtime too so composite logic can compile; only populated in editor.
        [NonSerialized]
        private static Graphics2DSettings _editorFallbackGraphics2DSettings;

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

        protected void SetRuntimeSprite(Sprite sprite) => _runtimeSprite = sprite;

        protected void SetSavedSprite(Sprite sprite) => _savedSprite = sprite;

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
            }
            else
            {
                // If empty/null key is passed, generate a new one
                EnsureKeyInitialized();
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

            message.LogError("StackedImage.SetValidationError");
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
            var l = new UnmaskedImageStackLayer
            {
                Tag = tag.Name,
                Order = tag.Order,
                Sprite = null,
                Tint = Color.white
            };
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

        public virtual void Render()
        {
            // Validate that we have layers to render
            if (Layers == null || Layers.Count == 0)
            {
                "Cannot render stackedImage: No layers present.".LogWarning();
                return;
            }

            // Ensure key is valid
            if (string.IsNullOrEmpty(_key))
            {
                EnsureKeyInitialized();
                $"StackedImage key was empty, generated new key: {_key}".LogWarning();
            }

            // Use compositor to create the final texture
            Texture2D composited = CompositeLayers();
            if (composited == null)
            {
                "Failed to composite layers.".LogError("StackedImage.Render");
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
                "CompositeLayers: no layers available to composite.".LogWarning();
                return null;
            }

            // Ensure key is set before compositing
            if (string.IsNullOrEmpty(_key))
            {
                EnsureKeyInitialized();
            }

            Graphics2DSettings settings = GetGraphics2DSettings();
            int width = settings.portraitRenderWidth;
            int height = settings.portraitRenderHeight;
            Texture2D baseTexture = CreateTransparentTexture(width, height);
            ImageStackLayer[] layers = CreateNormalizedLayers(Layers.ToArray());
            Sprite[] masks = GetLayerMasks(layers);
            Texture2D result = ImageCompositor.CompositeImageStackLayers(
                baseTexture,
                layers,
                masks,
                _tintColors
            );

            return result;
        }

        private Graphics2DSettings GetGraphics2DSettings()
        {
            Graphics2DSettings settings = null;
            try
            {
                settings = Graphics2DSettings.Instance;
            }
            catch { }

            if (settings != null)
            {
                return settings;
            }

            "Graphics2DSettings not found in Resources/GameSettings (expected under Resources/GameSettings/*). Using default 512x512.".LogError(
                "StackedImage.CompositeLayers"
            );
            if (_editorFallbackGraphics2DSettings == null)
            {
                _editorFallbackGraphics2DSettings =
                    ScriptableObject.CreateInstance<Graphics2DSettings>();
            }

            return _editorFallbackGraphics2DSettings;
        }

        private static Texture2D CreateTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var clearPixels = new Color[width * height];
            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = new Color(0, 0, 0, 0);
            }

            texture.SetPixels(clearPixels);
            texture.Apply();
            return texture;
        }

        private ImageStackLayer[] CreateNormalizedLayers(ImageStackLayer[] sourceLayers)
        {
            var layers = new ImageStackLayer[sourceLayers.Length];
            bool correctedScale = false;
            for (int i = 0; i < sourceLayers.Length; i++)
            {
                layers[i] = CopyNormalizedLayer(sourceLayers[i], ref correctedScale);
            }

            if (correctedScale)
            {
                $"CompositeLayers: One or more layers in image '{_key}' had non-positive Scale and were temporarily normalized to 1.0 for preview/compositing. Consider fixing the layer data.".LogWarning();
            }

            return layers;
        }

        private static ImageStackLayer CopyNormalizedLayer(
            ImageStackLayer source,
            ref bool correctedScale
        )
        {
            if (source == null)
            {
                return null;
            }

            var copy = new ImageStackLayer
            {
                Sprite = source.Sprite,
                Mask = source.Mask,
                Offset = source.Offset,
                Scale = source.Scale,
                Rotation = source.Rotation,
                Order = source.Order,
            };
            PreserveLayerTag(source, copy);
            PreserveLayerTint(source, copy);
            if (copy.Scale <= 0f)
            {
                copy.Scale = 1f;
                correctedScale = true;
            }

            return copy;
        }

        private static void PreserveLayerTag(ImageStackLayer source, ImageStackLayer copy)
        {
            try
            {
                copy.Tag = source.Tag;
            }
            catch { }
        }

        private static void PreserveLayerTint(ImageStackLayer source, ImageStackLayer copy)
        {
            try
            {
                var tintField = source.GetType().GetField("Tint");
                if (tintField?.GetValue(source) is Color tint)
                {
                    copy.GetType().GetField("Tint")?.SetValue(copy, tint);
                }
            }
            catch
            {
                // Preserve the existing fallback for layer types that do not expose Tint.
            }
        }

        private static Sprite[] GetLayerMasks(ImageStackLayer[] layers)
        {
            var masks = new Sprite[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                masks[i] = layers[i]?.Mask;
            }

            return masks;
        }

        private void SaveToFile(Texture2D texture)
        {
#if UNITY_EDITOR
            ClearValidationStatus();

            // Find GamePackageSettings to determine the correct save location
            var gamePackageSettings = GamePackage.GamePackageSettings.Instance;
            if (gamePackageSettings == null)
            {
                SetValidationError(
                    "Portrait rendering failed: Could not find GamePackageSettings in Resources. Please create a GamePackageSettings asset in your project."
                );
                return;
            }

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

            // Build the image save path: {ResourcesPath}/Components/Characters/Portraits
            string portraitSavePath = System.IO.Path.Combine(
                resourcesPath.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString()),
                "Components",
                "Characters",
                "Portraits"
            );

            string fileName = $"{_key}.png";
            string fullPath = System.IO.Path.Combine(portraitSavePath, fileName);

            // Create directory if it doesn't exist
            if (!System.IO.Directory.Exists(portraitSavePath))
            {
                System.IO.Directory.CreateDirectory(portraitSavePath);
            }

            // Save the texture as PNG
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, pngData);

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
#if UNITY_EDITOR
            ClearValidationStatus();

            // Find GamePackageSettings to determine the correct load location
            var gamePackageSettings = GamePackage.GamePackageSettings.Instance;
            if (gamePackageSettings == null)
            {
                SetValidationError(
                    "Portrait loading failed: Could not find GamePackageSettings in Resources."
                );
                return;
            }

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

            // Import the texture with sprite settings
            UnityEditor.TextureImporter importer =
                UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;

            if (importer == null)
            {
                $"Could not get TextureImporter for: {assetPath}".LogError(
                    "StackedImage.LoadSavedSprite"
                );
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
            }

            // Load the sprite
            _savedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (_savedSprite != null)
            {
                if (_owner != null)
                {
                    var ownerPath = UnityEditor.AssetDatabase.GetAssetPath(
                        _owner
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
                $"Failed to load sprite from: {assetPath}".LogError("StackedImage.LoadSavedSprite");
            }
#endif
        }
    }
}
