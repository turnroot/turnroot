# ImageStack — short ref

`ImageStack` is a `ScriptableObject` that stores an ordered list of `ImageStackLayer` entries used for composition (portraits, badges).

ImageStackLayer
- Holds `Sprite`, optional `Mask` (RGB channels map to 3 tint colors), `Offset`, `Scale`, `Order`, and `Tag`.
- The compositor applies tint → scale → offset → blend.

Notes
- Composition performed by `StackedImage.CompositeLayers()`.
- Render size comes from `GraphicsPrototypesSettings`; masks must match sprite dimensions.

Where to look
- `Assets/TurnrootFramework/Graphics2D` and `Tools/ImageCompositor.md`
