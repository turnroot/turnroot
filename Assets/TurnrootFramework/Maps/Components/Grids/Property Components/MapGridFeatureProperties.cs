using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Properties for map grid point features (treasure, doors, etc.).
    /// Inherits from the base property system.
    /// This class provides a place to store defaults for map feature assets.
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(
        fileName = "New MapGridFeatureProperties",
        menuName = "Turnroot/Maps/Feature Properties"
    )]
    public class MapGridFeatureProperties : MapGridPropertyBase
    {
        [Header("Feature Identity")]
        [Tooltip("ID used to match this asset to a feature type (e.g. 'treasure').")]
        public string featureId = string.Empty;

        [Tooltip("Optional friendly name for the feature defaults asset.")]
        public string featureName = string.Empty;
    }

    [System.Serializable]
    public class MapGridPointFeatureProperties : MapGridFeatureProperties { }
}
