using System.Collections;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.UI.Components.SimpleButton;
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

        private MenuType DetectMenuType(MenuLocation location)
        {
            return location switch
            {
                null => MenuType.Unknown,
                var l when l == _settings?.GetPreBattleMenu() => MenuType.PreBattle,
                var l when l == _settings?.GetGameSettingsMenu() => MenuType.Settings,
                var l when l == _settings?.GetGameSettingsGraphicsMenu() => MenuType.Graphics,
                var l when l == _settings?.GetGameSettingsGameplayMenu() => MenuType.Gameplay,
                var l when l == _settings?.GetGameSettingsAudioMenu() => MenuType.Audio,
                var l when l == _settings?.GetPrebattleMapMenu() => MenuType.Map,
                var l when l == _settings?.GetPrebattleUnitsMenu() => MenuType.Team,
                _ => MenuType.Unknown,
            };
        }

        public IEnumerator TransitionBetween(MenuLocation from, MenuLocation to)
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
                    _brain.Brain?.PublishPositioningModeEntered();
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

        private void SetupMenu(MenuLocation location)
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
