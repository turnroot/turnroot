using System;
using System.Collections.Generic;
using Assets.AbstractScripts.Graphics2D;
using Assets.Prototypes.Graphics.Portrait;
using NaughtyAttributes;
using UnityEngine;

namespace Assets.Prototypes.Characters.Subclasses
{
    [Serializable]
    public class Portrait
    {
        [SerializeField]
        private CharacterData _owner;

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
        private Color[] _tintColors = new Color[3] { Color.white, Color.white, Color.white };

        private Guid _id;

        public CharacterData Owner => _owner;
        public ImageStack ImageStack => _imageStack;
        public string Key => _key;

        public Sprite RuntimeSprite => _runtimeSprite;
        public Sprite SavedSprite => _savedSprite;
        public Guid Id => _id;
        public Color[] TintColors => _tintColors;

        public void SetOwner(CharacterData owner)
        {
            _owner = owner;
            UpdateTintColorsFromOwner();
        }

        public void SetKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _key = key;
                Debug.Log($"Portrait key set to: {_key}");
            }
            else
            {
                // If empty/null key is passed, generate a new one
                if (_id == Guid.Empty)
                {
                    _id = Guid.NewGuid();
                    _idString = _id.ToString();
                }
                _key = $"portrait_{Guid.NewGuid()}";
                Debug.Log($"Generated new portrait key: {_key}");
            }
        }

        public Portrait()
        {
            _id = Guid.NewGuid();
            _idString = _id.ToString();
            _key = $"portrait_{_id}";
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
                _key = $"portrait_{_id}";
                Debug.Log($"Generated new portrait key: {_key}");
            }

            // Initialize tint colors array if null
            if (_tintColors == null || _tintColors.Length < 3)
            {
                _tintColors = new Color[3] { Color.white, Color.white, Color.white };
            }

            // Update tint colors from owner character
            UpdateTintColorsFromOwner();
        }

        public void UpdateTintColorsFromOwner()
        {
            // Ensure array is initialized
            if (_tintColors == null || _tintColors.Length < 3)
            {
                _tintColors = new Color[3] { Color.white, Color.white, Color.white };
            }

            if (_owner != null)
            {
                _tintColors[0] = _owner.AccentColor1;
                _tintColors[1] = _owner.AccentColor2;
                _tintColors[2] = _owner.AccentColor3;
            }
        }

        public override string ToString()
        {
            return $"p{_id}";
        }

        public string Identify()
        {
            return $"Portrait(ID: {_id}, Owner: {_owner.name}, Key: {_key})";
        }

        public void Render()
        {
            // Validate that we have an ImageStack
            if (_imageStack == null)
            {
                Debug.LogWarning("Cannot render portrait: ImageStack is not assigned.");
                return;
            }

            // Ensure key is valid
            if (string.IsNullOrEmpty(_key))
            {
                if (_id == Guid.Empty)
                {
                    _id = Guid.NewGuid();
                    _idString = _id.ToString();
                }
                _key = $"portrait_{_id}";
                Debug.LogWarning($"Portrait key was empty, generated new key: {_key}");
            }

            Debug.Log($"Rendering portrait with key: {_key}");

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
                _key = $"portrait_{Guid.NewGuid()}";
                Debug.Log($"Generated portrait key during composite: {_key}");
            }

            // Create base texture
            Texture2D baseTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            Color[] clearPixels = new Color[512 * 512];
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
            string directoryPath = System.IO.Path.Combine(
                UnityEngine.Application.dataPath,
                "Resources",
                "GameContent",
                "Graphics",
                "Portraits"
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
            Debug.Log($"Successfully saved portrait texture: {fileName} to {fullPath}");

#if UNITY_EDITOR
            // Refresh the asset database so Unity sees the new file
            UnityEditor.AssetDatabase.Refresh();

            // Force Unity to import the asset immediately
            UnityEditor.AssetDatabase.ImportAsset(
                $"Assets/Resources/GameContent/Graphics/Portraits/{fileName}"
            );
#endif
        }

        private void LoadSavedSprite()
        {
#if UNITY_EDITOR
            // Wait for asset database to finish importing
            UnityEditor.AssetDatabase.Refresh();

            string assetPath = $"Assets/Resources/GameContent/Graphics/Portraits/{_key}.png";

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
                importer.filterMode = FilterMode.Point; // For pixel art
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
