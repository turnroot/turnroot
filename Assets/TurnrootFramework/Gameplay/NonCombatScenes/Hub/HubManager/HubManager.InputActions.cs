using Turnroot.UI;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        private struct TraversalStartContext
        {
            public UnityEngine.Transform TraversalPoint;
            public Character.HubCharacterManager CharacterManager;
        }

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

            if (
                !ValidateRequired(
                    nameof(OnNavigateSelect),
                    (ExploreChoice, nameof(ExploreChoice)),
                    (BattlefieldsChoice, nameof(BattlefieldsChoice)),
                    (EndDay, nameof(EndDay)),
                    (Settings, nameof(Settings)),
                    (Exit, nameof(Exit))
                )
            )
            {
                return;
            }

            if (choice == ExploreChoice)
            {
                OpenExploreTraversal();
                return;
            }

            switch (choice)
            {
                case var _ when choice == BattlefieldsChoice:
                    OpenBattleChoice();
                    return;
                case var _ when choice == EndDay:
                    HandleEndDaySelected();
                    return;
                case var _ when choice == Settings:
                    OpenSettingsMenu();
                    return;
                case var _ when choice == Exit:
                    _ = HandleExitSelected();
                    return;
            }

            $"HubManager: Selected choice '{choice.name}' is not mapped to a hub action.".LogWarning();
        }

        public void OpenExploreTraversal()
        {
            HubActionsFade?.Hide();

            if (_brain != null && !HubDayStateStore.HasSeenExploreTutorial(_brain))
            {
                OpenExploreTutorial();
                return;
            }

            BeginExploreTraversal();
        }

        private void OpenExploreTutorial()
        {
            if (ExploreTutorialPrefab == null)
            {
                BeginExploreTraversal();
                return;
            }

            var instance = Instantiate(ExploreTutorialPrefab);
            if (!instance.TryGetComponent<HubExploreTutorialHandler>(out var handler))
            {
                "HubManager: ExploreTutorialPrefab does not contain a HubExploreTutorialHandler component.".LogWarning();
                Destroy(instance);
                BeginExploreTraversal();
                return;
            }

            handler.Completed = BeginExploreTraversal;
        }

        public void BeginExploreTraversal()
        {
            "HubManager: Starting explore traversal.".LogInfo();
            HubActionsFade?.Hide();

            var contextResult = TryBuildTraversalStartContext();
            if (!contextResult.Success)
            {
                $"HubManager: Failed to start traversal. {contextResult.Error}".LogError();
                HubActionsFade?.Show();
                SetInputMode(HubInputMode.Location);
                UpdateChoiceSelection();
                return;
            }

            var context = contextResult.Value;

            if (GeneralCamera != null)
            {
                GeneralCamera.transform.SetPositionAndRotation(
                    context.TraversalPoint.position,
                    context.TraversalPoint.rotation
                );
            }

            SetInputMode(HubInputMode.Traversal);
            BackButtonFade?.Show();

            context.CharacterManager.HandleTraversalEntered(
                context.TraversalPoint,
                CurrentLocationName ?? HubSublocationName.Market
            );
        }

        private OperationResult<TraversalStartContext> TryBuildTraversalStartContext()
        {
            var characterManager = GetHubCharacterManager();
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(GeneralCamera, nameof(GeneralCamera)),
                OperationResultGuards.RequireNotNull(characterManager, "HubCharacterManager")
            );
            if (!validation.Success)
            {
                return OperationResult<TraversalStartContext>.Failure(validation.ErrorMessage);
            }

            var traversalPoint = ResolveTraversalEntryPoint();
            if (traversalPoint == null)
            {
                return OperationResult<TraversalStartContext>.Failure(
                    "No traversal entry point is configured. Assign TraversalStartAvatarPoint or a valid TeleportPoint."
                );
            }

            var context = new TraversalStartContext
            {
                TraversalPoint = traversalPoint,
                CharacterManager = characterManager,
            };

            return OperationResult<TraversalStartContext>.SuccessResult(context);
        }

        private UnityEngine.Transform ResolveTraversalEntryPoint()
        {
            if (TraversalStartAvatarPoint != null)
            {
                return TraversalStartAvatarPoint;
            }

            if (CurrentTraversalAvatarPoint != null)
            {
                return CurrentTraversalAvatarPoint;
            }

            if (
                !ValidateRequiredNotNullOrEmpty(
                    TeleportPoints,
                    nameof(TeleportPoints),
                    nameof(ResolveTraversalEntryPoint)
                )
            )
            {
                return null;
            }

            for (int i = 0; i < TeleportPoints.Length; i++)
            {
                var teleportPoint = TeleportPoints[i];
                if (
                    !ValidateRequired(
                        teleportPoint.Point,
                        $"{nameof(TeleportPoints)}[{i}].Point",
                        nameof(ResolveTraversalEntryPoint)
                    )
                )
                {
                    continue;
                }

                SetCurrentLocation(teleportPoint);
                return teleportPoint.Point;
            }

            return null;
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
        }

        private OperationResult HandleExitSelected()
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(_brain, nameof(_brain)),
                OperationResultGuards.RequireNotNull(
                    _brain?.sceneFlowBrain,
                    "_brain.sceneFlowBrain"
                ),
                OperationResultGuards.RequireNotNull(
                    _brain?.sceneFlowBrain?.sceneFlowGraph,
                    "_brain.sceneFlowBrain.sceneFlowGraph"
                )
            );
            if (!validation.Success)
            {
                return validation;
            }

            LoadingScreen?.Show();
            _brain.sceneFlowBrain.TransitionToScene(
                _brain.sceneFlowBrain.sceneFlowGraph.StartingSceneId
            );
            return OperationResult.Successful();
        }

        private void OpenSettingsMenu()
        {
            if (_brain?.uiBrain == null)
            {
                return;
            }

            // Prevent hub navigation while the settings menu is active
            BeginSettingsMenu();

            if (ValidateRequired(_brain.uiBrain.uiSettings, "UI Brain is not assigned."))
            {
                var settingsLocation = _brain.uiBrain.uiSettings.GetGameSettingsMenu();
                if (settingsLocation != null)
                {
                    _brain.uiBrain.TransitionToSubmenu(null, settingsLocation);
                }
                else
                {
                    "HubManager: GameSettingsMenu location not found".LogWarning();
                }
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
            if (ValidateRequired(tracker, "MenuTracker", nameof(BeginSettingsMenu)))
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
