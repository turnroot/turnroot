using System;
using Turnroot.GameSettings;
using Turnroot.UI.Components.SimpleButton;
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
            bool stateNeedsMenus = System.Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

            bool inSubmenu = (_menuTracker?.CurrentDepth ?? 0) > 1;

            bool needsBackButton = stateNeedsMenus || inSubmenu;

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
            bool needsDetailsButton = System.Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

            // If the pre-battle menu (radial/pie) is the current active menu, hide Details button
            var preBattleMenu = uiSettings?.GetPreBattleMenu();
            if (
                preBattleMenu?.activeInstance != null
                && _menuTracker?.CurrentMenu == preBattleMenu
                && preBattleMenu.style == MenuStyle.Pie
            )
            {
                TurnrootLogger.Log(
                    "UiBrain: Hiding Details button because pre-battle radial menu is active."
                );
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

        private void CreateBackButton() =>
            CreateRoleButton(
                ref _currentMenuCanvasPrefab,
                SimpleButtonRole.Back,
                HandleBackButtonPressed
            );

        private void DestroyBackButton() =>
            DestroyRoleButton(
                ref _currentMenuCanvasPrefab,
                SimpleButtonRole.Back,
                HandleBackButtonPressed
            );

        private void CreateDetailsButton() =>
            CreateRoleButton(
                ref _currentDetailsCanvasPrefab,
                SimpleButtonRole.Details,
                HandleDetailsButtonPressed
            );

        private void DestroyDetailsButton() =>
            DestroyRoleButton(
                ref _currentDetailsCanvasPrefab,
                SimpleButtonRole.Details,
                HandleDetailsButtonPressed
            );

        // Generic helper to create a canvas prefab and wire a role-specific SimpleButton
        private void CreateRoleButton(
            ref GameObject targetPrefabField,
            SimpleButtonRole role,
            Action handler
        )
        {
            if (uiSettings?.MenuCanvasPrefab == null)
            {
                TurnrootLogger.Log(
                    "UiBrain: MenuCanvasPrefab is not set in GamewideUiSettings",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            targetPrefabField = Instantiate(uiSettings.MenuCanvasPrefab);
            targetPrefabField.transform.SetParent(null);
            targetPrefabField.name = $"{targetPrefabField.name}_{role}";

            // Find all SimpleButton components in the prefab and pick the most appropriate one
            var simpleButtons = targetPrefabField.GetComponentsInChildren<SimpleButton>(true);
            if (simpleButtons == null || simpleButtons.Length == 0)
            {
                TurnrootLogger.Log(
                    $"UiBrain: No SimpleButton found in MenuCanvasPrefab for role {role}",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            // Prefer an existing button that already matches the desired role
            SimpleButton chosen = null;
            foreach (var sb in simpleButtons)
            {
                if (sb.Role == role)
                {
                    chosen = sb;
                    break;
                }
            }

            // If none matched by role, avoid overwriting an existing Back button when creating Details
            if (chosen == null)
            {
                foreach (var sb in simpleButtons)
                {
                    if (role == SimpleButtonRole.Details && sb.Role == SimpleButtonRole.Back)
                    {
                        // skip back buttons when creating details
                        continue;
                    }

                    // choose the first sensible candidate
                    chosen = sb;
                    break;
                }
            }

            // Fallback to first button if still nothing chosen (shouldn't happen)
            chosen ??= simpleButtons[0];

            // Ensure chosen button is marked with the correct role
            chosen.Role = role;

            // Assign the correct input action for the chosen button
            if (role == SimpleButtonRole.Back)
            {
                chosen.AssignSelectAction(InputActionFactory.CreateBack());
            }
            else if (role == SimpleButtonRole.Details)
            {
                chosen.AssignSelectAction(InputActionFactory.CreateDetails());
            }

            if (handler != null)
            {
                chosen.OnSelected -= handler;
            }
        }

        // Generic helper to destroy a canvas prefab and clean up role-specific wiring
        private void DestroyRoleButton(
            ref GameObject targetPrefabField,
            SimpleButtonRole role,
            Action handler
        )
        {
            if (targetPrefabField != null)
            {
                var simpleButton = targetPrefabField.GetComponentInChildren<SimpleButton>();
                if (simpleButton != null && simpleButton.Role == role)
                {
                    if (handler != null)
                    {
                        simpleButton.OnSelected -= handler;
                    }

                    // Dispose of any assigned input action to avoid leaks
                    try
                    {
                        if (simpleButton.SelectAction != null)
                        {
                            simpleButton.SelectAction.Disable();
                            simpleButton.SelectAction.Dispose();
                        }
                    }
                    catch { }
                }

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
                    TurnrootLogger.Log(
                        "UiBrain: Back navigation failed - null locations",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
            else
            {
                TurnrootLogger.Log("UiBrain: At root level, handling root back");
                HandleRootLevelBack();
            }
        }

        private void HandleDetailsButtonPressed()
        {
            if (_isTransitioning)
            {
                return;
            }
            TurnrootLogger.Log("UiBrain: Details button pressed - TODO: Implement details view");
        }

        private void HandleRootLevelBack()
        {
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
    }
}
