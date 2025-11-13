namespace Turnroot.Characters
{
    public static class PortraitLayerTags
    {
        public static readonly string[] Mandatory = new[]
        {
            // Note: this list is for identification; use CanonicalFrontToBack for UI/order
            "Face and Shoulders",
            "Hair",
            "Left Eye",
            "Right Eye",
            "Left Eyebrow",
            "Right Eyebrow",
            "Nose",
            "Mouth",
        };

        public static readonly string[] Optional = new[]
        {
            "Freckles",
            "Blush",
            "Beard",
            "Mustache",
            "Sideburns",
            "Wrinkles",
            "Necklace",
            "Earrings",
            "Hat",
            "Hair Accessories",
            "Glasses",
            "Shirt",
            "Collar",
            "Scarf",
            "Scars",
            "Birthmarks",
            "Tattoos",
            "Piercings",
            "Makeup",
            "Tears",
            "Sweat",
            "Wounds",
        };

        public static readonly string[] All;

        // The canonical ordering of mandatory tags from FRONT (top) to BACK (bottom).
        // The UI displays layers top-to-bottom = front-to-back, so the first entry here
        // will appear at the top of the list.
        public static readonly string[] CanonicalFrontToBackMandatory = new[]
        {
            "Hair",
            "Left Eyebrow",
            "Right Eyebrow",
            "Left Eye",
            "Right Eye",
            "Mouth",
            "Nose",
            "Face",
        };

        static PortraitLayerTags()
        {
            var list = new System.Collections.Generic.List<string>();
            list.AddRange(Mandatory);
            list.AddRange(Optional);
            All = list.ToArray();
        }

        public static bool IsMandatory(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return false;
            for (int i = 0; i < Mandatory.Length; i++)
                if (string.Equals(Mandatory[i], tag, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
    }
}
