using UnityEngine;

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

            Texture2D compositedTexture = new Texture2D(
                baseTexture.width,
                baseTexture.height,
                TextureFormat.RGBA32,
                false
            );
            Color[] basePixels = baseTexture.GetPixels();
            Color[] finalPixels = new Color[basePixels.Length];
            System.Array.Copy(basePixels, finalPixels, basePixels.Length);

            // Sort layers by Order property
            var sortedLayers = new ImageStackLayer[layers.Length];
            System.Array.Copy(layers, sortedLayers, layers.Length);
            System.Array.Sort(sortedLayers, (a, b) => a.Order.CompareTo(b.Order));

            for (int layerIndex = 0; layerIndex < sortedLayers.Length; layerIndex++)
            {
                ImageStackLayer layer = sortedLayers[layerIndex];
                if (layer == null || layer.Sprite == null)
                    continue;

                Sprite sprite = layer.Sprite;
                Sprite mask =
                    (masks != null && masks.Length > layerIndex) ? masks[layerIndex] : null;

                // Check if texture is readable
                if (!IsTextureReadable(sprite.texture))
                {
                    Debug.LogWarning(
                        $"Layer {layerIndex} texture is not readable. Enable Read/Write in import settings. Skipping."
                    );
                    continue;
                }

                Color[] layerPixels = sprite.texture.GetPixels();

                // Apply tinting if mask and tints are provided (same tints for all layers)
                if (mask != null && tints != null)
                {
                    layerPixels = TintSpritePixels(sprite, mask, tints);
                    if (layerPixels == null)
                    {
                        Debug.LogWarning($"Failed to tint layer {layerIndex}. Skipping.");
                        continue;
                    }
                }

                // Apply scale, rotation, and offset
                int layerWidth = sprite.texture.width;
                int layerHeight = sprite.texture.height;
                Vector2 offset = layer.Offset;
                float scale = layer.Scale;
                // Note: Rotation not yet implemented - would require matrix transformations

                // Calculate scaled dimensions
                int scaledWidth = Mathf.RoundToInt(layerWidth * scale);
                int scaledHeight = Mathf.RoundToInt(layerHeight * scale);

                // Composite layer onto final image using alpha blending with offset positioning
                // Iterate through destination pixels to avoid gaps when upscaling
                for (int destY = 0; destY < scaledHeight; destY++)
                {
                    for (int destX = 0; destX < scaledWidth; destX++)
                    {
                        // Apply offset to determine position in final texture
                        int finalX = destX + Mathf.RoundToInt(offset.x);
                        int finalY = destY + Mathf.RoundToInt(offset.y);

                        // Check if position is within bounds of final texture
                        if (
                            finalX < 0
                            || finalX >= baseTexture.width
                            || finalY < 0
                            || finalY >= baseTexture.height
                        )
                        {
                            continue;
                        }

                        // Sample from source texture (nearest-neighbor)
                        int sourceX = Mathf.FloorToInt(destX / scale);
                        int sourceY = Mathf.FloorToInt(destY / scale);

                        // Clamp to source bounds
                        sourceX = Mathf.Clamp(sourceX, 0, layerWidth - 1);
                        sourceY = Mathf.Clamp(sourceY, 0, layerHeight - 1);

                        // Get pixel from layer
                        int layerPixelIndex = sourceY * layerWidth + sourceX;
                        if (layerPixelIndex >= layerPixels.Length)
                            continue;

                        Color src = layerPixels[layerPixelIndex];

                        // Get corresponding pixel from final texture
                        int finalPixelIndex = finalY * baseTexture.width + finalX;
                        if (finalPixelIndex >= finalPixels.Length)
                            continue;

                        Color dst = finalPixels[finalPixelIndex];

                        // Standard alpha blending formula
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

            Texture2D compositedTexture = new Texture2D(
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

                Color[] layerPixels = layer.texture.GetPixels();

                // Apply tinting if mask and tints are provided (same tints for all layers)
                if (mask != null && tints != null)
                {
                    layerPixels = TintSpritePixels(layer, mask, tints);
                    if (layerPixels == null)
                    {
                        Debug.LogWarning($"Failed to tint layer {layerIndex}. Skipping.");
                        continue;
                    }
                }

                // Composite layer onto final image using alpha blending
                for (int i = 0; i < finalPixels.Length && i < layerPixels.Length; i++)
                {
                    Color src = layerPixels[i];
                    Color dst = finalPixels[i];

                    // Standard alpha blending formula
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

            compositedTexture.SetPixels(finalPixels);
            compositedTexture.Apply();
            return compositedTexture;
        }

        private static bool IsTextureReadable(Texture2D texture)
        {
            try
            {
                texture.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
