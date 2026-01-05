using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        public void OpenMainGameSettingsMenu()
        {
            if (_isTransitioning)
            {
                return;
            }

            var settingsMenuLocation = uiSettings?.GetGameSettingsMenu();
            if (settingsMenuLocation == null)
            {
                return;
            }

            if (preBattleMenuLocation?.activeInstance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Pre-battle menu instance not found");
#endif
                return;
            }

            // Guard: Return early if activeInstance already exists to prevent duplicates
            if (settingsMenuLocation.activeInstance != null)
            {
                return;
            }

            if (settingsMenuLocation.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: No prefab set for game settings menu location");
#endif
                return;
            }

            _isTransitioning = true;

            // Start the transition coroutine
            StartCoroutine(TransitionToSettingsMenu(preBattleMenuLocation, settingsMenuLocation));
        }

        public void HandleGameSettingsMenuNavigate(MenuItemBase item)
        {
#if UNITY_EDITOR
            Debug.Log($"UiBrain: Navigated to settings item: {item.ItemName}");
#endif
            // TODO: Handle settings menu navigation (highlighting, audio feedback, etc.)
        }

        public void HandleGameSettingsMenuSelect(MenuItemBase item)
        {
#if UNITY_EDITOR
            Debug.Log($"UiBrain: Selected settings item: {item.ItemName}");
#endif
            // TODO: Handle settings item selection based on item.ItemName
            // TODO: Open sub-menus or apply settings changes
            // Note: Back navigation is handled by the existing back button system

            if (item.ItemName == "Graphics")
            {
                _isTransitioning = true;
                StartCoroutine(
                    TransitionToSettingsMenu(settingsMenuLocation, gameSettingsGraphicsLocation)
                );
            }
        }

        public void BackToPreBattleMenu()
        {
            if (_isTransitioning)
            {
                return;
            }

            if (settingsMenuLocation?.activeInstance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Settings menu instance not found");
#endif
                return;
            }

            if (preBattleMenuLocation == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: Pre-battle menu location not found");
#endif
                return;
            }

            if (preBattleMenuLocation.prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogError("UiBrain: No prefab set for pre-battle menu location");
#endif
                return;
            }

            _isTransitioning = true;

            // Start the transition coroutine back to prebattle
            StartCoroutine(
                TransitionBackToPreBattleMenu(settingsMenuLocation, preBattleMenuLocation)
            );
        }

        private System.Collections.IEnumerator TransitionToSettingsMenu(
            MenuLocation preBattleMenuLocation,
            MenuLocation settingsMenuLocation
        )
        {
            var preBattleInstance = preBattleMenuLocation.activeInstance;

            // Hide the prebattle menu
            if (preBattleInstance.TryGetComponent<UIFade>(out var preBattleFade))
            {
                preBattleFade.Hide();
                var fadeDuration = preBattleFade.lerpTime + 0.1f;
                yield return new WaitForSeconds(fadeDuration);
            }

            // Clean up prebattle menu events
            CleanupPreBattleMenu(preBattleInstance);

            // Destroy prebattle menu
            Destroy(preBattleInstance);
            preBattleMenuLocation.activeInstance = null;

            // Instantiate settings menu
            settingsMenuLocation.activeInstance = Instantiate(settingsMenuLocation.prefab);
            if (!settingsMenuLocation.activeInstance.TryGetComponent<UIFade>(out var settingsFade))
            {
                settingsFade = settingsMenuLocation.activeInstance.AddComponent<UIFade>();
                settingsFade.lerpTime = uiSettings.MenuInternalTransitionTime;
            }

            // Set up settings menu events
            if (settingsMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var menu))
            {
                menu.uiBrain = this;
                menu.OnNavigate += HandleGameSettingsMenuNavigate;
                menu.OnItemSelected += HandleGameSettingsMenuSelect;

                // Set up input actions for keyboard/gamepad navigation
                SetupMenuInputActions(menu);

                // Apply colors based on menu style
                ApplyMenuColors(settingsMenuLocation.activeInstance, settingsMenuLocation.style);
            }

            // Increment menu depth to indicate we're in a submenu
            CurrentMenuDepth++;

            // Show the settings menu
            settingsFade.Show();

            _isTransitioning = false;
        }

        private System.Collections.IEnumerator TransitionBackToPreBattleMenu(
            MenuLocation settingsMenuLocation,
            MenuLocation preBattleMenuLocation
        )
        {
            var settingsInstance = settingsMenuLocation.activeInstance;

            // Hide the settings menu
            if (settingsInstance.TryGetComponent<UIFade>(out var settingsFade))
            {
                settingsFade.Hide();
                var fadeDuration = settingsFade.lerpTime + 0.1f;
                yield return new WaitForSeconds(fadeDuration);
            }

            // Clean up settings menu events
            if (settingsInstance.TryGetComponent<MenuBase>(out var menu))
            {
                menu.OnNavigate -= HandleGameSettingsMenuNavigate;
                menu.OnItemSelected -= HandleGameSettingsMenuSelect;
            }

            // Destroy settings menu
            Destroy(settingsInstance);
            settingsMenuLocation.activeInstance = null;

            // Decrement menu depth since we're going back to root level
            CurrentMenuDepth = Mathf.Max(0, CurrentMenuDepth - 1);

            // Instantiate prebattle menu
            preBattleMenuLocation.activeInstance = Instantiate(preBattleMenuLocation.prefab);
            if (
                !preBattleMenuLocation.activeInstance.TryGetComponent<UIFade>(out var preBattleFade)
            )
            {
                preBattleFade = preBattleMenuLocation.activeInstance.AddComponent<UIFade>();
                preBattleFade.lerpTime = uiSettings.MenuInternalTransitionTime;
            }

            // Set up prebattle menu events based on menu style
            var menuStyle = preBattleMenuLocation.style;
            if (menuStyle == MenuStyle.Pie)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<RadialMenu>(
                        out var radialMenu
                    )
                )
                {
                    radialMenu.uiBrain = this;
                    radialMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
                }
            }
            else if (menuStyle == MenuStyle.List || menuStyle == MenuStyle.Grid)
            {
                if (
                    preBattleMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var listMenu)
                )
                {
                    listMenu.uiBrain = this;
                    listMenu.OnNavigate += HandlePreBattleMenuNavigate;
                    listMenu.OnItemSelected += HandlePreBattleMenuSelect;

                    // Set up input actions for keyboard/gamepad navigation
                    SetupMenuInputActions(listMenu);
                }
            }

            // Apply colors based on menu style
            ApplyMenuColors(preBattleMenuLocation.activeInstance, menuStyle);

            // Show the prebattle menu
            preBattleFade.Show();

            _isTransitioning = false;
        }

        private void CleanupPreBattleMenu(GameObject preBattleInstance)
        {
            if (preBattleInstance.TryGetComponent<RadialMenu>(out var radialMenu))
            {
                radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
            }

            if (preBattleInstance.TryGetComponent<MenuBase>(out var menu))
            {
                menu.OnNavigate -= HandlePreBattleMenuNavigate;
                menu.OnItemSelected -= HandlePreBattleMenuSelect;
            }
        }

        private void ApplyMenuColors(GameObject menuInstance, MenuStyle style)
        {
            if (uiSettings == null)
            {
                return;
            }

            // Apply colors based on menu style
            if (style == MenuStyle.Pie)
            {
                // Radial menus already pull colors from GamewideUiSettings automatically
                return;
            }
            else
            {
                // Apply grid/list/filmstrip colors for other menu types
                ApplyGridListFilmstripColors(menuInstance);
            }
        }

        private void ApplyGridListFilmstripColors(GameObject menuInstance)
        {
            // Apply grid/list/filmstrip colors to button components
            var buttons = menuInstance.GetComponentsInChildren<UnityEngine.UI.Button>();
            foreach (var button in buttons)
            {
                var colorBlock = button.colors;
                colorBlock.normalColor = uiSettings.GridListFilmstripButtonNormalColor;
                colorBlock.highlightedColor = uiSettings.GridListFilmstripButtonHoveredColor;
                colorBlock.selectedColor = uiSettings.GridListFilmstripButtonSelectedColor;
                colorBlock.fadeDuration = uiSettings.ButtonTransitionDuration;
                button.colors = colorBlock;
            }
        }

        private void SetupMenuInputActions(MenuBase menu)
        {
            // Force refresh menu items to make sure they're properly detected
            menu.RefreshMenuItems();

            // Create new InputActions with proper bindings for keyboard navigation
            if (menu.navigateUpAction == null || menu.navigateUpAction.bindings.Count == 0)
            {
                menu.navigateUpAction = new UnityEngine.InputSystem.InputAction(
                    "NavigateUp",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.navigateUpAction.AddBinding("<Keyboard>/w");
                menu.navigateUpAction.AddBinding("<Keyboard>/upArrow");
            }

            if (menu.navigateDownAction == null || menu.navigateDownAction.bindings.Count == 0)
            {
                menu.navigateDownAction = new UnityEngine.InputSystem.InputAction(
                    "NavigateDown",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.navigateDownAction.AddBinding("<Keyboard>/s");
                menu.navigateDownAction.AddBinding("<Keyboard>/downArrow");
            }

            if (menu.selectAction == null || menu.selectAction.bindings.Count == 0)
            {
                menu.selectAction = new UnityEngine.InputSystem.InputAction(
                    "Select",
                    UnityEngine.InputSystem.InputActionType.Button
                );
                menu.selectAction.AddBinding("<Keyboard>/enter");
                menu.selectAction.AddBinding("<Keyboard>/space");
            }

            // Enable the actions
            menu.navigateUpAction?.Enable();
            menu.navigateDownAction?.Enable();
            menu.selectAction?.Enable();
        }
    }
}
