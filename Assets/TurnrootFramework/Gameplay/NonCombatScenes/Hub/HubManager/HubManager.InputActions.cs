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
            if (currentIndex < 0 || currentIndex >= _navigableChoices.Length)
            {
                return;
            }

            var choice = _navigableChoices[currentIndex];

            // ExploreChoice may be embedded inside LocationChoices or appended after —
            // handle it first regardless of position.
            if (ExploreChoice != null && choice == ExploreChoice)
            {
                OpenExploreMenu();
                return;
            }

            // BattlefieldsChoice may be embedded inside LocationChoices or appended after —
            // handle it first regardless of position.
            if (BattlefieldsChoice != null && choice == BattlefieldsChoice)
            {
                OpenBattleChoice();
                return;
            }

            var locationCount = LocationChoices?.Length ?? 0;

            // Choices inside LocationChoices: map to subLocations, skipping ExploreChoice and BattlefieldsChoice slots.
            if (currentIndex < locationCount)
            {
                // Count how many non-special choices precede currentIndex.
                int subIndex = 0;
                for (int i = 0; i < currentIndex; i++)
                {
                    if (
                        LocationChoices[i] != ExploreChoice
                        && LocationChoices[i] != BattlefieldsChoice
                    )
                    {
                        subIndex++;
                    }
                }

                if (subLocations != null && subIndex < subLocations.Length)
                {
                    var selectedLocation = subLocations[subIndex];
                    if (selectedLocation != null && selectedLocation.CanBeVisitedToday())
                    {
                        selectedLocation.PlayerVisit();
                    }
                }
                return;
            }

            // Choices after LocationChoices.
            // If ExploreChoice was NOT embedded, it sits here before EndDay/Settings.
            bool exploreEmbedded =
                ExploreChoice != null
                && LocationChoices != null
                && System.Array.IndexOf(LocationChoices, ExploreChoice) >= 0;

            bool battlefieldsEmbedded =
                BattlefieldsChoice != null
                && LocationChoices != null
                && System.Array.IndexOf(LocationChoices, BattlefieldsChoice) >= 0;

            int remaining = currentIndex - locationCount;

            if (ExploreChoice != null && !exploreEmbedded)
            {
                if (remaining == 0)
                {
                    OpenExploreMenu();
                    return;
                }
                remaining--;
            }

            if (BattlefieldsChoice != null && !battlefieldsEmbedded)
            {
                if (remaining == 0)
                {
                    OpenBattleChoice();
                    return;
                }
                remaining--;
            }

            if (remaining == 0)
            {
                HandleEndDaySelected();
            }
            else if (remaining == 1)
            {
                OpenSettingsMenu();
            }
        }

        /// <summary>Opens the Explore submenu from the main hub menu.</summary>
        public void OpenExploreMenu()
        {
            HubActionsFade?.Hide();
            SetInputMode(HubInputMode.ExploreMenu);
            OnExploreMenuOpened?.Invoke();
        }

        public void BackFromExploreMenu()
        {
            SetInputMode(HubInputMode.Location);
            HubActionsFade?.Show();
            UpdateChoiceSelection();
        }

        public void OpenBattleChoice()
        {
            if (BattleChoiceUi == null)
            {
                "HubManager: BattleChoiceUi is not assigned.".LogWarning();
                return;
            }

            HubActionsFade?.Hide();
            SetInputMode(HubInputMode.Battlefields);
            BattleChoiceUi.Open(this);
        }

        public void BackFromBattleChoice()
        {
            BattleChoiceUi?.Close();
            SetInputMode(HubInputMode.Location);
            HubActionsFade?.Show();
            UpdateChoiceSelection();
        }

        public void EnterExploreLocation(HubExploreLocation location)
        {
            if (location == null)
            {
                "HubManager: EnterExploreLocation called with a null location.".LogWarning();
                return;
            }

            if (!location.CanBeVisitedToday())
            {
                $"HubManager: {location.LocationName} is locked and cannot be visited.".LogWarning();
                return;
            }

            if (location.Indoors)
            {
                foreach (var effect in OutdoorEffects)
                {
                    if (effect != null)
                    {
                        effect.SetActive(false);
                    }
                }
            }

            location.PlayerVisit();
        }

        private void HandleEndDaySelected()
        {
            // Advance the day (persisted via LTM) and transition via SceneFlowBrain.
            // The EndOfDay scene is responsible for doing end-of-day work and
            // then returning to the hub when ready.
            IncrementForcedBattleDaysSpent();
            IncrementGameDateForHubLoad();
            _brain.storehouseBrain.SaveCurrentStorehouse();
            _brain.storehouseBrain.SaveGoldToLTM();

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
                // Unsubscribe any existing handler to prevent leaks from double-open.
                if (_menuDepthChangedHandler != null)
                {
                    tracker.OnDepthChanged -= _menuDepthChangedHandler;
                }

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
