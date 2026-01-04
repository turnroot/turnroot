#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Turnroot.Graphics2D.Utilities
{
    public static class TextureValidator
    {
        public static bool EnsureReadable(Texture2D tex, string context) =>
            IsReadable(tex) || MakeReadable(tex);

        // Convenience overload for callers that don't want to provide a context string.
        public static bool EnsureReadable(Texture2D tex) => EnsureReadable(tex, "TextureValidator");

        private static bool IsReadable(Texture2D tex)
        {
            if (tex == null)
            {
                return false;
            }

            // If the texture is an imported asset, prefer the importer setting
            string path = AssetDatabase.GetAssetPath(tex);
            if (!string.IsNullOrEmpty(path))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    return importer.isReadable;
                }
            }

            // Fallback to runtime check (may throw if unreadable)
            try
            {
                return tex.isReadable;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool MakeReadable(Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(
                    $"Could not find TextureImporter for '{tex?.name ?? "(null)"}' at path '{path}'."
                );
                return false;
            }

            if (importer.isReadable)
            {
                return true;
            }

            importer.isReadable = true;
            importer.SaveAndReimport();

            // Re-check importer setting (SaveAndReimport should update it)
            var refreshedImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (refreshedImporter != null && refreshedImporter.isReadable)
            {
                return true;
            }

            // As a final fallback, try runtime property
            try
            {
                return tex != null && tex.isReadable;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsGrayscalePNG(Texture2D texture)
        {
            if (texture == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                var data = System.IO.File.ReadAllBytes(path);
                if (data.Length < 24)
                {
                    return false;
                }

                // Validate PNG signature
                if (
                    data[0] != 0x89
                    || data[1] != 0x50
                    || data[2] != 0x4E
                    || data[3] != 0x47
                    || data[4] != 0x0D
                    || data[5] != 0x0A
                    || data[6] != 0x1A
                    || data[7] != 0x0A
                )
                {
                    return false;
                }

                int pos = 8;
                while (pos + 12 < data.Length)
                {
                    int length =
                        (data[pos] << 24)
                        | (data[pos + 1] << 16)
                        | (data[pos + 2] << 8)
                        | data[pos + 3];
                    string type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
                    if (type == "IHDR" && length >= 13)
                    {
                        int colorType = data[pos + 17];
                        return colorType == 0 || colorType == 4; // grayscale or grayscale+alpha
                    }

                    pos += length + 12;
                }
            }
            catch (System.Exception)
            {
                Debug.LogError($"Failed to read PNG file at path '{path}'.");
                return false;
            }

            return false;
        }

        /// <summary>
        /// Validate that a texture matches the expected dimensions.
        /// Logs a warning and returns false if the texture is null or the size doesn't match.
        /// </summary>
        public static bool ValidateSize(
            Texture2D tex,
            int expectedWidth,
            int expectedHeight,
            string context = null
        )
        {
            if (tex == null)
            {
                if (!string.IsNullOrEmpty(context))
                {
                    Debug.LogWarning($"{context}: texture is null.");
                }

                return false;
            }

            if (tex.width != expectedWidth || tex.height != expectedHeight)
            {
                var ctx = string.IsNullOrEmpty(context) ? "Texture" : context;
                Debug.LogWarning(
                    $"{ctx}: size mismatch. Expected {expectedWidth}x{expectedHeight}, got {tex.width}x{tex.height}."
                );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate that two textures have identical dimensions. Logs a warning and returns false on mismatch.
        /// </summary>
        public static bool ValidateMatch(
            Texture2D expected,
            Texture2D actual,
            string context = null
        )
        {
            if (expected == null || actual == null)
            {
                var ctx = string.IsNullOrEmpty(context) ? "Texture match" : context;
                Debug.LogWarning(
                    $"{ctx}: one or both textures are null (expected='{expected?.name ?? "(null)"}', actual='{actual?.name ?? "(null)"}')."
                );
                return false;
            }

            return ValidateSize(
                actual,
                expected.width,
                expected.height,
                context ?? $"Texture '{expected.name}'"
            );
        }

        /// <summary>
        /// Convenience overloads for Sprite arguments.
        /// </summary>
        public static bool ValidateMatch(
            Sprite expectedSprite,
            Sprite actualSprite,
            string context = null
        )
        {
            if (expectedSprite == null || actualSprite == null)
            {
                var ctx = string.IsNullOrEmpty(context) ? "Sprite match" : context;
                Debug.LogWarning(
                    $"{ctx}: one or both sprites are null (expected='{expectedSprite?.name ?? "(null)"}', actual='{actualSprite?.name ?? "(null)"}')."
                );
                return false;
            }
            return ValidateMatch(
                expectedSprite.texture,
                actualSprite.texture,
                context ?? $"Sprite '{expectedSprite.name}'"
            );
        }
    }
}

#endif
