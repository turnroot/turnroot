using System.Collections;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.UI.Components.SimpleButton;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    public enum MenuType
    {
        Unknown,
        PreBattle,
        Settings,
        Graphics,
        Gameplay,
        Audio,
        Controls,
        Battle,
        Map,
        Team,
    }

    public class MenuTransitionManager
    {
        private readonly UiBrain _brain;
        private readonly GamewideUiSettings _settings;

        // Track menu types for better cleanup
        private MenuType _currentMenuType = MenuType.Unknown;

        public MenuTransitionManager(UiBrain brain, GamewideUiSettings settings)
        {
            _brain = brain;
            _settings = settings;
        }

        public MenuType CurrentMenuType => _currentMenuType;

        private MenuType DetectMenuType(MenuLocation location)
        {
            if (location == null)
            {
                return MenuType.Unknown;
            }

            if (location == _settings?.GetPreBattleMenu())
            {
                return MenuType.PreBattle;
            }

            if (location == _settings?.GetGameSettingsMenu())
            {
                return MenuType.Settings;
            }

            if (location == _settings?.GetGameSettingsGraphicsMenu())
            {
                return MenuType.Graphics;
            }

            if (location == _settings?.GetGameSettingsGameplayMenu())
            {
                return MenuType.Gameplay;
            }

            if (location == _settings?.GetGameSettingsAudioMenu())
            {
                return MenuType.Audio;
            }

            return location == _settings?.GetGameSettingsControlsMenu() ? MenuType.Controls
                : location == _settings?.GetPrebattleMapMenu() ? MenuType.Map
                : location == _settings?.GetPrebattleUnitsMenu() ? MenuType.Team
                : MenuType.Unknown;
        }

        // Simplified TransitionBetween - always destroys source and creates target fresh
        public IEnumerator TransitionBetween(MenuLocation from, MenuLocation to)
        {
            var fromInstance = from?.activeInstance;
#if UNITY_EDITOR
            Debug.Log(
                $"MenuTransitionManager: Transitioning from {from?.menuName} to {to?.menuName}"
            );
#endif

            _currentMenuType = DetectMenuType(to);

            // Hide and destroy source menu
            if (fromInstance != null)
            {
                // If we're leaving the pre-battle unit positions menu, notify listeners (exit positioning mode)
                if (from == _settings?.GetPrebattleUnitPositionsMenu())
                {
                    _brain.GetBrain()?.PublishPositioningModeExited();
                }

                if (fromInstance.TryGetComponent<UIFade>(out var fromFade))
                {
                    fromFade.Hide();
                    yield return new WaitForSeconds(fromFade.lerpTime + 0.1f);
                }

                CleanupMenuEvents(fromInstance);
                Object.Destroy(fromInstance);
                from.activeInstance = null;
            }

            // Create and show target menu fresh
            if (to.prefab != null)
            {
                to.activeInstance = Object.Instantiate(to.prefab);
                SetupMenu(to);
                HandleCreatedMenuInstance(to);

                // If the created menu is the unit positions menu, notify systems that positioning mode entered
                if (to == _settings?.GetPrebattleUnitPositionsMenu())
                {
                    _brain.GetBrain()?.PublishPositioningModeEntered();
                }

                var targetFade = EnsureUIFade(
                    to.activeInstance,
                    _settings.MenuInternalTransitionTime
                );
                targetFade.Show();
            }
        }

        public IEnumerator TransitionToBattle(MenuLocation preBattle)
        {
            // Update current menu type
            _currentMenuType = MenuType.Battle;

            var menuInstance = preBattle?.activeInstance;
            if (menuInstance == null)
            {
                yield break;
            }

            // Start fade out
            if (menuInstance.TryGetComponent<UIFade>(out var uiFade))
            {
                uiFade.Hide();
                yield return new WaitForSeconds(uiFade.lerpTime + 0.1f);
            }

            // Clean up and destroy menu
            CleanupMenuEvents(menuInstance);
            Object.Destroy(menuInstance);
            preBattle.activeInstance = null;

            // Notify brain of completion
            _brain.Brain.PublishPreBattleCompleted();
        }

        private void SetupMenu(MenuLocation location)
        {
            var instance = location.activeInstance;
            var menuType = DetectMenuType(location);
            System.Action<MenuItemBase> itemHandler = menuType switch
            {
                MenuType.PreBattle or MenuType.Map or MenuType.Team =>
                    _brain.HandlePreBattleMenuSelect,
                MenuType.Settings
                or MenuType.Graphics
                or MenuType.Gameplay
                or MenuType.Audio
                or MenuType.Controls => _brain.HandleGameSettingsMenuSelect,
                _ => _brain.HandleMenuSelect,
            };

            var menuBases = instance.GetComponentsInChildren<MenuBase>(true);
            foreach (var menu in menuBases)
            {
                menu.uiBrain = _brain;
                menu.OnItemSelected += itemHandler;
                _brain.SetupMenuInputActions(menu);

                var simpleButtons = menu.GetComponentsInChildren<SimpleButton>(true);
                foreach (var sb in simpleButtons)
                {
                    sb.AssignSelectAction(menu.selectAction);
                }
            }

            if (instance.TryGetComponent<RadialMenu>(out var radial))
            {
                radial.uiBrain = _brain;
                radial.OnItemSelected += itemHandler;
                radial.navigateAction.Enable();

                if (radial.selectAction == null || radial.selectAction.bindings.Count == 0)
                {
                    radial.selectAction?.Disable();
                    radial.selectAction?.Dispose();
                    radial.selectAction = InputActionFactory.CreateSelect();
                }
                else
                {
                    radial.selectAction.Enable();
                }
            }

            _brain.SetupSettingsUIBindings(instance);
            _brain.ApplyMenuColors(instance, location.style);
        }

        private void HandleCreatedMenuInstance(MenuLocation to)
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
            preparationObjectResolver?.Initialize(_brain.GetBrain());
        }

        private void InitializeTeamMenu(GameObject instance)
        {
            var unitColumns = instance.GetComponentInChildren<UnitSelectionColumns>(true);
            unitColumns?.Initialize(_brain.GetBrain());
        }

        private void CleanupMenuEvents(GameObject instance)
        {
            // Clean up MenuBase handlers on all nested menus. NOTE: Keep this limited to
            // menu-related handlers only; do not touch unrelated UI elements such as
            // standalone SimpleButton instances which may live outside of menus.
            var menus = instance.GetComponentsInChildren<MenuBase>(true);
            foreach (var menu in menus)
            {
                // Clean up all possible event handlers that may have been wired in SetupMenu()/SetupPreBattleMenu()
                menu.OnItemSelected -= _brain.HandlePreBattleMenuSelect;
                menu.OnItemSelected -= _brain.HandleGameSettingsMenuSelect;
                menu.OnItemSelected -= _brain.HandleMenuSelect; // ensure general handlers are removed as well
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

        private UIFade EnsureUIFade(GameObject instance, float lerpTime)
        {
            if (!instance.TryGetComponent<UIFade>(out var uiFade))
            {
                uiFade = instance.AddComponent<UIFade>();
            }
            uiFade.lerpTime = lerpTime;
            return uiFade;
        }
    }
}
