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
        [Header("Menu Styles"), HorizontalLine(color: EColor.Green)]
        public bool PieMenusUseIcons = true;
        public bool PieMenusHaveText = false;
        public MenuStyle BattlePreparationMenuStyle = MenuStyle.Filmstrip;
        public MenuStyle InBattleMenuStyle = MenuStyle.List;
        public MenuStyle InBattleUnitSelectedMenuStyle = MenuStyle.List;

        [Range(0f, 10f)]
        public float MenuButtonSpacing = 2f;
    }
}
