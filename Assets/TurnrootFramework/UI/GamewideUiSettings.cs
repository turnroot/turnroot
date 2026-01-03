using System.Collections.Generic;
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

    public enum MenuName
    {
        None,
        MainMenu,
        SaveFileMenu,
        GameSettingsMenu,
        GraphicsMenu,
        AudioMenu,
        ControlsMenu,
        GameplayMenu,
        NewGameMenu,
        AvatarSettingsMenu,
        PreBattleMenu,
        PreBattleTeamMenu,
        PreBattleItemsMenu,
        PreBattleSkillsMenu,
        PreBattleSettingsMenu,
        PreBattleMapMenu,
        PreBattleSupportMenu,
    }

    [System.Serializable]
    public class MenuLocation
    {
        [Tooltip("Parent menu location (null for root menus)")]
        [System.NonSerialized]
        public MenuLocation parent;

        [Tooltip("Parent menu by name - set this to establish hierarchy")]
        public MenuName parentMenuName;

        [Tooltip("The type/name of this menu")]
        public MenuName menuName;

        [Tooltip("Visual style for this menu")]
        public MenuStyle style;

        [Tooltip("Prefab to instantiate for this menu")]
        public GameObject prefab;

        // Track the active instance of this menu
        [System.NonSerialized]
        public GameObject activeInstance;

        [HideInInspector]
        public int Depth
        {
            get
            {
                int depth = 0;
                var current = parent;
                while (current != null)
                {
                    depth++;
                    current = current.parent;
                    if (depth > 10)
                    {
                        break;
                    }
                }
                return depth;
            }
        }

        public MenuLocation(
            MenuLocation parent = null,
            MenuName menuName = MenuName.MainMenu,
            MenuStyle style = MenuStyle.List
        )
        {
            this.parent = parent;
            parentMenuName = parent?.menuName ?? MenuName.None;
            this.menuName = menuName == MenuName.None ? MenuName.MainMenu : menuName;
            this.style = style;
        }

        public MenuLocation Clone(MenuLocation parent) =>
            new(parent)
            {
                style = style,
                prefab = prefab,
                menuName = menuName,
            };
    }

    [CreateAssetMenu(
        fileName = "GamewideUiSettings",
        menuName = "Turnroot/Game Settings/UI/Gamewide UI Settings"
    )]
    public class GamewideUiSettings : SingletonScriptableObject<GamewideUiSettings>
    {
        [Header("Menus"), HorizontalLine(color: EColor.Blue), SerializeField]
        public List<MenuLocation> allPossibleMenuLocations;

        public GamewideUiSettings()
        {
            // Only initialize if list is null or empty to preserve Inspector settings
            if (allPossibleMenuLocations == null || allPossibleMenuLocations.Count == 0)
            {
                InitializeDefaultMenuLocations();
            }
        }

        private void InitializeDefaultMenuLocations()
        {
            // TODO: Verify these are correctly hierached
            allPossibleMenuLocations = new List<MenuLocation>();

            // Main menu
            var mainMenu = new MenuLocation();
            allPossibleMenuLocations.Add(mainMenu);
            var saveFileMenu = new MenuLocation(mainMenu, MenuName.SaveFileMenu);
            allPossibleMenuLocations.Add(saveFileMenu);
            var gameSettingsMenu = new MenuLocation(mainMenu, MenuName.GameSettingsMenu);
            allPossibleMenuLocations.Add(gameSettingsMenu);
            // Game settings
            var graphicsMenu = new MenuLocation(gameSettingsMenu, MenuName.GraphicsMenu);
            allPossibleMenuLocations.Add(graphicsMenu);
            var audioMenu = new MenuLocation(gameSettingsMenu, MenuName.AudioMenu);
            allPossibleMenuLocations.Add(audioMenu);
            var controlsMenu = new MenuLocation(gameSettingsMenu, MenuName.ControlsMenu);
            allPossibleMenuLocations.Add(controlsMenu);
            var gameplayMenu = new MenuLocation(gameSettingsMenu, MenuName.GameplayMenu);
            allPossibleMenuLocations.Add(gameplayMenu);
            // New game + avatar
            var newGameMenu = new MenuLocation(saveFileMenu, MenuName.NewGameMenu);
            allPossibleMenuLocations.Add(newGameMenu);
            var avatarSettingsMenu = new MenuLocation(newGameMenu, MenuName.AvatarSettingsMenu);
            allPossibleMenuLocations.Add(avatarSettingsMenu);

            // TODO: Fill out world map, hub, etc
            // Pre-battle
            var preBattleMenu = new MenuLocation(
                menuName: MenuName.PreBattleMenu,
                style: MenuStyle.Pie
            );
            allPossibleMenuLocations.Add(preBattleMenu);
            var preBattleTeamMenu = new MenuLocation(preBattleMenu, MenuName.PreBattleTeamMenu);
            allPossibleMenuLocations.Add(preBattleTeamMenu);
            var preBattleItemsMenu = new MenuLocation(preBattleMenu, MenuName.PreBattleItemsMenu);
            allPossibleMenuLocations.Add(preBattleItemsMenu);
            var preBattleSkillsMenu = new MenuLocation(preBattleMenu, MenuName.PreBattleSkillsMenu);
            allPossibleMenuLocations.Add(preBattleSkillsMenu);
            var preBattleMapMenu = new MenuLocation(preBattleMenu, MenuName.PreBattleMapMenu);
            allPossibleMenuLocations.Add(preBattleMapMenu);
            var preBattleSupportMenu = new MenuLocation(
                preBattleMenu,
                MenuName.PreBattleSupportMenu
            );
            allPossibleMenuLocations.Add(preBattleSupportMenu);
            // Pre-battle settings menu is the game settings menu
            var preBattleSettingsMenu = gameSettingsMenu.Clone(parent: preBattleMenu);
            allPossibleMenuLocations.Add(preBattleSettingsMenu);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure list is initialized in the Editor
            if (allPossibleMenuLocations == null || allPossibleMenuLocations.Count == 0)
            {
                InitializeDefaultMenuLocations();
            }

            // Resolve parent references from parentMenuName
            ResolveParentReferences();
        }
#endif

        // Helper methods to find menu locations
        public MenuLocation GetMenuLocation(MenuName menuName) =>
            allPossibleMenuLocations?.Find(m => m.menuName == menuName);

        public List<MenuLocation> GetChildMenus(MenuLocation parent)
        {
            return allPossibleMenuLocations == null
                ? new List<MenuLocation>()
                : allPossibleMenuLocations.FindAll(m => m.parent == parent);
        }

        public MenuLocation GetPreBattleMenu() => GetMenuLocation(MenuName.PreBattleMenu);

        public MenuLocation GetGameSettingsMenu() => GetMenuLocation(MenuName.GameSettingsMenu);

        public void ResolveParentReferences()
        {
            if (allPossibleMenuLocations == null)
            {
                return;
            }

            foreach (var location in allPossibleMenuLocations)
            {
                // Prevent self-parenting
                if (location.parentMenuName == location.menuName)
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"Menu '{location.menuName}' cannot be its own parent. Setting parent to None."
                    );
#endif
                    location.parentMenuName = MenuName.None;
                }

                // Set parent reference based on parentMenuName
                location.parent =
                    location.parentMenuName == MenuName.None
                        ? null
                        : GetMenuLocation(location.parentMenuName);
            }
        }

        [Header("Menu Styles"), HorizontalLine(color: EColor.Green)]
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
