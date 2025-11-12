using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.AbstractScripts.Graphics2D
{
    public static class ImageCompositor
    {
        public static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogError("Cannot create sprite from null texture.");
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        public static Color[] TintSpritePixels(Sprite sprite, Sprite mask, Color[] tints)
        {
            // The mask Red channel uses tints[0]
            // Green channel uses tints[1]
            // Blue channel uses tints[2]

            if (sprite == null || mask == null)
            {
                Debug.LogError("Invalid parameters for TintSpritePixels.");
                return null;
            }

            if (tints == null || tints.Length < 3)
            {
                Debug.LogError("TintSpritePixels requires at least 3 tint colors.");
                return null;
            }

            if (
                mask.texture.width != sprite.texture.width
                || mask.texture.height != sprite.texture.height
            )
            {
                Debug.LogError("Mask and sprite textures must be the same size.");
                return null;
            }

            // Check if textures are readable
            if (!IsTextureReadable(sprite.texture) || !IsTextureReadable(mask.texture))
            {
                Debug.LogError(
                    "Sprite or mask texture is not readable. Enable Read/Write in import settings."
                );
                return null;
            }

            Color[] originalPixels = sprite.texture.GetPixels();
            Color[] maskPixels = mask.texture.GetPixels();
            Color[] tintedPixels = new Color[originalPixels.Length];

            for (int i = 0; i < originalPixels.Length; i++)
            {
                Color original = originalPixels[i];
                Color maskPixel = maskPixels[i];

                float rFactor = maskPixel.r;
                float gFactor = maskPixel.g;
                float bFactor = maskPixel.b;

                // Calculate total mask strength for normalization
                float totalStrength = rFactor + gFactor + bFactor;

                Color finalColor;

                if (totalStrength > 0f)
                {
                    // Normalize the factors so they sum to 1
                    float rWeight = rFactor / totalStrength;
                    float gWeight = gFactor / totalStrength;
                    float bWeight = bFactor / totalStrength;

                    // Blend the three tint colors together based on normalized weights
                    Color blendedTint =
                        tints[0] * rWeight + tints[1] * gWeight + tints[2] * bWeight;

                    // Lerp between original and the blended tint using total strength
                    finalColor = Color.Lerp(original, blendedTint, totalStrength);
                    finalColor.a = original.a; // Preserve original alpha
                }
                else
                {
                    // No tinting if all channels are zero
                    finalColor = original;
                }

                tintedPixels[i] = finalColor;
            }
            return tintedPixels;
        }

        private static Color[] ApplyColorsToLayerPixels(
            Color[] layerPixels,
            Sprite mask,
            Sprite sprite,
            ImageStackLayer layer,
            Color[] tints,
            int layerIndex
        )
        {
            // Apply tinting:
            // - If a mask and global tints are provided, use mask-based tinting via TintSpritePixels.
            // - Otherwise, for unmasked grayscale layers, use the layer's per-layer Tint
            //   (expected to be stored on ImageStackLayer.Tint) and convert grayscale -> color.
            if (mask != null && tints != null)
            {
                // Pre-validate common failure modes so we can give a clearer message
                string layerTag = layer != null ? layer.Tag : string.Empty;
                string spriteName =
                    sprite != null && sprite.texture != null ? sprite.texture.name : "<null>";
                string maskName =
                    mask != null && mask.texture != null ? mask.texture.name : "<null>";

                if (tints == null || tints.Length < 3)
                {
                    Debug.LogWarning(
                        $"Tinting skipped for layer {layerIndex} (sprite='{spriteName}', tag='{layerTag}'): tints array is null or too short (length={(tints == null ? 0 : tints.Length)}). Provide exactly 3 tint colors for mask-based tinting."
                    );
                    return null;
                }

                if (sprite == null || sprite.texture == null || mask.texture == null)
                {
                    Debug.LogWarning(
                        $"Tinting skipped for layer {layerIndex} (tag='{layerTag}'): sprite or mask texture is null."
                    );
                    return null;
                }

                if (
                    mask.texture.width != sprite.texture.width
                    || mask.texture.height != sprite.texture.height
                )
                {
                    Debug.LogWarning(
                        $"Tinting skipped for layer {layerIndex} (sprite='{spriteName}', mask='{maskName}', tag='{layerTag}'): size mismatch (sprite={sprite.texture.width}x{sprite.texture.height}, mask={mask.texture.width}x{mask.texture.height}). Ensure both textures are the same dimensions."
                    );
                    return null;
                }

                if (!IsTextureReadable(sprite.texture) || !IsTextureReadable(mask.texture))
                {
                    Debug.LogWarning(
                        $"Tinting skipped for layer {layerIndex} (sprite='{spriteName}', mask='{maskName}', tag='{layerTag}'): one or more textures are not readable. Enable Read/Write in import settings for both assets."
                    );
                    return null;
                }

                var tinted = TintSpritePixels(sprite, mask, tints);
                if (tinted == null)
                {
                    Debug.LogWarning(
                        $"Tinting failed for layer {layerIndex} (sprite='{spriteName}', tag='{layerTag}'): TintSpritePixels returned null despite pre-checks. Skipping."
                    );
                    return null;
                }
                return tinted;
            }

            // No mask-based tinting requested. If the layer object carries a Tint
            // field, bake that tint into the layer pixels. We expect the caller to
            // populate ImageStackLayer.Tint for unmasked layers (e.g., Hair).
            Color layerTint = Color.white;
            if (layer != null)
            {
                layerTint = layer.Tint;
            }

            bool isGrayscale = false;
#if UNITY_EDITOR
            isGrayscale = IsGrayscalePNG(sprite.texture);
#endif

            Color[] tintedPixels;
            if (isGrayscale)
            {
                // Convert grayscale to colored pixels: use luminance as strength
                tintedPixels = new Color[layerPixels.Length];
                for (int p = 0; p < layerPixels.Length; p++)
                {
                    Color src = layerPixels[p];
                    // compute luminance from rgb
                    float lum = 0.299f * src.r + 0.587f * src.g + 0.114f * src.b;
                    Color colored = new Color(
                        layerTint.r * lum,
                        layerTint.g * lum,
                        layerTint.b * lum,
                        src.a
                    );
                    tintedPixels[p] = colored;
                }
            }
            else
            {
                // Image has color or cannot check, do not apply tint
                tintedPixels = layerPixels;
            }
            return tintedPixels;
        }

        public static Texture2D CompositeImageStackLayers(
            Texture2D baseTexture,
            ImageStackLayer[] layers,
            Sprite[] masks = null,
            Color[] tints = null
        )
        {
            if (baseTexture == null || layers == null)
            {
                Debug.LogError("Invalid parameters for CompositeImageStackLayers.");
                return null;
            }

            // Create texture and pixel buffer
            Color[] finalPixels;
            Texture2D compositedTexture = CreateCompositedTexture(baseTexture, out finalPixels);

            var sortedLayers = SortLayersByOrder(layers);

            for (int layerIndex = 0; layerIndex < sortedLayers.Length; layerIndex++)
            {
                ImageStackLayer layer = sortedLayers[layerIndex];
                if (layer == null || layer.Sprite == null)
                    continue;

                Sprite sprite = layer.Sprite;
                Sprite mask =
                    (masks != null && masks.Length > layerIndex) ? masks[layerIndex] : null;

                Color[] layerPixels = GetSpritePixelsIfReadable(sprite);
                if (layerPixels == null)
                {
                    NotifyTextureNotReadable(sprite.texture, layerIndex);
                    continue;
                }

                layerPixels = ApplyColorsToLayerPixels(
                    layerPixels,
                    mask,
                    sprite,
                    layer,
                    tints,
                    layerIndex
                );

                if (layerPixels == null)
                    continue;

                CompositeLayerOntoFinal(
                    layerPixels,
                    sprite,
                    layer,
                    finalPixels,
                    baseTexture.width,
                    baseTexture.height
                );
            }

            compositedTexture.SetPixels(finalPixels);
            compositedTexture.Apply();
            return compositedTexture;
        }

        public static Texture2D CompositeLayersOnTexture(
            Texture2D baseTexture,
            Sprite[] layers,
            Sprite[] masks = null,
            Color[] tints = null
        )
        {
            if (baseTexture == null || layers == null)
            {
                Debug.LogError("Invalid parameters for CompositeLayersOnTexture.");
                return null;
            }

            Texture2D compositedTexture = new(
                baseTexture.width,
                baseTexture.height,
                TextureFormat.RGBA32,
                false
            );
            Color[] basePixels = baseTexture.GetPixels();
            Color[] finalPixels = new Color[basePixels.Length];
            System.Array.Copy(basePixels, finalPixels, basePixels.Length);

            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                Sprite layer = layers[layerIndex];
                if (layer == null)
                    continue;

                Sprite mask =
                    (masks != null && masks.Length > layerIndex) ? masks[layerIndex] : null;

                // Validate layer size matches base texture
                if (
                    layer.texture.width != baseTexture.width
                    || layer.texture.height != baseTexture.height
                )
                {
                    Debug.LogWarning(
                        $"Layer {layerIndex} size mismatch. Skipping. Expected {baseTexture.width}x{baseTexture.height}, got {layer.texture.width}x{layer.texture.height}"
                    );
                    continue;
                }

                Color[] layerPixels = GetSpritePixelsIfReadable(layer);
                if (layerPixels == null)
                {
                    NotifyTextureNotReadable(layer.texture, layerIndex);
                    continue;
                }

                // Apply tinting if mask and tints are provided (same tints for all layers)
                if (mask != null && tints != null)
                {
                    layerPixels = ApplyColorsToLayerPixels(
                        layerPixels,
                        mask,
                        layer,
                        null,
                        tints,
                        layerIndex
                    );
                    if (layerPixels == null)
                    {
                        Debug.LogWarning($"Failed to tint layer {layerIndex}. Skipping.");
                        continue;
                    }
                }

                // Composite layer onto final image using elementwise blending (aligned buffers)
                CompositePixelsElementwise(finalPixels, layerPixels);
            }

            compositedTexture.SetPixels(finalPixels);
            compositedTexture.Apply();
            return compositedTexture;
        }

        private static bool IsTextureReadable(Texture2D texture)
        {
            try
            {
                _ = texture.GetPixel(0, 0);
                return true;
            }
            catch
            {
                Debug.LogError(
                    $"Texture '{texture.name}' is not readable. Enable Read/Write in import settings."
                );

                return false;
            }
        }

#if UNITY_EDITOR
        private static bool IsGrayscalePNG(Texture2D texture)
        {
            if (texture == null)
                return false;
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path) || !path.ToLower().EndsWith(".png"))
                return false;
            try
            {
                byte[] data = System.IO.File.ReadAllBytes(path);
                if (
                    data.Length < 24
                    || data[0] != 0x89
                    || data[1] != 0x50
                    || data[2] != 0x4E
                    || data[3] != 0x47
                    || data[4] != 0x0D
                    || data[5] != 0x0A
                    || data[6] != 0x1A
                    || data[7] != 0x0A
                )
                    return false;
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
            catch { }
            return false;
        }
