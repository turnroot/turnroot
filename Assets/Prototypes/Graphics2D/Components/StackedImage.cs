using System;
using System.Collections.Generic;
using Assets.AbstractScripts.Graphics2D;
using Assets.Prototypes.Graphics.Portrait;
using NaughtyAttributes;
using UnityEngine;

namespace Assets.Prototypes.Graphics2D
{
    [Serializable]
    public abstract class StackedImage<TOwner>
        where TOwner : UnityEngine.Object
    {
        [SerializeField]
        protected TOwner _owner;

        [SerializeField]
        private ImageStack _imageStack;

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

        private Guid _id;

        public TOwner Owner => _owner;
        public ImageStack ImageStack => _imageStack;
        public string Key => _key;

        public Sprite RuntimeSprite => _runtimeSprite;
        public Sprite SavedSprite => _savedSprite;
        public Guid Id => _id;
        public Color[] TintColors => _tintColors;

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
                Debug.Log($"StackedImage key set to: {_key}");
            }
            else
            {
                // If empty/null key is passed, generate a new one
                EnsureKeyInitialized();
                Debug.Log($"Generated new stackedImage key: {_key}");
            }
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
                Debug.Log($"Generated new stackedImage key: {_key}");
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

        // Subclasses must provide the subdirectory name for saving files
        // e.g., "Portraits" for Portrait class, "ItemIcons" for ItemIcon class
        protected abstract string GetSaveSubdirectory();

        public override string ToString()
        {
            return $"p{_id}";
        }

        public string Identify()
        {
            string ownerName = _owner != null ? _owner.name : "null";
            return $"StackedImage(ID: {_id}, Owner: {ownerName}, Key: {_key})";
        }

        public void Render()
        {
            // Validate that we have an ImageStack
            if (_imageStack == null)
            {
                Debug.LogWarning("Cannot render stackedImage: ImageStack is not assigned.");
                return;
            }

            // Ensure key is valid
            if (string.IsNullOrEmpty(_key))
            {
                EnsureKeyInitialized();
                Debug.LogWarning($"StackedImage key was empty, generated new key: {_key}");
            }

            Debug.Log($"Rendering stackedImage with key: {_key}");

            // Use compositor to create the final texture
            Texture2D composited = CompositeLayers();
            if (composited == null)
            {
                Debug.LogError("Failed to composite layers.");
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
            if (_imageStack == null || _imageStack.Layers == null || _imageStack.Layers.Count == 0)
            {
                Debug.LogWarning("No layers to composite.");
                return null;
            }

            // Ensure key is set before compositing
            if (string.IsNullOrEmpty(_key))
            {
                EnsureKeyInitialized();
            }

            // Get render dimensions from settings
            GraphicsPrototypesSettings settings = Resources.Load<GraphicsPrototypesSettings>(
                "GraphicsPrototypesSettings"
            );
            int width = settings.portraitRenderWidth;
            int height = settings.portraitRenderHeight;

            // Create base texture with transparent pixels
            Texture2D baseTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] clearPixels = new Color[width * height];
            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = new Color(0, 0, 0, 0);
            }
            baseTexture.SetPixels(clearPixels);
            baseTexture.Apply();

            // Composite the layers using static method
            ImageStackLayer[] layers = _imageStack.Layers.ToArray();

            // Extract masks from layers
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
            // Use Application.dataPath to get the correct project path
            // Subclass provides the subdirectory name (e.g., "Portraits", "ItemIcons")
            string subdirectory = GetSaveSubdirectory();
            string directoryPath = System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "Resources",
                "GameContent",
                "Graphics",
                subdirectory
            );
            string fileName = $"{_key}.png";
            string fullPath = System.IO.Path.Combine(directoryPath, fileName);

            // Create directory if it doesn't exist
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
                Debug.Log($"Created directory: {directoryPath}");
            }

            // Save the texture as PNG
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, pngData);
            Debug.Log($"Successfully saved stackedImage texture: {fileName} to {fullPath}");

#if UNITY_EDITOR
            // Refresh the asset database so Unity sees the new file
            UnityEditor.AssetDatabase.Refresh();

            // Force Unity to import the asset immediately
            string assetImportPath =
                $"Assets/Resources/GameContent/Graphics/{subdirectory}/{fileName}";
            UnityEditor.AssetDatabase.ImportAsset(assetImportPath);
#endif
        }

        private void LoadSavedSprite()
        {
#if UNITY_EDITOR
            // Wait for asset database to finish importing
            UnityEditor.AssetDatabase.Refresh();

            string subdirectory = GetSaveSubdirectory();
            string assetPath = $"Assets/Resources/GameContent/Graphics/{subdirectory}/{_key}.png";

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

                // Mark the character as dirty to save the sprite reference
                if (_owner != null)
                {
                    UnityEditor.EditorUtility.SetDirty(_owner);
                    UnityEditor.AssetDatabase.SaveAssets();
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
