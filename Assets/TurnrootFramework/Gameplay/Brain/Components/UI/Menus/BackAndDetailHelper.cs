// Deprecated: Back and Details helpers were merged into BackHelper.cs. This file intentionally left blank to avoid duplicate definitions.
using System;
using Turnroot.Gameplay.Brain;
using Turnroot.UI.Components.SimpleButton;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
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
            bool needsBackButton = System.Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

            if (needsBackButton && _currentMenuCanvasPrefab == null)
            {
                CreateBackButton();
            }
            else if (!needsBackButton && _currentMenuCanvasPrefab != null)
            {
                DestroyBackButton();
            }
        }

        private void HandleDetailsButtonForState(string stateName)
        {
            // For now, mirror the Back button behavior (appear when menus are active)
            bool needsDetailsButton = System.Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

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
#if UNITY_EDITOR
                Debug.LogWarning("UiBrain: MenuCanvasPrefab is not set in GamewideUiSettings");
#endif
                return;
            }

            targetPrefabField = Instantiate(uiSettings.MenuCanvasPrefab);

            // Find the SimpleButton component in children since it's a child of the canvas
            var simpleButton = targetPrefabField.GetComponentInChildren<SimpleButton>();
            if (simpleButton != null && simpleButton.Role == role)
            {
                // Assign a role-appropriate input action
                if (role == SimpleButtonRole.Back)
                {
                    simpleButton.AssignSelectAction(InputActionFactory.CreateBack());
                }
                else if (role == SimpleButtonRole.Details)
                {
                    simpleButton.AssignSelectAction(InputActionFactory.CreateDetails());
                }

                // Wire the selection handler
                if (handler != null)
                {
                    simpleButton.OnSelected += handler;
                }
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
                    TransitionToSubmenu(fromLocation, toLocation, isBackNavigation: true);
                }
            }
            else
            {
                // At root level, handle based on current state
                HandleRootLevelBack();
            }
        }

        private void HandleDetailsButtonPressed()
        {
            if (_isTransitioning)
            {
                return;
            }

            // TODO: Implement details behavior when a details button is pressed
#if UNITY_EDITOR
            Debug.Log("UiBrain: Details button pressed - TODO: implement behavior");
#endif
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

#if UNITY_EDITOR
            Debug.Log($"UiBrain: Root level back pressed in state: {currentState}");
#endif
        }
    }
}