#endif

        // Helper: create the composited texture and initialize final pixel buffer
        private static Texture2D CreateCompositedTexture(
            Texture2D baseTexture,
            out Color[] finalPixels
        )
        {
            Texture2D compositedTexture = new(
                baseTexture.width,
                baseTexture.height,
                TextureFormat.RGBA32,
                false
            );
            Color[] basePixels = baseTexture.GetPixels();
            finalPixels = new Color[basePixels.Length];
            System.Array.Copy(basePixels, finalPixels, basePixels.Length);
            return compositedTexture;
        }

        // Helper: return a sorted copy of layers by Order
        private static ImageStackLayer[] SortLayersByOrder(ImageStackLayer[] layers)
        {
            var copy = new ImageStackLayer[layers.Length];
            System.Array.Copy(layers, copy, layers.Length);
            System.Array.Sort(copy, (a, b) => a.Order.CompareTo(b.Order));
            return copy;
        }

        // Helper: get pixels from a sprite if readable, otherwise null
        private static Color[] GetSpritePixelsIfReadable(Sprite sprite)
        {
            if (sprite == null)
                return null;
            if (!IsTextureReadable(sprite.texture))
                return null;
            return sprite.texture.GetPixels();
        }

        // Helper: show an editor popup for a non-readable texture and offer to open the
        // texture asset in the inspector (editor-only). This centralizes the UI so both
        // composition paths behave identically.
        private static void NotifyTextureNotReadable(Texture2D texture, int layerIndex)
        {
            if (texture == null)
                return;

            string message =
                $"Layer {layerIndex} texture '{texture.name}' is not readable. Enable Read/Write in the texture import settings and reimport the asset.";
#if UNITY_EDITOR
            int choice = EditorUtility.DisplayDialogComplex(
                "Texture not readable",
                message,
                "OK",
                "Open Import Settings",
                "Skip"
            );
            if (choice == 1)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                var obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }
            // choice == 2 (Skip) or 0 (OK) both fall through; caller typically skips the layer.
