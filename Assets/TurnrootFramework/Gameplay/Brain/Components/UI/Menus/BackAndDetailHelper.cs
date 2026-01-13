using System;
using Turnroot.Gameplay.Brain;
using Turnroot.UI.Components.SimpleButton;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        // TODO: Back button isn't working!!!
        // Handle both Back and Details button presence based on state
        private void HandleButtonsForState(string stateName)
        {
            HandleBackButtonForState(stateName);
            HandleDetailsButtonForState(stateName);
        }

        private void HandleBackButtonForState(string stateName)
        {
#if UNITY_EDITOR
            Debug.Log($"HandleBackButtonForState: state={stateName}");
#endif

            // Check if we need a back button based on:
            // 1. The current state needs menus, OR
            // 2. We're currently in a submenu (depth > 1)
            bool stateNeedsMenus = System.Array.Exists(
                StateBrain.StatesThatNeedMenus,
                state => state == stateName
            );

            bool inSubmenu = (_menuTracker?.CurrentDepth ?? 0) > 1;

            bool needsBackButton = stateNeedsMenus || inSubmenu;

#if UNITY_EDITOR
            Debug.Log(
                $"  stateNeedsMenus: {stateNeedsMenus}, inSubmenu: {inSubmenu}, needsBackButton: {needsBackButton}"
            );
            Debug.Log($"  _currentMenuCanvasPrefab is null? {_currentMenuCanvasPrefab == null}");
#endif

            if (needsBackButton)
            {
                if (_currentMenuCanvasPrefab == null)
                {
#if UNITY_EDITOR
                    Debug.Log("  -> Creating NEW back button");
#endif
                    CreateBackButton();
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log("  -> Back button already exists, skipping creation");
#endif
                }
            }
            else if (!needsBackButton && _currentMenuCanvasPrefab != null)
            {
#if UNITY_EDITOR
                Debug.Log("  -> Destroying back button (not needed)");
#endif
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
            targetPrefabField.transform.SetParent(null);
            targetPrefabField.name = $"{targetPrefabField.name}_{role}";

            // Find the SimpleButton component
            var simpleButton = targetPrefabField.GetComponentInChildren<SimpleButton>();
            if (simpleButton == null)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    $"UiBrain: No SimpleButton found in MenuCanvasPrefab for role {role}"
                );
#endif
                return;
            }

            // CRITICAL: Set the role BEFORE doing anything else
            simpleButton.Role = role;

            // Assign input action
            if (role == SimpleButtonRole.Back)
            {
                simpleButton.AssignSelectAction(InputActionFactory.CreateBack());
            }
            else if (role == SimpleButtonRole.Details)
            {
                simpleButton.AssignSelectAction(InputActionFactory.CreateDetails());
            }

            // Wire the handler - THIS IS THE CRITICAL PART
            if (handler != null)
            {
                // Remove any existing subscription first to prevent duplicates
                try
                {
                    simpleButton.OnSelected -= handler;
                }
                catch { }
                // Now add it
                simpleButton.OnSelected += handler;

#if UNITY_EDITOR
                Debug.Log($"UiBrain: Subscribed {role} handler.");
#endif
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
#if UNITY_EDITOR
            Debug.Log(
                $"UiBrain: Back button pressed. Transitioning: {_isTransitioning}, CanGoBack: {_menuTracker?.CanGoBack()}"
            );
#endif

            if (_isTransitioning)
            {
#if UNITY_EDITOR
                Debug.Log("UiBrain: Ignoring back - currently transitioning");
#endif
                return;
            }

            if (_menuTracker?.CanGoBack() == true)
            {
                var (fromLocation, toLocation) = _menuTracker.PopTransition();

#if UNITY_EDITOR
                Debug.Log(
                    $"UiBrain: Back navigation from {fromLocation?.menuName} to {toLocation?.menuName}"
                );
                Debug.Log(
                    $"  from.activeInstance: {(fromLocation?.activeInstance != null ? "EXISTS" : "NULL")}"
                );
                Debug.Log(
                    $"  to.activeInstance: {(toLocation?.activeInstance != null ? "EXISTS" : "NULL")}"
                );
#endif
                if (fromLocation != null && toLocation != null)
                {
                    TransitionToSubmenu(fromLocation, toLocation, isBackNavigation: true);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("UiBrain: Back navigation failed - null locations");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("UiBrain: At root level, handling root back");
#endif
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
