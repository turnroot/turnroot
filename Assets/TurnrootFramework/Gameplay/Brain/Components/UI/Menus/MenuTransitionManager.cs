using System.Collections;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Defines the types of menus available in the game.
    /// </summary>
    public enum MenuType
    {
        Unknown,
        PreBattle,
        Settings,
        Graphics,
        Gameplay,
        Explore,
        Audio,
        Battle,
        Map,
        Team,
    }

    /// <summary>
    /// Manages transitions and animations between different menu states.
    /// </summary>
    public class MenuTransitionManager
    {
        private readonly UiBrain _brain;
        private readonly GamewideUiSettings _settings;

        public MenuTransitionManager(UiBrain brain, GamewideUiSettings settings)
        {
            _brain = brain;
            _settings = settings;
        }

        public MenuType CurrentMenuType { get; private set; } = MenuType.Unknown;

        private MenuType DetectMenuType(MenuEntry entry)
        {
            return entry switch
            {
                null => MenuType.Unknown,
                var e when e == _settings?.GetPreBattleMenu() => MenuType.PreBattle,
                var e when e == _settings?.GetGameSettingsMenu() => MenuType.Settings,
                var e when e == _settings?.GetGameSettingsGraphicsMenu() => MenuType.Graphics,
                var e when e == _settings?.GetGameSettingsGameplayMenu() => MenuType.Gameplay,
                var e when e == _settings?.GetGameSettingsAudioMenu() => MenuType.Audio,
                var e when e == _settings?.GetGameSettingsExploreMenu() => MenuType.Explore,
                var e when e == _settings?.GetPrebattleMapMenu() => MenuType.Map,
                var e when e == _settings?.GetPrebattleUnitsMenu() => MenuType.Team,
                _ => MenuType.Unknown,
            };
        }

        public IEnumerator TransitionBetween(MenuEntry from, MenuEntry to)
        {
            var fromInstance = from?.activeInstance;
            CurrentMenuType = DetectMenuType(to);

            if (fromInstance != null)
            {
                if (from == _settings.GetPrebattleUnitPositionsMenu())
                {
                    _brain.Brain.PublishPositioningModeExited();
                }

                var fromFade = UIFadeCache.Get(fromInstance);
                if (fromFade != null)
                {
                    fromFade.Hide();
                    yield return new WaitForSeconds(fromFade.lerpTime + 0.1f);
                }

                CleanupMenuEvents(fromInstance);
                Object.Destroy(fromInstance);
                from.activeInstance = null;
                UIFadeCache.Remove(fromInstance);
            }

            if (to.prefab != null)
            {
                to.activeInstance = Object.Instantiate(to.prefab);
                SetupMenu(to);
                HandleCreatedMenuInstance(to);

                if (to == _settings?.GetPrebattleUnitPositionsMenu())
                {
                    _brain.Brain.PublishPositioningModeEntered();
                }

                var targetFade = EnsureUIFade(
                    to.activeInstance,
                    _settings.MenuInternalTransitionTime
                );
                targetFade.Show();
            }
        }

        public IEnumerator TransitionToBattle(MenuEntry preBattle)
        {
            // Update current menu type
            CurrentMenuType = MenuType.Battle;

            var menuInstance = preBattle?.activeInstance;
            if (menuInstance == null)
            {
                yield break;
            }

            // Start fade out
            var uiFade = UIFadeCache.Get(menuInstance);
            if (uiFade != null)
            {
                uiFade.Hide();
                yield return new WaitForSeconds(uiFade.lerpTime + 0.1f);
            }

            // Clean up and destroy menu
            CleanupMenuEvents(menuInstance);
            Object.Destroy(menuInstance);
            preBattle.activeInstance = null;
            UIFadeCache.Remove(menuInstance);

            // Notify brain of completion
            _brain.Brain.PublishPreBattleCompleted();
        }

        private void SetupMenu(MenuEntry location)
        {
            var instance = location.activeInstance;
            var menuType = DetectMenuType(location);
            System.Action<MenuItemBase> itemHandler = menuType switch
            {
                MenuType.PreBattle or MenuType.Map or MenuType.Team =>
                    _brain.HandlePreBattleMenuSelect,
                MenuType.Settings or MenuType.Graphics or MenuType.Gameplay or MenuType.Audio =>
                    _brain.HandleGameSettingsMenuSelect,
                _ => _brain.HandleMenuSelect,
            };

            var menuBases = instance.GetComponentsInChildren<MenuBase>(true);
            foreach (var menu in menuBases)
            {
                menu.uiBrain = _brain;

                // Ensure input actions are set up for keyboard/gamepad navigation.
                // This is especially important when menus are instantiated dynamically.
                _brain.SetupMenuInputActions(menu);

                // Ensure the menu has fresh items and an initial selection.
                menu.RefreshMenuItems();
                if (menu.menuItems.Count > 0)
                {
                    menu.SetSelection(0);
                }

                // Set up any PanelRows-based submenus (used by settings screens)
                // PanelRows now uses shared UIInputActionDefaults directly.
                var panelRows =
                    instance.GetComponentsInChildren<UI.Components.Menu.Submenu.PanelRows>(true);
                foreach (var rows in panelRows)
                {
                    rows.Initialize();
                }

                menu.OnItemSelected += itemHandler;
            }

            if (instance.TryGetComponent<RadialMenu>(out var radial))
            {
                radial.uiBrain = _brain;
                radial.OnItemSelected += itemHandler;
            }

            _brain.SetupSettingsUIBindings(instance);
            _brain.ApplyMenuColors(instance, location.style);
        }

        private void HandleCreatedMenuInstance(MenuEntry to)
        {
            var instance = to.activeInstance;

            // Initialize components for known special menus (no-op when component not present)
            InitializePreparationObjectResolver(instance);
            InitializeTeamMenu(instance);
        }

        private void InitializePreparationObjectResolver(GameObject instance)
        {
            var preparationObjectResolver =
                instance.GetComponentInChildren<PreparationObjectResolver>(true);
            preparationObjectResolver?.Initialize(_brain.Brain);
        }

        private void InitializeTeamMenu(GameObject instance)
        {
            var unitColumns = instance.GetComponentInChildren<UnitSelectionColumns>(true);
            unitColumns?.Initialize(_brain.Brain);
        }

        private void CleanupMenuEvents(GameObject instance)
        {
            var menus = instance.GetComponentsInChildren<MenuBase>(true);
            foreach (var menu in menus)
            {
                menu.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                menu.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
                menu.OnItemSelected -= _brain.HandleMenuSelect;
            }

            // Clean up any nested RadialMenu handlers too
            var radials = instance.GetComponentsInChildren<RadialMenu>(true);
            foreach (var radial in radials)
            {
                radial.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                radial.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
                radial.OnItemSelected -= _brain.HandleMenuSelect;
            }
        }

        private UIFade EnsureUIFade(GameObject instance, float lerpTime) =>
            UIFadeCache.GetOrCreate(instance, lerpTime);
    }
}
