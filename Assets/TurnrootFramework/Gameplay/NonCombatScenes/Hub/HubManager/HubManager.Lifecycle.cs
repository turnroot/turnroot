using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        #region Unity Lifecycle

        private void OnEnable()
        {
            if (ValidateRequired(InputProvider, nameof(InputProvider), nameof(OnEnable)))
            {
                InputProvider.OnInput += HandleInput;
            }
        }

        private void OnDisable()
        {
            if (ValidateRequired(InputProvider, nameof(InputProvider), nameof(OnDisable)))
            {
                InputProvider.OnInput -= HandleInput;
            }

            // Reset cursor state in case look mode was active when this component disabled.
            RestoreLookCursorState();
        }

        private bool _hubInitialized = false;

        public void Start()
        {
            gameDate = GameplayGeneralSettings.Instance.StartingGameDate;

            _brain = FindFirstObjectByType<Brain.Brain>();

            if (!ValidateRequired(_brain, nameof(_brain), nameof(Start)))
            {
                "Critical error: Brain not found.".LogWarning();
                return;
            }

            if (LoadingScreen == null)
            {
                LoadingScreen = FindFirstObjectByType<LoadingScreenController>();
            }

            if (dock == null)
            {
                dock = FindFirstObjectByType<Dock>();
            }

            if (shopsManager == null)
            {
                shopsManager = FindFirstObjectByType<ShopsManager>();
            }

            if (_brain.ltm != null && _brain.ltm.Initialized)
            {
                InitializeHubForCurrentDate();
            }
            else
            {
                _brain.OnLongTermMemoryInitialized += HandleLongTermMemoryInitialized;
            }
        }

        private void HandleLongTermMemoryInitialized()
        {
            _brain.OnLongTermMemoryInitialized -= HandleLongTermMemoryInitialized;
            InitializeHubForCurrentDate();
        }

        private void InitializeHubForCurrentDate()
        {
            if (_hubInitialized)
            {
                return;
            }

            _hubInitialized = true;

            var ltm = _brain.ltm;
            if (ltm != null && ltm.Initialized)
            {
                var storedDate = ltm.GetGameDate();
                if (storedDate == GameDate.Default)
                {
                    // First load ever
                    gameDate = GameplayGeneralSettings.Instance.StartingGameDate;
                    ltm.SetGameDate(gameDate.year, (Month)(gameDate.month - 1), gameDate.day);
                    $"HubManager: No saved game date found, using starting date {gameDate.year}/{gameDate.month}/{gameDate.day}".LogInfo();
                }
                else
                {
                    gameDate = storedDate;
                }
            }

            // Ensure all hub state is deterministic for this day.
            HubDayStateStore.Initialize(_brain, gameDate);
            HubDayRandom.Initialize(HubDayStateStore.Seed);

            var hasProcessed = HubDayStateStore.HasProcessedDailyUpdates;
            if (!hasProcessed)
            {
                dock.UpdateDailyVoyageStatuses();
                CheckShipsDocked();
                CheckRareItems();
                HubDayStateStore.MarkDailyUpdatesProcessed(_brain);
            }

            Initialize();
        }

        public void Initialize()
        {
            if (
                !ValidateRequired(
                    nameof(Initialize),
                    (_brain, nameof(_brain)),
                    (_brain?.charactersBrain, "_brain.charactersBrain"),
                    (_brain?.audioBrain, "_brain.audioBrain"),
                    (_brain?.saveFileBrain, "_brain.saveFileBrain")
                )
            )
            {
                return;
            }

            _brain.OnGameDateChanged += HandleGameDateChanged;
            _brain.OnCharacterBirthdayThisWeek += HandleCharacterBirthdayThisWeek;
            UpdateDateText();
            _brain.charactersBrain.CheckBirthdays();

            _brain.audioBrain.SetMusic(HubBackgroundMusic);

            pastShipDockedStatuses = LoadDockShipStatuses();

            if (!HubDayStateStore.HasProcessedDailyUpdates)
            {
                dock.UpdateDailyVoyageStatuses();

                CheckShipsDocked();

                CheckRareItems();
            }
            else
            {
                dock.EnforceCapacityOnLoad();
            }

            UpdateChapterNumberAndNameText(
                _brain.saveFileBrain.ActiveSaveFile.ChapterNumber,
                _brain.saveFileBrain.ActiveSaveFile.ChapterName
            );
            InvokeChapterHubEvents(_brain.saveFileBrain.ActiveSaveFile.ChapterNumber);
            SetInputMode(HubInputMode.Location);

            if (GameplayGeneralSettings.Instance.HubHasTeamLocations)
            {
                var discoveredLocations = FindObjectsByType<HubCharacterSpawnArea>(
                    FindObjectsSortMode.None
                );
                CharacterSpawnAreas = discoveredLocations;
                InitializeTeamLocations();
            }

            CacheSpawnPointHeights();

            if (ExploreChoice != null)
            {
                ExploreChoice.CanBeSelected = true;
            }

            if (BattlefieldsChoice != null)
            {
                BattlefieldsChoice.CanBeSelected = BattleChoiceUi != null;
            }

            if (EndDay != null)
            {
                RefreshEndDayAvailability();
            }
            if (Settings != null)
            {
                Settings.CanBeSelected = true;
            }

            BuildNavigableChoices();
            UpdateChoiceSelection();

            if (GeneralCamera == null || cameraPoints == null || cameraPoints.Length == 0)
            {
                return;
            }

            int idx = HubDayRandom.Range(0, cameraPoints.Length);
            Transform dest = cameraPoints[idx];
            GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
        }

        private void InvokeChapterHubEvents(int currentChapter)
        {
            if (DoThingsAtChapters == null || DoThingsAtChapters.Length == 0)
            {
                return;
            }

            for (int i = 0; i < DoThingsAtChapters.Length; i++)
            {
                var entry = DoThingsAtChapters[i];
                if (entry.Chapter != currentChapter || entry.Event == null)
                {
                    continue;
                }

                entry.Event.Invoke();
            }
        }

        public void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnGameDateChanged -= HandleGameDateChanged;
                _brain.OnCharacterBirthdayThisWeek -= HandleCharacterBirthdayThisWeek;
            }

            EndSettingsMenu();
        }

        #endregion
    }
}
