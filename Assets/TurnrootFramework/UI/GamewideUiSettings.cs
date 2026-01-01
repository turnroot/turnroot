using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.GameSettings
{
    
    public enum MenuStyle
    {
        Pie,
        List,
        Filmstrip,
        Row,
        Grid,
        Turnwheel,
    }

    [CreateAssetMenu(
        fileName = "GamewideUiSettings",
        menuName = "Turnroot/Game Settings/UI/Gamewide UI Settings"
    )]
    public class GamewideUiSettings : SingletonScriptableObject<GamewideUiSettings>
    {
        public GameObject PreBattleMenuPrefab;

        [Header("Menu Styles"), HorizontalLine(color: EColor.Green)]
        public MenuStyle BattlePreparationMenuStyle = MenuStyle.Filmstrip;
        public MenuStyle InBattleMenuStyle = MenuStyle.List;
        public MenuStyle InBattleUnitSelectedMenuStyle = MenuStyle.List;

        [Range(0f, 10f)]
        public float MenuButtonSpacing = 2f;

        [Header("Radial Menu"), HorizontalLine(color: EColor.Yellow)]
        [Tooltip("Default normal color for radial menu segments")]
        public Color RadialMenuNormalColor = Color.white;

        [Tooltip("Default selected color for radial menu segments")]
        public Color RadialMenuSelectedColor = new Color(1f, 0.8f, 0f);

        [Range(0f, 1f), Tooltip("Inner radius for radial menus (0-1, percent of total radius)")]
        public float RadialMenuInnerRadius = 0.3f;

        [Range(0f, 0.2f), Tooltip("Gap between radial segments (0-1)")]
        public float RadialMenuSegmentGap = 0.02f;

        [Header("Radial Menu Content")]
        [Tooltip("Show icons in radial menus")]
        public bool RadialMenuHaveIcons = true;

        [Tooltip("Show labels in radial menus")]
        public bool RadialMenuHaveLabels = true;
    }
}