#else
            Debug.LogWarning(message + " Skipping.");
#endif
        }

        // Helper: composite a single layer's pixels (already tinted) onto finalPixels
        private static void CompositeLayerOntoFinal(
            Color[] layerPixels,
            Sprite sprite,
            ImageStackLayer layer,
            Color[] finalPixels,
            int finalWidth,
            int finalHeight
        )
        {
            if (layerPixels == null || sprite == null || layer == null)
                return;

            int layerWidth = sprite.texture.width;
            int layerHeight = sprite.texture.height;
            Vector2 offset = layer.Offset;
            float scale = layer.Scale;

            int scaledWidth = Mathf.RoundToInt(layerWidth * scale);
            int scaledHeight = Mathf.RoundToInt(layerHeight * scale);

            for (int destY = 0; destY < scaledHeight; destY++)
            {
                for (int destX = 0; destX < scaledWidth; destX++)
                {
                    int finalX = destX + Mathf.RoundToInt(offset.x);
                    int finalY = destY + Mathf.RoundToInt(offset.y);

                    if (finalX < 0 || finalX >= finalWidth || finalY < 0 || finalY >= finalHeight)
                        continue;

                    int sourceX = Mathf.FloorToInt(destX / scale);
                    int sourceY = Mathf.FloorToInt(destY / scale);

                    sourceX = Mathf.Clamp(sourceX, 0, layerWidth - 1);
                    sourceY = Mathf.Clamp(sourceY, 0, layerHeight - 1);

                    int layerPixelIndex = sourceY * layerWidth + sourceX;
                    if (layerPixelIndex >= layerPixels.Length)
                        continue;

                    Color src = layerPixels[layerPixelIndex];

                    int finalPixelIndex = finalY * finalWidth + finalX;
                    if (finalPixelIndex >= finalPixels.Length)
                        continue;

                    Color dst = finalPixels[finalPixelIndex];

                    float srcAlpha = src.a;
                    float dstAlpha = dst.a * (1 - srcAlpha);
                    float outAlpha = srcAlpha + dstAlpha;

                    if (outAlpha > 0)
                    {
                        finalPixels[finalPixelIndex] = new Color(
                            (src.r * srcAlpha + dst.r * dstAlpha) / outAlpha,
                            (src.g * srcAlpha + dst.g * dstAlpha) / outAlpha,
                            (src.b * srcAlpha + dst.b * dstAlpha) / outAlpha,
                            outAlpha
                        );
                    }
                }
            }
        }

        // Helper: composite two aligned pixel buffers element-wise (no scaling or offsets)
        private static void CompositePixelsElementwise(Color[] finalPixels, Color[] layerPixels)
        {
            if (finalPixels == null || layerPixels == null)
                return;
            int len = Mathf.Min(finalPixels.Length, layerPixels.Length);
            for (int i = 0; i < len; i++)
            {
                Color src = layerPixels[i];
                Color dst = finalPixels[i];

                float srcAlpha = src.a;
                float dstAlpha = dst.a * (1 - srcAlpha);
                float outAlpha = srcAlpha + dstAlpha;

                if (outAlpha > 0)
                {
                    finalPixels[i] = new Color(
                        (src.r * srcAlpha + dst.r * dstAlpha) / outAlpha,
                        (src.g * srcAlpha + dst.g * dstAlpha) / outAlpha,
                        (src.b * srcAlpha + dst.b * dstAlpha) / outAlpha,
                        outAlpha
                    );
                }
                else
                {
                    finalPixels[i] = new Color(0, 0, 0, 0);
                }
            }
        }
    }
}
