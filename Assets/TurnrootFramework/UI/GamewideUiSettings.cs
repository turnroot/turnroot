using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.GameSettings
{
    public enum MenuStyle
    {
        Pie,
        List,
        Filmstrip,
        Grid,
    }

    [CreateAssetMenu(
        fileName = "GamewideUiSettings",
        menuName = "Turnroot/Game Settings/UI/Gamewide UI Settings"
    )]
    public class GamewideUiSettings : SingletonScriptableObject<GamewideUiSettings>
    {
        public GameObject PreBattleMenuPrefab;

        [Header("Menu Styles"), HorizontalLine(color: EColor.Green)]
        public MenuStyle BattlePreparationMenuStyle = MenuStyle.Pie;
        public MenuStyle InBattleMenuStyle = MenuStyle.List;
        public MenuStyle InBattleUnitSelectedMenuStyle = MenuStyle.List;

        [Range(0f, 10f)]
        public float MenuButtonSpacing = 2f;

        [Range(0f, 1.5f)]
        public float MenuFadeTime = .75f;

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

        [Header("Radial Menu Input")]
        [Range(0f, 1f), Tooltip("Joystick deadzone for radial menu navigation")]
        public float RadialMenuJoystickDeadzone = 0.3f;

        [Range(0f, 2f), Tooltip("Initial delay before navigation repeat starts (seconds)")]
        public float RadialMenuNavigationInitialDelay = 0.4f;

        [Range(0f, 0.5f), Tooltip("Delay between navigation repeats (seconds)")]
        public float RadialMenuNavigationRepeatDelay = 0.08f;

        [Header("Radial Menu Layout")]
        [Range(200f, 2000f), Tooltip("Default radius for radial menus in pixels")]
        public float RadialMenuDefaultRadiusPixels = 800f;
    }
}
