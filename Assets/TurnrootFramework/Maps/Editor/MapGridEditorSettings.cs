using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Display mode for map features in the editor.
    /// </summary>
    public enum FeatureDisplay
    {
        Icon,
        Initial,
    }

    /// <summary>
    /// Settings asset for customizing the map grid editor appearance and behavior.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MapGridEditorSettings",
        menuName = "Turnroot/Editor Settings/Map Grid Editor Settings"
    )]
    public class MapGridEditorSettings : ScriptableObject
    {
        [Range(0, 3)]
        public int gridThickness = 1;
        public Color gridColor = Color.black;
        public FeatureDisplay featureDisplay = FeatureDisplay.Icon;

        [Header("UI Layout")]
        [Tooltip("Indentation in pixels for property keys under section headers")]
        public int propertyIndent = 12;

        [Header("Selection & Border Colors")]
        [Tooltip("Border color used for selected features.")]
        public Color selectedFeatureBorderColor = Color.magenta;

        [Tooltip("Border color used for selected tiles that do not have a feature.")]
        public Color selectedTileBorderColor = new(0.1f, 0.7f, 0.95f, 1f);

        [Tooltip("Border color used for tiles that have modified properties (not selected).")]
        public Color modifiedPropertyBorderColor = new(1f, 0.75f, 1f, 0.6f);

        [Header("Header Accent")]
        [Tooltip("Color used as background for section headers in the right-hand panel")]
        public Color headerAccentColor = new(0.0f, 0.35f, 0.8f, 0.18f);

        [Header("Per-type Header Accent Colors")]
        [Tooltip("Accent color for boolean property headers")]
        public Color headerAccentBoolColor = new(0.24f, 0.7f, 0.2f, 0.18f);

        // String and Int header accent colors removed (these property types are no longer used)

        [Tooltip("Accent color for float property headers")]
        public Color headerAccentFloatColor = new(0.9f, 0.76f, 0.0f, 0.18f);

        [Tooltip("Accent color for unit property headers")]
        public Color headerAccentUnitColor = new(0.6f, 0.1f, 0.7f, 0.18f);

        [Tooltip("Accent color for object item headers")]
        public Color headerAccentObjectItemColor = new(0.2f, 0.5f, 0.6f, 0.18f);

        [Tooltip("Accent color for event property headers")]
        public Color headerAccentEventColor = new(0.7f, 0.2f, 0.4f, 0.18f);
    }
}
