using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(
    fileName = "New MapGridPointFeatureProperties",
    menuName = "Turnroot/Maps/Tile Feature Properties"
)]
public class MapGridPointFeatureProperties : ScriptableObject
{
    [Header("Feature identity")]
    [Tooltip("ID used to match this asset to a feature type (e.g. 'treasure').")]
    public string featureId = string.Empty;

    [Tooltip("Optional friendly name for the feature defaults asset.")]
    public string featureName = string.Empty;

    [System.Serializable]
    public class StringProperty
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    [System.Serializable]
    public class BoolProperty
    {
        public string key = string.Empty;
        public bool value = false;
    }

    [System.Serializable]
    public class IntProperty
    {
        public string key = string.Empty;
        public int value = 0;
    }

    [System.Serializable]
    public class FloatProperty
    {
        public string key = string.Empty;
        public float value = 0f;
    }

    public List<StringProperty> stringProperties = new();
    public List<BoolProperty> boolProperties = new();
    public List<IntProperty> intProperties = new();
    public List<FloatProperty> floatProperties = new();

    public bool[] DrawProperties()
    {
        bool[] hasValues = new bool[4];
        hasValues[0] = stringProperties.Count > 0;
        hasValues[1] = boolProperties.Count > 0;
        hasValues[2] = intProperties.Count > 0;
        hasValues[3] = floatProperties.Count > 0;
        return hasValues;
    }
}
