using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.GameSettings
{
    /// <summary>
    /// Visual presentation styles available for game menus.
    /// </summary>
    public enum MenuStyle
    {
        Pie,
        List,
        Filmstrip,
        Grid,
        None,
    }

    /// <summary>
    /// Identifiers for all menu locations in the game.
    /// </summary>
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
        PrebattleUnitPositionsMenu,
        BattleActionSelectMenu,
    }

    /// <summary>
    /// Represents a menu location in the menu hierarchy with style, prefab, and parent relationship.
    /// </summary>
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
                var visited = new HashSet<MenuLocation>();
                while (current != null)
                {
                    // Detect circular references in the parent chain
                    if (!visited.Add(current))
                    {
                        Debug.LogError(
                            $"Circular parent reference detected in menu hierarchy starting from '{menuName}'."
                        );
                        return -1;
                    }

                    depth++;
                    current = current.parent;
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

        public MenuLocation Clone(MenuLocation newParent) =>
            new(newParent)
            {
                style = style,
                prefab = prefab,
                menuName = menuName,
                parentMenuName = parent?.menuName ?? MenuName.None,
            };
    }

    /// <summary>
    /// Global UI configuration for menus, menu hierarchy, and UI element settings.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GamewideUiSettings",
        menuName = "Turnroot/Game Settings/UI/Gamewide UI Settings"
    )]
    public class GamewideUiSettings : SingletonScriptableObject<GamewideUiSettings>
    {
        [Header("Menus"), HorizontalLine(color: EColor.Blue), SerializeField]
        public List<MenuLocation> allPossibleMenuLocations;

        [
            Header("Portraits"),
            SerializeField,
            Tooltip("Sprite to use when a unit portrait is missing")
        ]
        public Sprite NoPortraitSprite;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Only initialize if list is null or empty to preserve Inspector settings
            if (allPossibleMenuLocations == null || allPossibleMenuLocations.Count == 0)
            {
                InitializeDefaultMenuLocations();
            }
        }

        private void InitializeDefaultMenuLocations()
        {
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
            var gameplayMenu = new MenuLocation(gameSettingsMenu, MenuName.GameplayMenu);
            allPossibleMenuLocations.Add(gameplayMenu);
            // New game + avatar
            var newGameMenu = new MenuLocation(saveFileMenu, MenuName.NewGameMenu);
            allPossibleMenuLocations.Add(newGameMenu);
            var avatarSettingsMenu = new MenuLocation(newGameMenu, MenuName.AvatarSettingsMenu);
            allPossibleMenuLocations.Add(avatarSettingsMenu);

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
            // Pre-battle settings menu: its own menu under PreBattleMenu
            var preBattleSettingsMenu = new MenuLocation(
                preBattleMenu,
                MenuName.PreBattleSettingsMenu
            );
            allPossibleMenuLocations.Add(preBattleSettingsMenu);

            // Pre-battle unit positions menu (Starting Positions). Use Grid style by default.
            var preBattlePositionsMenu = new MenuLocation(
                preBattleMenu,
                MenuName.PrebattleUnitPositionsMenu,
                MenuStyle.None
            );
            allPossibleMenuLocations.Add(preBattlePositionsMenu);
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

        public MenuLocation GetGameSettingsGraphicsMenu() => GetMenuLocation(MenuName.GraphicsMenu);

        public MenuLocation GetGameSettingsGameplayMenu() => GetMenuLocation(MenuName.GameplayMenu);

        public MenuLocation GetGameSettingsMenu() => GetMenuLocation(MenuName.GameSettingsMenu);

        public MenuLocation GetGameSettingsAudioMenu() => GetMenuLocation(MenuName.AudioMenu);

        public MenuLocation GetGameSettingsControlsMenu() => GetMenuLocation(MenuName.ControlsMenu);

        public MenuLocation GetPrebattleMapMenu() => GetMenuLocation(MenuName.PreBattleMapMenu);

        public MenuLocation GetPrebattleUnitsMenu() => GetMenuLocation(MenuName.PreBattleTeamMenu);

        public MenuLocation GetPrebattleUnitPositionsMenu() =>
            GetMenuLocation(MenuName.PrebattleUnitPositionsMenu);

        public MenuLocation GetBattleActionSelectMenu() =>
            GetMenuLocation(MenuName.BattleActionSelectMenu);

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

        [
            Range(0f, 1.5f),
            Tooltip("Fade time for internal menu transitions (should be shorter than MenuFadeTime)")
        ]
        public float MenuInternalTransitionTime = .15f;

        [Tooltip(
            "Additional buffer (seconds) to wait after UIFade.lerpTime when doing menu fade transitions"
        )]
        public float MenuFadeBuffer = 0.1f;

        [Tooltip("Small buffer (seconds) used for brief UI fade hide/show operations")]
        public float UiFadeSmallBuffer = 0.02f;

        [Header("Radial Menu"), HorizontalLine(color: EColor.Yellow)]
        [Tooltip("Default normal color for radial menu segments")]
        public Color RadialMenuNormalColor = Color.white;

        [Tooltip("Default selected color for radial menu segments")]
        public Color RadialMenuSelectedColor = new(1f, 0.8f, 0f);

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

        [Header("Button Settings"), HorizontalLine(color: EColor.Orange)]
        public Color RadialMenuButtonNormalColor = Color.white;
        public Color RadialMenuButtonHoveredColor = Color.cyan;
        public Color RadialMenuButtonSelectedColor = Color.yellow;
        public Color GridListFilmstripButtonNormalColor = Color.white;
        public Color GridListFilmstripButtonHoveredColor = Color.cyan;
        public Color GridListFilmstripButtonSelectedColor = Color.yellow;

        [Range(0f, 1f), Tooltip("Duration for button color transitions")]
        public float ButtonTransitionDuration = 0.12f;

        [Space, Tooltip("Prefab for menu canvas with back button that appears in menu states")]
        public GameObject MenuCanvasPrefab;

        [
            Header("Battle Graphics"),
            HorizontalLine(color: EColor.Pink),
            Range(0.1f, 10f),
            InfoBox(
                "Scale applied to models on the battle map- units, weapons, objects, effects, all of it.You'll want to adjust this based on the size of your map grids. Obviously, you need to keep your scale consistent in your 3DCC program!"
            )
        ]
        public float ModelsScale = 5f;
        public Sprite PathTipSprite; // the end of the path
        public Sprite PathPreTipSprite; // end -1 of the path
        public Sprite PathStraightSprite;
        public Sprite PathCornerSprite;
        public Sprite PathStartSprite;
        public Sprite CursorSprite;
        public GameObject BattleCursorPrefab;
        public Vector3 BattleCursorOffset = new(0f, 1f, 0.2f);

        [MinValue(0), MaxValue(1)]
        public Vector2 CameraPanSafeZone = new(.25f, .25f);

        [Range(0.1f, 1f)]
        public float CameraPanSpeed = 0.6f;

        [Range(.01f, .3f)]
        public float CameraPanStopDistance = 0.01f;

        [
            Range(0.05f, 2f),
            Tooltip(
                "Duration in seconds for animated camera rotations (clockwise/counterclockwise)"
            )
        ]
        public float CameraRotationDuration = 0.3f;

        [Header("Map Conditions"), HorizontalLine(color: EColor.Blue)]
        public Sprite timeDayImage;
        public Sprite timeNightImage;
        public Sprite timeDawnImage;
        public Sprite timeSunsetImage;
        public Sprite temperatureVeryColdImage;
        public Sprite temperatureVeryHotImage;
        public Sprite isRainingImage;
        public Sprite isSnowingImage;
        public Sprite isFoggyImage;
        public Sprite isStormyImage;
        public Sprite isWindyImage;
        public Sprite clearImage;
        public Sprite isUnderwaterImage;
        public Sprite isSwampyImage;
        public Sprite isUndergroundImage;
        public Sprite isDesertImage;
        public Sprite isRockyImage;
        public Sprite isVolcanicImage;

        [Header("Map Rendering"), HorizontalLine(color: EColor.Blue)]
        [Tooltip("Cell size in pixels used when rendering map images and minimaps")]
        public int MapCellSize = 32;

        [Tooltip("Resources-relative path where map icons are stored (trailing slash optional)")]
        public string MapIconPath = "EditorSettings/MapGridEditorIcons/";

        public Color MapGridLineColor = new(0.3f, 0.3f, 0.3f, 1f);
        public Color MapBlackCellColor = Color.black;
        public Color MapDarkGrayTerrainColor = new(0.3f, 0.3f, 0.3f, 1f);
        public Color MapLightGrayTerrainColor = new(0.2f, 0.2f, 0.2f, 1f);
        public Color MapBlueSpawnColor = new(0.2f, 0.5f, 1f, 1f);

        // Map rendering getters for external consumers
        public int GetMapCellSize() => MapCellSize;

        public string GetMapIconPath() => MapIconPath;

        public Color GetMapGridLineColor() => MapGridLineColor;

        public Color GetMapBlackCellColor() => MapBlackCellColor;

        public Color GetMapDarkGrayTerrainColor() => MapDarkGrayTerrainColor;

        public Color GetMapLightGrayTerrainColor() => MapLightGrayTerrainColor;

        public Color GetMapBlueSpawnColor() => MapBlueSpawnColor;
    }
}
