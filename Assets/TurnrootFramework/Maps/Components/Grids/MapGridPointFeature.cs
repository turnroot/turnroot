using System.Collections.Generic;
using UnityEngine;

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
        Underground,
        Eraser,
    }

    public static FeatureType TypeFromId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return FeatureType.None;
        string fid = id.ToLower();
        if (fid.StartsWith("treasure"))
            return FeatureType.Treasure;
        if (fid.StartsWith("door"))
            return FeatureType.Door;
        if (fid.StartsWith("warp"))
            return FeatureType.Warp;
        if (fid.StartsWith("healing"))
            return FeatureType.Healing;
        if (fid.StartsWith("ranged"))
            return FeatureType.Ranged;
        if (fid.StartsWith("mechanism"))
            return FeatureType.Mechanism;
        if (fid.StartsWith("control"))
            return FeatureType.Control;
        if (fid.StartsWith("breakable"))
            return FeatureType.Breakable;
        if (fid.StartsWith("shelter"))
            return FeatureType.Shelter;
        if (fid.StartsWith("underground"))
            return FeatureType.Underground;
        if (fid.StartsWith("eraser"))
            return FeatureType.Eraser;
        return FeatureType.None;
    }

    public static string IdFromType(FeatureType t)
    {
        switch (t)
        {
            case FeatureType.Treasure:
                return "treasure";
            case FeatureType.Door:
                return "door";
            case FeatureType.Warp:
                return "warp";
            case FeatureType.Healing:
                return "healing";
            case FeatureType.Ranged:
                return "ranged";
            case FeatureType.Mechanism:
                return "mechanism";
            case FeatureType.Control:
                return "control";
            case FeatureType.Breakable:
                return "breakable";
            case FeatureType.Shelter:
                return "shelter";
            case FeatureType.Underground:
                return "underground";
            case FeatureType.Eraser:
                return "eraser";
            default:
                return string.Empty;
        }
    }

    // Helper: map a feature type id string to a single-letter marker used by the editor overlay.
    public static string GetFeatureLetter(string typeId)
    {
        if (string.IsNullOrEmpty(typeId))
            return null;
        string fid = typeId.ToLower();
        if (fid.StartsWith("treasure"))
            return "T";
        if (fid.StartsWith("door"))
            return "D";
        if (fid.StartsWith("warp"))
            return "W";
        if (fid.StartsWith("healing"))
            return "H";
        if (fid.StartsWith("ranged"))
            return "R";
        if (fid.StartsWith("mechanism"))
            return "M";
        if (fid.StartsWith("control"))
            return "C";
        if (fid.StartsWith("breakable"))
            return "B";
        if (fid.StartsWith("shelter"))
            return "S";
        if (fid.StartsWith("underground"))
            return "U";
        return "?";
    }
}
