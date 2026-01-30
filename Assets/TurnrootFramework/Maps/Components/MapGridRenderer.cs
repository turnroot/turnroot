#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Renders MapGrid to image files - full map, standard minimap, and unexplored map
    /// </summary>
    public class MapGridRenderer
    {
        // These values are configurable via GamewideUiSettings (Map Rendering)
        private int _cellSize = 32; // Default fallback
        private string _iconPath = "EditorSettings/MapGridEditorIcons/"; // Resources-relative path

        private Color _gridLineColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private Color _blackCellColor = Color.black;
        private Color _darkGrayTerrainColor = new Color(0.3f, 0.3f, 0.3f, 1f); // For wall, deep water, mountain
        private Color _lightGrayTerrainColor = new Color(0.2f, 0.2f, 0.2f, 1f); // For tall wall, void
        private Color _blueSpawnColor = new Color(0.2f, 0.5f, 1f, 1f);

        private void InitializeFromSettings()
        {
            var settings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<GameSettings.GamewideUiSettings>(
                    "GameSettings"
                );
            if (settings == null)
            {
                return;
            }

            _cellSize = Mathf.Max(4, settings.GetMapCellSize());

            var path = settings.GetMapIconPath() ?? string.Empty;
            path = path.Trim();
            if (path.Length == 0)
            {
                // Keep default
            }
            else
            {
                // Ensure trailing slash for Resource loading
                if (!path.EndsWith("/"))
                {
                    path += "/";
                }

                _iconPath = path;
            }

            _gridLineColor = settings.GetMapGridLineColor();
            _blackCellColor = settings.GetMapBlackCellColor();
            _darkGrayTerrainColor = settings.GetMapDarkGrayTerrainColor();
            _lightGrayTerrainColor = settings.GetMapLightGrayTerrainColor();
            _blueSpawnColor = settings.GetMapBlueSpawnColor();
        }

        private readonly Dictionary<string, Texture2D> _iconCache = new();

        // Terrain types that should show as dark gray (30%) on minimap
        private readonly HashSet<string> _darkGrayTerrainTypes = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase
        )
        {
            "wall",
            "deep water",
            "deepwater",
            "mountain",
        };

        // Terrain types that should show as light gray (20%) on minimap
        private readonly HashSet<string> _lightGrayTerrainTypes = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase
        )
        {
            "tall wall",
            "tallwall",
            "void",
        };

        // Feature types to skip rendering
        private readonly HashSet<string> _skipFeatureTypes = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase
        )
        {
            "undergrounditem",
            "underground",
            "shelter",
        };

        public void RenderAndSaveMapImages(
            MapGrid grid,
            out Sprite fullMapSprite,
            out Sprite standardMapSprite,
            out Sprite unexploredMapSprite
        )
        {
            // Initialize out parameters
            fullMapSprite = null;
            standardMapSprite = null;
            unexploredMapSprite = null;

            if (grid == null)
            {
                Debug.LogError("MapGridRenderer: Cannot render null grid");
                return;
            }

            // Load rendering overrides from settings
            InitializeFromSettings();

            // Ensure grid points exist
            grid.EnsureGridPoints();

            // Find the save path using GamePackageSettings
            string savePath = GetMapSavePath();
            if (string.IsNullOrEmpty(savePath))
            {
                Debug.LogError("MapGridRenderer: Could not determine save path");
                return;
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                TurnrootLogger.Log($"Created directory: {savePath}");
            }

            // Generate map name (sanitize for file system)
            string mapName = string.IsNullOrEmpty(grid.MapName)
                ? "untitled_map"
                : SanitizeFileName(grid.MapName);

            // Render and save full map (colored terrain + black icons + blue spawn points)
            Texture2D fullMap = RenderFullMap(grid);
            if (fullMap != null)
            {
                fullMapSprite = SaveTexture(fullMap, savePath, $"{mapName}_full");
                Object.DestroyImmediate(fullMap);
            }

            // Render and save standard minimap (black/dark gray terrain + white icons + blue spawn points)
            Texture2D standardMap = RenderStandardMinimap(grid);
            if (standardMap != null)
            {
                standardMapSprite = SaveTexture(standardMap, savePath, $"{mapName}_standard");
                Object.DestroyImmediate(standardMap);
            }

            // Render and save unexplored map (just black + blue spawn points, no features/terrain)
            Texture2D unexploredMap = RenderUnexploredMap(grid);
            if (unexploredMap != null)
            {
                unexploredMapSprite = SaveTexture(unexploredMap, savePath, $"{mapName}_unexplored");
                Object.DestroyImmediate(unexploredMap);
            }

            TurnrootLogger.Log(
                $"MapGridRenderer: Successfully rendered map images for '{mapName}'"
            );
        }

        private Texture2D RenderFullMap(MapGrid grid)
        {
            // Get traversable area bounds
            var bounds = GetTraversableBounds(grid);
            int width = bounds.width * _cellSize;
            int height = bounds.height * _cellSize;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; // Crisp pixel art style

            // Load terrain asset for colors
            TerrainTypes terrainAsset = TerrainTypes.LoadDefault();

            // Draw each cell within traversable area
            for (int row = bounds.minRow; row <= bounds.maxRow; row++)
            {
                for (int col = bounds.minCol; col <= bounds.maxCol; col++)
                {
                    // Calculate position in the texture (offset by bounds)
                    int texRow = row - bounds.minRow;
                    int texCol = col - bounds.minCol;

                    Vector2Int cellPos = new Vector2Int(row, col);
                    bool isSpawnPoint = grid.PlayerTeamSpawnPoints.Contains(cellPos);

                    if (isSpawnPoint)
                    {
                        // Draw solid blue for spawn points
                        DrawCell(texture, texRow, texCol, _blueSpawnColor);
                    }
                    else
                    {
                        // Get terrain color
                        MapGridPoint point = grid.GetGridPoint(row, col);
                        Color cellColor = GetTerrainColor(point, terrainAsset);
                        DrawCell(texture, texRow, texCol, cellColor);

                        // Draw feature icon if present (black icons on colored terrain)
                        if (
                            point != null
                            && !string.IsNullOrEmpty(point.FeatureTypeId)
                            && !ShouldSkipFeature(point.FeatureTypeId)
                        )
                        {
                            DrawFeatureIcon(
                                texture,
                                texRow,
                                texCol,
                                point.FeatureTypeId,
                                Color.black
                            );
                        }
                    }

                    // Draw grid lines
                    DrawGridLines(texture, texRow, texCol, bounds.width, bounds.height);
                }
            }

            texture.Apply();
            return texture;
        }

        private Texture2D RenderStandardMinimap(MapGrid grid)
        {
            // Get traversable area bounds
            var bounds = GetTraversableBounds(grid);
            int width = bounds.width * _cellSize;
            int height = bounds.height * _cellSize;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            // Load terrain asset to check terrain types
            TerrainTypes terrainAsset = TerrainTypes.LoadDefault();

            // Draw each cell within traversable area
            for (int row = bounds.minRow; row <= bounds.maxRow; row++)
            {
                for (int col = bounds.minCol; col <= bounds.maxCol; col++)
                {
                    // Calculate position in the texture (offset by bounds)
                    int texRow = row - bounds.minRow;
                    int texCol = col - bounds.minCol;

                    Vector2Int cellPos = new Vector2Int(row, col);
                    bool isSpawnPoint = grid.PlayerTeamSpawnPoints.Contains(cellPos);

                    if (isSpawnPoint)
                    {
                        // Draw solid blue for spawn points
                        DrawCell(texture, texRow, texCol, _blueSpawnColor);
                    }
                    else
                    {
                        MapGridPoint point = grid.GetGridPoint(row, col);

                        // Check terrain type and assign appropriate color
                        Color cellColor = GetMinimapTerrainColor(point, terrainAsset);

                        DrawCell(texture, texRow, texCol, cellColor);

                        // Draw feature icon if present (white icons on black/gray background)
                        if (
                            point != null
                            && !string.IsNullOrEmpty(point.FeatureTypeId)
                            && !ShouldSkipFeature(point.FeatureTypeId)
                        )
                        {
                            DrawFeatureIcon(
                                texture,
                                texRow,
                                texCol,
                                point.FeatureTypeId,
                                Color.white
                            );
                        }
                    }

                    // Draw grid lines
                    DrawGridLines(texture, texRow, texCol, bounds.width, bounds.height);
                }
            }

            texture.Apply();
            return texture;
        }

        private Texture2D RenderUnexploredMap(MapGrid grid)
        {
            // Get traversable area bounds
            var bounds = GetTraversableBounds(grid);
            int width = bounds.width * _cellSize;
            int height = bounds.height * _cellSize;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            // Draw each cell within traversable area
            for (int row = bounds.minRow; row <= bounds.maxRow; row++)
            {
                for (int col = bounds.minCol; col <= bounds.maxCol; col++)
                {
                    // Calculate position in the texture (offset by bounds)
                    int texRow = row - bounds.minRow;
                    int texCol = col - bounds.minCol;

                    Vector2Int cellPos = new Vector2Int(row, col);
                    bool isSpawnPoint = grid.PlayerTeamSpawnPoints.Contains(cellPos);

                    // Just black and blue spawn points - no features, no terrain differences
                    Color cellColor = isSpawnPoint ? _blueSpawnColor : _blackCellColor;
                    DrawCell(texture, texRow, texCol, cellColor);

                    // Draw grid lines
                    DrawGridLines(texture, texRow, texCol, bounds.width, bounds.height);
                }
            }

            texture.Apply();
            return texture;
        }

        private Color GetMinimapTerrainColor(MapGridPoint point, TerrainTypes terrainAsset)
        {
            if (point == null)
            {
                return _blackCellColor;
            }

            TerrainType terrainType = terrainAsset?.GetTypeById(point.TerrainTypeId);
            if (terrainType == null)
            {
                terrainType = point.GetCachedTerrainType();
            }

            if (terrainType == null || string.IsNullOrEmpty(terrainType.Name))
            {
                return _blackCellColor;
            }

            // Check for light gray terrains (20%)
            if (_lightGrayTerrainTypes.Contains(terrainType.Name))
            {
                return _lightGrayTerrainColor;
            }

            // Check for dark gray terrains (30%)
            if (_darkGrayTerrainTypes.Contains(terrainType.Name))
            {
                return _darkGrayTerrainColor;
            }

            // Default to black
            return _blackCellColor;
        }

        private bool ShouldSkipFeature(string featureTypeId) =>
            !string.IsNullOrEmpty(featureTypeId) && _skipFeatureTypes.Contains(featureTypeId);

        private (
            int minRow,
            int maxRow,
            int minCol,
            int maxCol,
            int width,
            int height
        ) GetTraversableBounds(MapGrid grid)
        {
            if (grid == null)
            {
                TurnrootLogger.Log(
                    "MapGridRenderer: GetTraversableBounds called with null grid",
                    TurnrootLogger.LogLevel.Error
                );
                return (0, 0, 0, 0, 0, 0);
            }

            // Use the entire grid as the traversable area (no separate corners feature)
            int minRow = 0;
            int maxRow = Mathf.Max(0, grid.GridWidth - 1);
            int minCol = 0;
            int maxCol = Mathf.Max(0, grid.GridHeight - 1);
            int width = grid.GridWidth;
            int height = grid.GridHeight;

            TurnrootLogger.Log(
                $"MapGridRenderer: Rendering full grid area - Rows [{minRow}-{maxRow}], Cols [{minCol}-{maxCol}], Size: {width}x{height}"
            );

            return (minRow, maxRow, minCol, maxCol, width, height);
        }

        private void DrawCell(Texture2D texture, int row, int col, Color color)
        {
            int startX = row * _cellSize;
            int startY = col * _cellSize;

            for (int x = 0; x < _cellSize; x++)
            {
                for (int y = 0; y < _cellSize; y++)
                {
                    texture.SetPixel(startX + x, startY + y, color);
                }
            }
        }

        private void DrawGridLines(
            Texture2D texture,
            int row,
            int col,
            int gridWidth,
            int gridHeight
        )
        {
            int startX = row * _cellSize;
            int startY = col * _cellSize;
            int lineThickness = 1;

            // Always draw top edge
            for (int x = 0; x < _cellSize; x++)
            {
                for (int t = 0; t < lineThickness; t++)
                {
                    texture.SetPixel(startX + x, startY + t, _gridLineColor);
                }
            }

            // Always draw left edge
            for (int y = 0; y < _cellSize; y++)
            {
                for (int t = 0; t < lineThickness; t++)
                {
                    texture.SetPixel(startX + t, startY + y, _gridLineColor);
                }
            }

            // Always draw bottom edge
            for (int x = 0; x < _cellSize; x++)
            {
                for (int t = 0; t < lineThickness; t++)
                {
                    texture.SetPixel(startX + x, startY + _cellSize - 1 - t, _gridLineColor);
                }
            }

            // Always draw right edge
            for (int y = 0; y < _cellSize; y++)
            {
                for (int t = 0; t < lineThickness; t++)
                {
                    texture.SetPixel(startX + _cellSize - 1 - t, startY + y, _gridLineColor);
                }
            }
        }

        private void DrawFeatureIcon(
            Texture2D texture,
            int row,
            int col,
            string featureTypeId,
            Color tint
        )
        {
            // Try to load the icon texture
            Texture2D icon = GetToolIcon(featureTypeId);

            if (icon != null)
            {
                // Draw the icon with the specified tint color
                int startX = row * _cellSize;
                int startY = col * _cellSize;

                // Scale icon to fit in cell with some padding
                int padding = 4;
                int iconSize = _cellSize - (padding * 2);

                for (int x = 0; x < iconSize; x++)
                {
                    for (int y = 0; y < iconSize; y++)
                    {
                        // Sample from icon texture
                        float u = (float)x / iconSize;
                        float v = (float)y / iconSize;
                        Color iconPixel = icon.GetPixelBilinear(u, v);

                        // Only draw if icon pixel has some alpha
                        if (iconPixel.a > 0.1f)
                        {
                            // Apply tint color
                            Color tintedPixel = new Color(tint.r, tint.g, tint.b, iconPixel.a);
                            texture.SetPixel(
                                startX + padding + x,
                                startY + padding + y,
                                tintedPixel
                            );
                        }
                    }
                }
            }
            else
            {
                // Fallback: draw letter if no icon found
                string letter = MapGridPointFeature.GetFeatureLetter(featureTypeId);
                if (string.IsNullOrEmpty(letter))
                {
                    letter =
                        featureTypeId.Length > 0 ? featureTypeId.Substring(0, 1).ToUpper() : "?";
                }
                // For now, just draw a small square as placeholder
                // You could implement text rendering here if needed
                int startX = row * _cellSize + _cellSize / 3;
                int startY = col * _cellSize + _cellSize / 3;
                int size = _cellSize / 3;

                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        texture.SetPixel(startX + x, startY + y, tint);
                    }
                }
            }
        }

        private Texture2D GetToolIcon(string featureId)
        {
            if (string.IsNullOrEmpty(featureId))
            {
                return null;
            }

            string cacheKey = "feature_" + featureId;

            if (_iconCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // Build variant list (same logic as MapGridEditorWindow)
            var variants = new List<string>();

            // Map featureId to friendly names (matching the editor's ToolSet)
            var friendlyNameMap = new Dictionary<string, string>
            {
                { "treasure", "Treasure" },
                { "door", "Door" },
                { "warp", "Warp" },
                { "healing", "Healing" },
                { "ranged", "Ranged" },
                { "mechanism", "Mechanism" },
                { "control", "Control" },
                { "breakable", "BreakableWall" },
                { "shelter", "Shelter" },
                { "village", "Village" },
                { "fortress", "Fortress" },
                { "underground", "UndergroundItem" },
            };

            string friendlyName = null;
            friendlyNameMap.TryGetValue(featureId.ToLower(), out friendlyName);

            if (!string.IsNullOrEmpty(friendlyName))
            {
                variants.Add(friendlyName);
                variants.Add(friendlyName.Replace(" ", ""));
            }

            variants.Add(featureId);
            variants.Add(featureId.Replace(" ", "").ToLower());
            variants.Add(featureId.ToLower());

            if (featureId.Length > 0)
            {
                variants.Add(char.ToUpper(featureId[0]) + featureId.Substring(1));
            }

            // Try to load icon from Resources
            Texture2D tex = null;
            foreach (var variant in variants.Where(v => !string.IsNullOrEmpty(v)))
            {
                string path = _iconPath + variant;
                tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                {
                    break;
                }

                var spr = Resources.Load<Sprite>(path);
                if (spr?.texture != null)
                {
                    tex = spr.texture;
                    break;
                }
            }

            _iconCache[cacheKey] = tex;
            return tex;
        }

        private Color GetTerrainColor(MapGridPoint point, TerrainTypes terrainAsset)
        {
            if (point == null)
            {
                return Color.white;
            }

            TerrainType terrainType = terrainAsset?.GetTypeById(point.TerrainTypeId);
            if (terrainType == null)
            {
                terrainType = point.GetCachedTerrainType();
            }

            return terrainType?.EditorColor ?? Color.white;
        }

        private Sprite SaveTexture(Texture2D texture, string path, string fileName)
        {
            string fullPath = Path.Combine(path, $"{fileName}.png");

            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);

            TurnrootLogger.Log($"Saved map image: {fullPath}");

            // Refresh AssetDatabase and import
            AssetDatabase.Refresh();
            string assetPath = fullPath.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            AssetDatabase.ImportAsset(assetPath);

            // Configure as sprite
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = _cellSize;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();
            }

            // Load and return the sprite
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                TurnrootLogger.Log($"Loaded sprite reference for: {fileName}");
            }
            else
            {
                Debug.LogError($"Failed to load sprite from: {assetPath}");
            }

            return sprite;
        }

        private string GetMapSavePath()
        {
            var gamePackageSettings =
                Turnroot.Utilities.GameSettingsLoader.LoadFirst<GamePackage.GamePackageSettings>();
            if (gamePackageSettings == null)
            {
                Debug.LogError("Could not find GamePackageSettings in Resources");
                return null;
            }

            string gamePackageSettingsPath = AssetDatabase.GetAssetPath(gamePackageSettings);
            if (string.IsNullOrEmpty(gamePackageSettingsPath))
            {
                Debug.LogError("Could not determine GamePackageSettings location");
                return null;
            }

            // Extract base Resources path
            string resourcesPath = gamePackageSettingsPath.Substring(
                0,
                gamePackageSettingsPath.LastIndexOf("/GameSettings")
            );

            if (!resourcesPath.EndsWith("/Resources"))
            {
                Debug.LogError(
                    $"GamePackageSettings is not in a Resources folder: {gamePackageSettingsPath}"
                );
                return null;
            }

            // Build map save path: {ResourcesPath}/Components/Maps
            string mapSavePath = Path.Combine(
                resourcesPath.Replace("/", Path.DirectorySeparatorChar.ToString()),
                "Components",
                "Maps"
            );

            return mapSavePath;
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid file name characters
            char[] invalids = Path.GetInvalidFileNameChars();
            string sanitized = string.Join(
                "_",
                fileName.Split(invalids, System.StringSplitOptions.RemoveEmptyEntries)
            );
            return sanitized.Trim().Replace(" ", "_").ToLower();
        }
    }
}
#endif
