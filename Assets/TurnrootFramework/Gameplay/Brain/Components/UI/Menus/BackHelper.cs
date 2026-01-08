using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Menu;
using Turnroot.UI.Components.SimpleButton;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
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

        private void CreateBackButton()
        {
            if (uiSettings?.MenuCanvasPrefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("UiBrain: MenuCanvasPrefab is not set in GamewideUiSettings");
#endif
                return;
            }

            _currentMenuCanvasPrefab = Instantiate(uiSettings.MenuCanvasPrefab);

            // Wire up the back button to handle menu navigation
            // Find the SimpleButton component in children since it's a child of the canvas
            var simpleButton = _currentMenuCanvasPrefab.GetComponentInChildren<SimpleButton>();
            if (simpleButton != null && simpleButton.Role == SimpleButtonRole.Back)
            {
                simpleButton.OnSelected += HandleBackButtonPressed;
            }
            else
            {
                // TODO: Handle other button types
            }
        }

        private void DestroyBackButton()
        {
            if (_currentMenuCanvasPrefab != null)
            {
                // Clean up event subscription
                var simpleButton = _currentMenuCanvasPrefab.GetComponentInChildren<SimpleButton>();
                if (simpleButton != null && simpleButton.Role == SimpleButtonRole.Back)
                {
                    simpleButton.OnSelected -= HandleBackButtonPressed;
                }

                Destroy(_currentMenuCanvasPrefab);
                _currentMenuCanvasPrefab = null;
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
