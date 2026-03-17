using Turnroot.UI;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Input Actions

        public void HandleLocationInput(string action)
        {
            if (_navigableChoices == null)
            {
                BuildNavigableChoices();
            }

            if (_navigableChoices == null || _navigableChoices.Length == 0)
            {
                "HubManager: No navigable choices assigned".LogError();
                return;
            }

            if (InputProvider != null)
            {
                InputProvider.Navigate(
                    action,
                    _navigableChoices,
                    ref currentIndex,
                    _navigableChoices.Length,
                    OnNavigateSelect
                );
            }
            else
            {
                UiChoiceHandler.HandleNavigation(
                    action,
                    _navigableChoices,
                    ref currentIndex,
                    _navigableChoices.Length,
                    OnNavigateSelect
                );
            }

            UpdateChoiceSelection();
        }

        private void OnNavigateSelect()
        {
            var locationCount = LocationChoices?.Length ?? 0;

            if (currentIndex < 0 || currentIndex >= _navigableChoices.Length)
            {
                return;
            }

            // Location items (first N choices)
            if (currentIndex < locationCount)
            {
                if (currentIndex < subLocations.Length)
                {
                    var selectedLocation = subLocations[currentIndex];
                    if (selectedLocation != null && selectedLocation.CanBeVisitedToday())
                    {
                        selectedLocation.PlayerVisit();
                    }
                }
                return;
            }

            // Extra choices (End Day, Settings)
            var extraIndex = currentIndex - locationCount;
            if (extraIndex == 0)
            {
                HandleEndDaySelected();
            }
            else if (extraIndex == 1)
            {
                OpenSettingsMenu();
            }
        }

        private void HandleEndDaySelected()
        {
            // Advance the day (persisted via LTM) and transition via SceneFlowBrain.
            // The EndOfDay scene is responsible for doing end-of-day work and
            // then returning to the hub when ready.
            IncrementGameDateForHubLoad();

            if (_brain?.sceneFlowBrain != null)
            {
                // Show the shared loading screen (if configured) while the transition happens.
                LoadingScreen?.Show();

                // Flag tells the scene flow graph that the next transition should return to hub
                _brain.sceneFlowBrain.SetCustomFlag(
                    Utilities.SceneFlows.SceneFlowConditionKeys.EndHubDay,
                    true
                );

                var available = _brain.sceneFlowBrain.GetAvailableScenes();
                if (available != null && available.Count > 0)
                {
                    // Use the first available transition (scene flow graph should order this appropriately)
                    _brain.sceneFlowBrain.TransitionToScene(available[0].sceneId);
                    return;
                }

                "HubManager: No available next scene found in SceneFlowBrain".LogWarning();
                return;
            }

            // Fallback (not ideal, but preserves prior behavior).
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
        }

        private void OpenSettingsMenu()
        {
            if (_brain?.uiBrain == null)
            {
                return;
            }

            // Prevent hub navigation while the settings menu is active
            BeginSettingsMenu();

            var settingsLocation = _brain.uiBrain?.uiSettings?.GetGameSettingsMenu();
            if (settingsLocation != null)
            {
                _brain.uiBrain.TransitionToSubmenu(null, settingsLocation);
            }
            else
            {
                "HubManager: GameSettingsMenu location not found".LogWarning();
            }
        }

        private void BeginSettingsMenu()
        {
            if (_settingsMenuOpen)
            {
                return;
            }

            _settingsMenuOpen = true;
            SetInputMode(HubInputMode.None);

            HubActionsFade?.Hide();
            notifications?.HideContainer();

            if (_menuCanvasInstance == null && MenuCanvasPrefab != null)
            {
                _menuCanvasInstance = Instantiate(MenuCanvasPrefab);
                _menuCanvasInstance.transform.SetParent(transform, false);
            }

            var tracker = _brain?.uiBrain?.GetMenuTracker();
            if (tracker != null)
            {
                _menuDepthChangedHandler = () =>
                {
                    if (tracker.CurrentDepth == 0)
                    {
                        EndSettingsMenu();
                    }
                };
                tracker.OnDepthChanged += _menuDepthChangedHandler;
            }
        }

        private void EndSettingsMenu()
        {
            if (!_settingsMenuOpen)
            {
                return;
            }

            _settingsMenuOpen = false;

            HubActionsFade?.Show();
            notifications?.ShowContainer();

            var tracker = _brain?.uiBrain?.GetMenuTracker();
            if (tracker != null && _menuDepthChangedHandler != null)
            {
                tracker.OnDepthChanged -= _menuDepthChangedHandler;
                _menuDepthChangedHandler = null;
            }

            if (_menuCanvasInstance != null)
            {
                Destroy(_menuCanvasInstance);
                _menuCanvasInstance = null;
            }

            SetInputMode(HubInputMode.Location);
            UpdateChoiceSelection();
        }

        #endregion
    }
}
