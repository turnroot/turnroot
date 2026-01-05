using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        #region Settings Menu Opening and Core Operations

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

        #endregion

        #region Settings Menu Event Handlers

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
            else if (item.ItemName == "Gameplay")
            {
                _isTransitioning = true;
                StartCoroutine(
                    TransitionToSettingsMenu(settingsMenuLocation, gameSettingsGameplayLocation)
                );
            }
        }

        #endregion

        #region Settings Menu Navigation and Transitions

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
            MenuLocation fromMenuLocation,
            MenuLocation toMenuLocation
        )
        {
            var fromInstance = fromMenuLocation.activeInstance;

            // Hide the source menu
            if (fromInstance.TryGetComponent<UIFade>(out var fromFade))
            {
                fromFade.Hide();
                var fadeDuration = fromFade.lerpTime + 0.1f;
                yield return new WaitForSeconds(fadeDuration);
            }

            // If transitioning from prebattle to settings, destroy prebattle menu
            if (fromMenuLocation == preBattleMenuLocation)
            {
                // Clean up prebattle menu events
                CleanupPreBattleMenu(fromInstance);

                // Destroy prebattle menu
                Destroy(fromInstance);
                fromMenuLocation.activeInstance = null;
            }
            else
            {
                // If transitioning from settings to sub-menu (like graphics), disable parent menu
                // Disable the parent menu's input handling and interaction
                if (fromInstance.TryGetComponent<MenuBase>(out var parentMenu))
                {
                    parentMenu.enabled = false;
                }
                // Also disable the GameObject to prevent any input handling
                fromInstance.SetActive(false);
            }

            // Instantiate target menu if it doesn't exist
            if (toMenuLocation.activeInstance == null)
            {
                toMenuLocation.activeInstance = Instantiate(toMenuLocation.prefab);
            }

            if (!toMenuLocation.activeInstance.TryGetComponent<UIFade>(out var toFade))
            {
                toFade = toMenuLocation.activeInstance.AddComponent<UIFade>();
                toFade.lerpTime = uiSettings.MenuInternalTransitionTime;
            }

            // Set up target menu events
            if (toMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var menu))
            {
                menu.uiBrain = this;
                menu.OnNavigate += HandleGameSettingsMenuNavigate;
                menu.OnItemSelected += HandleGameSettingsMenuSelect;

                // Set up input actions for keyboard/gamepad navigation
                SetupMenuInputActions(menu);

                // Apply colors based on menu style
                ApplyMenuColors(toMenuLocation.activeInstance, toMenuLocation.style);
            }

            // Set up settings UI bindings if this is a settings menu
            SetupSettingsUIBindings(toMenuLocation.activeInstance);

            // Set up settings UI bindings if this is a settings menu
            SetupSettingsUIBindings(toMenuLocation.activeInstance);

            // Increment menu depth to indicate we're in a submenu
            CurrentMenuDepth++;

            // Show the target menu
            toFade.Show();

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

        protected System.Collections.IEnumerator TransitionBackToSettingsMenu(
            MenuLocation currentMenuLocation,
            MenuLocation parentMenuLocation
        )
        {
            var currentInstance = currentMenuLocation.activeInstance;

            // Hide the current menu (e.g., graphics menu)
            if (currentInstance.TryGetComponent<UIFade>(out var currentFade))
            {
                currentFade.Hide();
                var fadeDuration = currentFade.lerpTime + 0.1f;
                yield return new WaitForSeconds(fadeDuration);
            }

            // Clean up current menu events
            if (currentInstance.TryGetComponent<MenuBase>(out var currentMenu))
            {
                currentMenu.OnNavigate -= HandleGameSettingsMenuNavigate;
                currentMenu.OnItemSelected -= HandleGameSettingsMenuSelect;
            }

            // Destroy current menu
            Destroy(currentInstance);
            currentMenuLocation.activeInstance = null;

            // Decrement menu depth
            CurrentMenuDepth = Mathf.Max(0, CurrentMenuDepth - 1);

            // Show the parent menu (settings menu should already exist)
            if (parentMenuLocation.activeInstance != null)
            {
                // Re-enable the parent menu that was disabled
                parentMenuLocation.activeInstance.SetActive(true);
                if (parentMenuLocation.activeInstance.TryGetComponent<MenuBase>(out var parentMenu))
                {
                    parentMenu.enabled = true;
                }

                // Show the parent menu with fade
                if (parentMenuLocation.activeInstance.TryGetComponent<UIFade>(out var parentFade))
                {
                    parentFade.Show();
                }
            }

            _isTransitioning = false;
        }

        #endregion

        #region Menu Cleanup and Styling

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

        #endregion
    }
}
