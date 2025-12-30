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
#if UNITY_EDITOR
                Debug.LogError("Cannot create sprite from null texture.");
#endif
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
#if UNITY_EDITOR
                Debug.LogError("Invalid parameters for TintSpritePixels.");
#endif
                return null;
            }

            if (tints == null || tints.Length < 3)
            {
#if UNITY_EDITOR
                Debug.LogError("TintSpritePixels requires 3 tint colors.");
#endif
                return null;
            }

            if (
                mask.texture.width != sprite.texture.width
                || mask.texture.height != sprite.texture.height
            )
            {
#if UNITY_EDITOR
                Debug.LogError("Mask and sprite textures must be the same size.");
#endif
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
                    !Turnroot.Graphics2D.Utilities.TextureValidator.ValidateMatch(
                        sprite.texture,
                        mask.texture,
                        $"Tinting skipped for layer {layerIndex} (sprite='{spriteName}', mask='{maskName}', tag='{layerTag}')"
                    )
                )
                {
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
            var gtex = sprite.texture;
            isGrayscale = Turnroot.Graphics2D.Utilities.TextureValidator.IsGrayscalePNG(gtex);
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
#if UNITY_EDITOR
                Debug.LogError("Invalid parameters for CompositeImageStackLayers.");
#endif
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
                {
                    continue;
                }

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
                {
                    continue;
                }

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
#if UNITY_EDITOR
                Debug.LogError("Invalid parameters for CompositeLayersOnTexture.");
#endif
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
                {
                    continue;
                }

                Sprite mask =
                    (masks != null && masks.Length > layerIndex) ? masks[layerIndex] : null;

                // Validate layer size matches base texture
                if (
                    !Turnroot.Graphics2D.Utilities.TextureValidator.ValidateMatch(
                        baseTexture,
                        layer.texture,
                        $"Layer {layerIndex} size mismatch. Skipping."
                    )
                )
                {
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
#if UNITY_EDITOR
                        Debug.LogWarning($"Failed to tint layer {layerIndex}. Skipping.");
#endif
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
        private static Color[] GetSpritePixelsIfReadable(Sprite sprite) =>
            sprite == null ? null
            : !Turnroot.Graphics2D.Utilities.TextureValidator.EnsureReadable(sprite.texture) ? null
            : sprite.texture.GetPixels();

        // Helper: show an editor popup for a non-readable texture and offer to open the
        // texture asset in the inspector (editor-only). This centralizes the UI so both
        // composition paths behave identically.
        private static void NotifyTextureNotReadable(Texture2D texture, int layerIndex)
        {
            if (texture == null)
            {
                return;
            }

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
#if UNITY_EDITOR
            Debug.LogWarning(message + " Skipping.");
#endif
#endif
        }

        // Helper: alpha-blend a source color over a destination color
        private static Color AlphaBlend(Color src, Color dst)
        {
            float srcAlpha = src.a;
            float dstAlpha = dst.a * (1 - srcAlpha);
            float outAlpha = srcAlpha + dstAlpha;

            return outAlpha > 0
                ? new Color(
                    (src.r * srcAlpha + dst.r * dstAlpha) / outAlpha,
                    (src.g * srcAlpha + dst.g * dstAlpha) / outAlpha,
                    (src.b * srcAlpha + dst.b * dstAlpha) / outAlpha,
                    outAlpha
                )
                : new Color(0, 0, 0, 0);
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
            {
                return;
            }

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
                    {
                        continue;
                    }

                    int sourceX = Mathf.Clamp(Mathf.FloorToInt(destX / scale), 0, layerWidth - 1);
                    int sourceY = Mathf.Clamp(Mathf.FloorToInt(destY / scale), 0, layerHeight - 1);

                    int layerPixelIndex = sourceY * layerWidth + sourceX;
                    if (layerPixelIndex >= layerPixels.Length)
                    {
                        continue;
                    }

                    int finalPixelIndex = finalY * finalWidth + finalX;
                    if (finalPixelIndex >= finalPixels.Length)
                    {
                        continue;
                    }

                    finalPixels[finalPixelIndex] = AlphaBlend(
                        layerPixels[layerPixelIndex],
                        finalPixels[finalPixelIndex]
                    );
                }
            }
        }

        // Helper: composite two aligned pixel buffers element-wise (no scaling or offsets)
        private static void CompositePixelsElementwise(Color[] finalPixels, Color[] layerPixels)
        {
            if (finalPixels == null || layerPixels == null)
            {
                return;
            }

            int len = Mathf.Min(finalPixels.Length, layerPixels.Length);
            for (int i = 0; i < len; i++)
            {
                finalPixels[i] = AlphaBlend(layerPixels[i], finalPixels[i]);
            }
        }
    }
}
