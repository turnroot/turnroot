# ImageCompositor — short ref

Static utility for layering and tinting images. CPU-based, designed for editor use (portraits and badges).

Key APIs
- `TintSpritePixels(sprite, mask, tints)` — tint pixels via mask RGB
- `CompositeImageStackLayers(base, layers, masks, tints)` — scale/offset/blend ordered layers
- `CreateSpriteFromTexture()` — create runtime sprite from Texture2D

Public methods
- `CreateSpriteFromTexture(Texture2D)` — helper to create a sprite from a texture
- `TintSpritePixels(Sprite sprite, Sprite mask, Color[] tints)` — mask-based tinting
- `CompositeImageStackLayers(Texture2D base, ImageStackLayer[] layers, Sprite[] masks, Color[] tints)` — composite several ImageStackLayer entries into a texture

See also
- [StackedImage](../Graphics2D/StackedImage.md) — high-level use of compositor for rendering
- [StackedImageEditorWindow](./StackedImageEditorWindow.md) — editor integration and preview

Notes
- Expect Read/Write enabled textures; masks must match sprite dimensions
- Tuned for editor; heavy use of `GetPixels()` so minimize calls on large textures
