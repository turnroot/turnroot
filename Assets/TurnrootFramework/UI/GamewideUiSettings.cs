using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Utilities;
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
        HubActionsMenu,
        SaveFileMenu,
        GameSettingsMenu,
        GraphicsMenu,
        ExploreMenu,
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
    /// Represents a menu definition (name, prefab, style, and runtime instance).
    /// </summary>
    [System.Serializable]
    public class MenuEntry
    {
        [Tooltip("The type/name of this menu")]
        public MenuName menuName;

        [Tooltip("Visual style for this menu")]
        public MenuStyle style;

        [Tooltip("Prefab to instantiate for this menu")]
        public GameObject prefab;

        // Track the active instance of this menu
        [System.NonSerialized]
        public GameObject activeInstance;
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
        [System.Serializable]
        public class MenuPrefabBinding
        {
            public MenuName menuName;
            public MenuStyle style = MenuStyle.List;
            public GameObject prefab;
        }

        [Header("Menus"), HorizontalLine(color: EColor.Blue)]
        public List<MenuPrefabBinding> MenuPrefabs;

        // Runtime cache of created MenuEntry objects created from MenuPrefabs.
        // This allows the menu system to maintain runtime state (activeInstance, etc.)
        // without requiring a complex serialized hierarchy.
        private readonly Dictionary<MenuName, MenuEntry> _menuEntryCache = new();

        [
            Header("Portraits"),
            SerializeField,
            Tooltip("Sprite to use when a unit portrait is missing")
        ]
        public Sprite NoPortraitSprite;

        [System.Serializable]
        public class ItemTypeIcon
        {
            public ObjectSubtype Subtype;
            public Sprite Icon;
        }

        [Header("Shop Item Type Icons")]
        [Tooltip("Assign icons by object subtype.")]
        public ItemTypeIcon[] ItemTypeIcons;

        [System.Serializable]
        public class LetterIconMapping
        {
            [Tooltip("Sprite for S-letter aptitude")]
            public Sprite S;

            [Tooltip("Sprite for A-letter aptitude")]
            public Sprite A;

            [Tooltip("Sprite for B-letter aptitude")]
            public Sprite B;

            [Tooltip("Sprite for C-letter aptitude")]
            public Sprite C;

            [Tooltip("Sprite for D-letter aptitude")]
            public Sprite D;

            [Tooltip("Sprite for E-letter aptitude")]
            public Sprite E;
        }

        [Header("Aptitude Letter Icons")]
        public LetterIconMapping LetterIcons = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            MenuPrefabs ??= new List<MenuPrefabBinding>();
            _menuEntryCache.Clear();
        }

        public MenuEntry GetMenuEntry(MenuName menuName)
        {
            if (menuName == MenuName.None)
            {
                $"Menu entry for {menuName} not found.".LogError();
                return null;
            }

            if (_menuEntryCache.TryGetValue(menuName, out var cached))
            {
                return cached;
            }

            var binding = MenuPrefabs?.Find(b => b.menuName == menuName);
            var entry = new MenuEntry
            {
                menuName = menuName,
                prefab = binding?.prefab,
                style = binding?.style ?? MenuStyle.List,
            };

            _menuEntryCache[menuName] = entry;
            return entry;
        }

        public MenuEntry GetPreBattleMenu() => GetMenuEntry(MenuName.PreBattleMenu);

        public MenuEntry GetGameSettingsGraphicsMenu() => GetMenuEntry(MenuName.GraphicsMenu);

        public MenuEntry GetGameSettingsGameplayMenu() => GetMenuEntry(MenuName.GameplayMenu);

        public MenuEntry GetGameSettingsMenu() => GetMenuEntry(MenuName.GameSettingsMenu);

        public MenuEntry GetGameSettingsAudioMenu() => GetMenuEntry(MenuName.AudioMenu);

        public MenuEntry GetGameSettingsExploreMenu() => GetMenuEntry(MenuName.ExploreMenu);

        public MenuEntry GetGameSettingsControlsMenu() => GetMenuEntry(MenuName.ControlsMenu);

        public MenuEntry GetPrebattleMapMenu() => GetMenuEntry(MenuName.PreBattleMapMenu);

        public MenuEntry GetPrebattleUnitsMenu() => GetMenuEntry(MenuName.PreBattleTeamMenu);

        public MenuEntry GetHubActionsMenu() => GetMenuEntry(MenuName.HubActionsMenu);

        public MenuEntry GetPrebattleUnitPositionsMenu() =>
            GetMenuEntry(MenuName.PrebattleUnitPositionsMenu);

        public MenuEntry GetBattleActionSelectMenu() =>
            GetMenuEntry(MenuName.BattleActionSelectMenu);

        [Header("Menu Styles"), HorizontalLine(color: EColor.Green)]
        [Range(0f, 10f)]
        public float MenuButtonSpacing = 2f;

        [Range(0f, 1.5f)]
        public float MenuFadeTime = .75f;

        [
            Range(0f, 5f),
            Tooltip("Minimum time (seconds) to show loading screen, even if scene loads faster.")
        ]
        public float MinimumLoadingTime = 0.5f;

        [
            Range(0f, 5f),
            Tooltip("Time (seconds) to wait for loading UI to fade in before starting scene load.")
        ]
        public float LoadingFadeInTime = 0.75f;

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

        public GameObject PassiveSkillOverlayPrefab;
        public GameObject BattleCursorPrefab;
        public Vector3 BattleCursorOffset = new(0f, 1f, 0.2f);

        [MinValue(0), MaxValue(1)]
        public Vector2 CameraPanSafeZone = new(.25f, .25f);

        [Range(0.1f, 1f)]
        public float CameraPanSpeed = 0.6f;

        [Range(0.01f, .3f)]
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
