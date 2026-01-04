using System.Collections.Generic;

namespace Turnroot.Graphics2D.Tags
{
    public sealed class PortraitLayerTags : LayerTagRegistry<PortraitLayerTags>
    {
        private PortraitLayerTags() { }

        public static readonly List<LayerTag> Tags = new()
        {
            new LayerTag("Hair", 7, true),
            new LayerTag("Left Eyebrow", 6, true),
            new LayerTag("Right Eyebrow", 5, true),
            new LayerTag("Left Eye", 4, true),
            new LayerTag("Right Eye", 3, true),
            new LayerTag("Mouth", 2, true),
            new LayerTag("Nose", 1, true),
            new LayerTag("Face and Shoulders", 0, true),
            new LayerTag("Freckles", 8, false),
            new LayerTag("Blush", 9, false),
            new LayerTag("Beard", 10, false),
            new LayerTag("Mustache", 11, false),
            new LayerTag("Sideburns", 12, false),
            new LayerTag("Wrinkles", 13, false),
            new LayerTag("Necklace", 14, false),
            new LayerTag("Earrings", 15, false),
            new LayerTag("Hat", 16, false),
            new LayerTag("Hair Accessories", 17, false),
            new LayerTag("Glasses", 18, false),
            new LayerTag("Shirt", 19, false),
            new LayerTag("Collar", 20, false),
            new LayerTag("Scarf", 21, false),
            new LayerTag("Scars", 22, false),
            new LayerTag("Birthmarks", 23, false),
            new LayerTag("Tattoos", 24, false),
            new LayerTag("Piercings", 25, false),
            new LayerTag("Makeup", 26, false),
            new LayerTag("Tears", 27, false),
            new LayerTag("Sweat", 28, false),
            new LayerTag("Wounds", 29, false),
        };

        public static new bool TryGet(string name, out LayerTag tag) =>
            LayerTagRegistry<PortraitLayerTags>.TryGet(name, out tag);

        public static new LayerTag Get(string name) => GetConcrete(name);

        public static new bool IsMandatory(string name) =>
            LayerTagRegistry<PortraitLayerTags>.IsMandatory(name);

        public static new int GetOrder(string name) =>
            LayerTagRegistry<PortraitLayerTags>.GetOrder(name);
    }
}
