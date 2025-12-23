[System.Serializable]
public class MapGridPointFeature
{
    public string typeId = string.Empty;
    public string name = string.Empty;
    public MapGridPointFeatureProperties properties = new();

    public MapGridPointFeature() { }

    public enum FeatureType
    {
        None = 0,
        Treasure,
        Door,
        Warp,
        Healing,
        Ranged,
        Mechanism,
        Control,
        Breakable,
        Shelter,
        Village,
        Fortress,
        Underground,
        Eraser,
    }

    public static FeatureType TypeFromId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return FeatureType.None;
        }

        string fid = id.ToLower();
        if (fid.StartsWith("treasure"))
        {
            return FeatureType.Treasure;
        }

        if (fid.StartsWith("door"))
        {
            return FeatureType.Door;
        }

        if (fid.StartsWith("warp"))
        {
            return FeatureType.Warp;
        }

        if (fid.StartsWith("healing"))
        {
            return FeatureType.Healing;
        }

        if (fid.StartsWith("ranged"))
        {
            return FeatureType.Ranged;
        }

        if (fid.StartsWith("mechanism"))
        {
            return FeatureType.Mechanism;
        }

        return fid.StartsWith("control")
            ? FeatureType.Control
            : fid.StartsWith("breakable") ? FeatureType.Breakable
            : fid.StartsWith("shelter") ? FeatureType.Shelter
            : fid.StartsWith("village") ? FeatureType.Village
            : fid.StartsWith("fortress") ? FeatureType.Fortress
            : fid.StartsWith("underground") ? FeatureType.Underground
            : fid.StartsWith("eraser") ? FeatureType.Eraser
            : FeatureType.None;
    }

    public static string IdFromType(FeatureType t)
    {
        return t switch
        {
            FeatureType.Treasure => "treasure",
            FeatureType.Door => "door",
            FeatureType.Warp => "warp",
            FeatureType.Healing => "healing",
            FeatureType.Ranged => "ranged",
            FeatureType.Mechanism => "mechanism",
            FeatureType.Control => "control",
            FeatureType.Breakable => "breakable",
            FeatureType.Shelter => "shelter",
            FeatureType.Village => "village",
            FeatureType.Fortress => "fortress",
            FeatureType.Underground => "underground",
            FeatureType.Eraser => "eraser",
            _ => string.Empty,
        };
    }

    // Helper: map a feature type id string to a single-letter marker used by the editor overlay.
    public static string GetFeatureLetter(string typeId)
    {
        if (string.IsNullOrEmpty(typeId))
        {
            return null;
        }

        string fid = typeId.ToLower();
        return fid.StartsWith("treasure") ? "T"
            : fid.StartsWith("door") ? "D"
            : fid.StartsWith("warp") ? "W"
            : fid.StartsWith("healing") ? "H"
            : fid.StartsWith("ranged") ? "R"
            : fid.StartsWith("mechanism") ? "M"
            : fid.StartsWith("control") ? "C"
            : fid.StartsWith("breakable") ? "B"
            : fid.StartsWith("shelter") ? "S"
            : fid.StartsWith("underground") ? "U"
            : fid.StartsWith("village") ? "V"
            : fid.StartsWith("fortress") ? "F"
            : "?";
    }
}
