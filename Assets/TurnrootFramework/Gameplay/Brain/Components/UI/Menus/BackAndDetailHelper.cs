using System;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        // Handle both Back and Details button presence based on state
        private void HandleButtonsForState(string stateName)
        {
            HandleBackButtonForState(stateName);
            HandleDetailsButtonForState(stateName);
        }

        private void HandleBackButtonForState(string stateName)
        {
            // Check if we need a back button based on:
            // 1. The current state needs menus, OR
            // 2. We're currently in a submenu (depth > 1)
            bool stateNeedsMenus = Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

            bool inSubmenu = (_menuTracker?.CurrentDepth ?? 0) > 1;

            // Also show Back button at depth 1 when the root settings menu is active,
            // so settings opened from hub can always be closed even if state info is transient.
            bool atRootSettingsMenu =
                (_menuTracker?.CurrentDepth ?? 0) == 1
                && _menuTracker?.CurrentMenu == uiSettings?.GetGameSettingsMenu();

            bool needsBackButton = stateNeedsMenus || inSubmenu || atRootSettingsMenu;

            if (needsBackButton)
            {
                if (_currentMenuCanvasPrefab == null)
                {
                    CreateBackButton();
                }
            }
            else if (!needsBackButton && _currentMenuCanvasPrefab != null)
            {
                DestroyBackButton();
            }
        }

        private void HandleDetailsButtonForState(string stateName)
        {
            bool needsDetailsButton = Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

            // If the pre-battle menu (radial/pie) is the current active menu, hide Details button
            var preBattleMenu = uiSettings?.GetPreBattleMenu();
            if (
                preBattleMenu?.activeInstance != null
                && IsInPreBattleMenu()
                && preBattleMenu.style == MenuStyle.Pie
            )
            {
                "UiBrain: Hiding Details button because pre-battle radial menu is active.".LogInfo();
                needsDetailsButton = false;
            }

            if (needsDetailsButton && _currentDetailsCanvasPrefab == null)
            {
                CreateDetailsButton();
            }
            else if (!needsDetailsButton && _currentDetailsCanvasPrefab != null)
            {
                DestroyDetailsButton();
            }
        }

        private void CreateBackButton()
        {
            CreateRoleCanvas(ref _currentMenuCanvasPrefab, "Back");
            UIInputActionDefaults.Back.performed -= OnBackPerformed;
            UIInputActionDefaults.Back.performed += OnBackPerformed;
            "UiBrain: Subscribed Back handler to UIInputActionDefaults.Back.".LogInfo();
        }

        private void DestroyBackButton()
        {
            UIInputActionDefaults.Back.performed -= OnBackPerformed;
            DestroyRoleCanvas(ref _currentMenuCanvasPrefab);
        }

        private void CreateDetailsButton()
        {
            CreateRoleCanvas(ref _currentDetailsCanvasPrefab, "Details");
            UIInputActionDefaults.ToggleDetails.performed -= OnDetailsPerformed;
            UIInputActionDefaults.ToggleDetails.performed += OnDetailsPerformed;
            "UiBrain: Subscribed Details handler to UIInputActionDefaults.ToggleDetails.".LogInfo();
        }

        private void DestroyDetailsButton()
        {
            UIInputActionDefaults.ToggleDetails.performed -= OnDetailsPerformed;
            DestroyRoleCanvas(ref _currentDetailsCanvasPrefab);
        }

        private void OnBackPerformed(UnityEngine.InputSystem.InputAction.CallbackContext _) =>
            HandleBackButtonPressed();

        private void OnDetailsPerformed(UnityEngine.InputSystem.InputAction.CallbackContext _) =>
            HandleDetailsButtonPressed();

        // Instantiates the visual canvas prefab for a role button (Back or Details).
        private void CreateRoleCanvas(ref GameObject targetPrefabField, string roleName)
        {
            if (uiSettings?.MenuCanvasPrefab == null)
            {
                "UiBrain: MenuCanvasPrefab is not set in GamewideUiSettings".LogWarning();
                return;
            }

            targetPrefabField = Instantiate(uiSettings.MenuCanvasPrefab);
            targetPrefabField.transform.SetParent(null);
            targetPrefabField.name = $"{targetPrefabField.name}_{roleName}";
        }

        // Destroys the visual canvas prefab.
        private void DestroyRoleCanvas(ref GameObject targetPrefabField)
        {
            if (targetPrefabField != null)
            {
                Destroy(targetPrefabField);
                targetPrefabField = null;
            }
        }

        private void HandleBackButtonPressed()
        {
            if (_isTransitioning)
            {
                return;
            }

            // Special case: when only the root settings menu is open,
            // pressing back should close that settings menu.
            var currentMenu = _menuTracker?.CurrentMenu;
            if (_menuTracker?.CurrentDepth == 1 && currentMenu == uiSettings?.GetGameSettingsMenu())
            {
                CloseCurrentMenu(currentMenu);
                _menuTracker.Clear();
                return;
            }

            if (_menuTracker?.CanGoBack() == true)
            {
                var (fromLocation, toLocation) = _menuTracker.PopTransition();
                if (fromLocation != null && toLocation != null)
                {
                    // Start coroutine directly to avoid re-tracking depth on back navigation
                    StartCoroutine(TransitionToSubmenuCoroutine(fromLocation, toLocation));
                }
                else
                {
                    "UiBrain: Back navigation failed - null locations".LogWarning();
                }
            }
            else
            {
                "UiBrain: At root level, handling root back".LogInfo();
                HandleRootLevelBack();
            }
        }

        private void CloseCurrentMenu(MenuEntry current)
        {
            if (current == null)
            {
                return;
            }

            var instance = current.activeInstance;
            if (instance != null)
            {
                // Clean up and destroy the menu instance
                var fade = UIFadeCache.Get(instance);
                fade?.Hide();

                // Clean up any event subscriptions so we don't leak listeners
                var menus = instance.GetComponentsInChildren<MenuBase>(true);
                foreach (var menu in menus)
                {
                    menu.OnItemSelected -= HandlePreBattleMenuSelect;
                    menu.OnItemSelected -= HandleGameSettingsMenuSelect;
                    menu.OnItemSelected -= HandleMenuSelect;
                }

                var radials = instance.GetComponentsInChildren<RadialMenu>(true);
                foreach (var radial in radials)
                {
                    radial.OnItemSelected -= HandlePreBattleMenuSelect;
                    radial.OnItemSelected -= HandleGameSettingsMenuSelect;
                    radial.OnItemSelected -= HandleMenuSelect;
                }

                Destroy(instance);
                UIFadeCache.Remove(instance);
                current.activeInstance = null;
            }
        }

        private void HandleDetailsButtonPressed()
        {
            if (_isTransitioning)
            {
                return;
            }
            "UiBrain: Details button pressed - TODO: Implement details view".LogInfo();
        }

        private void HandleRootLevelBack()
        {
            // If we reached root back while settings is still active, close it.
            if (TryCloseActiveRootSettingsMenu())
            {
                return;
            }

            var currentState = Brain?.stateBrain?.CurrentState?.Name;

            // TODO: Implement root level back behavior based on state
            switch (currentState)
            {
                case BrainStateNames.PreBattle:
                    // TODO: Return to previous state or world map
                    break;
                case BrainStateNames.Paused:
                    Brain.stateBrain.Resume();
                    break;
                case BrainStateNames.MainMenu:
                    // TODO: Exit game or return to previous screen
                    break;
                default:
                    break;
            }
        }

        private bool TryCloseActiveRootSettingsMenu()
        {
            var settingsMenu = uiSettings?.GetGameSettingsMenu();
            if (settingsMenu == null)
            {
                return false;
            }

            var trackedMenu = _menuTracker?.CurrentMenu;
            if (trackedMenu == settingsMenu)
            {
                CloseCurrentMenu(trackedMenu);
                _menuTracker?.Clear();
                return true;
            }

            if (settingsMenu.activeInstance != null)
            {
                CloseCurrentMenu(settingsMenu);
                _menuTracker?.Clear();
                return true;
            }

            return false;
        }
    }
}
