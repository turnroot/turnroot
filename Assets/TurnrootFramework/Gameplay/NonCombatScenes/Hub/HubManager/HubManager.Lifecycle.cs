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
            if (InputProvider != null)
            {
                InputProvider.OnInput += HandleInput;
            }
        }

        private void OnDisable()
        {
            if (InputProvider != null)
            {
                InputProvider.OnInput -= HandleInput;
            }
        }

        private bool _hubInitialized = false;

        public void Start()
        {
            gameDate = GameplayGeneralSettings.Instance.StartingGameDate;

            _brain = FindFirstObjectByType<Brain.Brain>();

            if (_brain == null)
            {
                "HubManager: No Brain found".LogError();
                return;
            }

            if (LoadingScreen == null)
            {
                LoadingScreen = FindFirstObjectByType<LoadingScreenController>();
            }

            // If LTM is already ready, initialize immediately; otherwise, wait for the
            // LTM initialization event (this can happen after Start when using some
            // async brain init paths)
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
                    // First load ever: initialize from settings and persist
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
                dock?.UpdateDailyVoyageStatuses();
                CheckShipsDocked();
                CheckRareItems();
                HubDayStateStore.MarkDailyUpdatesProcessed(_brain);
            }

            Initialize();
        }

        public void Initialize()
        {
            _brain.OnGameDateChanged += HandleGameDateChanged;
            _brain.OnCharacterBirthdayThisWeek += HandleCharacterBirthdayThisWeek;
            _brain.OnHubSublocationInputModeChange += HandleSublocationInputModeChange;
            UpdateDateText();
            _brain.charactersBrain.CheckBirthdays();

            _brain.audioBrain.SetMusic(HubBackgroundMusic);

            pastShipDockedStatuses = LoadDockShipStatuses();

            if (!HubDayStateStore.HasProcessedDailyUpdates)
            {
                dock?.UpdateDailyVoyageStatuses();

                CheckShipsDocked();

                CheckRareItems();
            }
            else
            {
                // Daily updates already processed for today; rebuild dock runtime lists and
                // re-enforce capacity so MaxDockedShipsPerSide is respected on hub re-entry
                dock?.EnforceCapacityOnLoad();
            }

            UpdateChapterNumberAndNameText(
                _brain.saveFileBrain.ActiveSaveFile.ChapterNumber,
                _brain.saveFileBrain.ActiveSaveFile.ChapterName
            );
            SetInputMode(HubInputMode.Location);

            // Initialize sublocations and explore locations BEFORE HubTeamLocations.Initialize,
            // so that CanBeVisitedToday() / IsLocked can safely access _brain.
            if (LocationChoices == null || LocationChoices.Length != subLocations.Length)
            {
                $"LocationChoices has {LocationChoices?.Length ?? 0} entries but subLocations has {subLocations.Length}. They must be the same length and in the same order. Check the HubManager inspector.".LogWarning();
            }

            for (int i = 0; i < subLocations.Length; i++)
            {
                subLocations[i].Initialize(_brain);

                if (LocationChoices == null || i >= LocationChoices.Length)
                {
                    $"No LocationChoices entry at index {i} for sublocation '{subLocations[i].LocationName}'. Add a UiChoice for it in the HubManager inspector.".LogWarning();
                    continue;
                }

                LocationChoices[i].CanBeSelected = subLocations[i].CanBeVisitedToday();
            }

            if (ExploreLocations != null)
            {
                foreach (var exploreLoc in ExploreLocations)
                {
                    if (exploreLoc != null)
                    {
                        exploreLoc.Initialize(_brain);
                    }
                }
            }

            if (GameplayGeneralSettings.Instance.HubHasTeamLocations)
            {
                GetComponent<HubTeamLocations>().Initialize(_brain, subLocations, ExploreLocations);
            }
            else
            {
                GetComponent<HubTeamLocations>().gameObject.SetActive(false);
            }

            CacheSpawnPointHeights();

            if (ExploreChoice != null)
            {
                ExploreChoice.CanBeSelected =
                    ExploreLocations != null && ExploreLocations.Length > 0;
            }

            if (EndDay != null)
            {
                EndDay.CanBeSelected = true;
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

        public void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnGameDateChanged -= HandleGameDateChanged;
                _brain.OnCharacterBirthdayThisWeek -= HandleCharacterBirthdayThisWeek;
                _brain.OnHubSublocationInputModeChange -= HandleSublocationInputModeChange;
            }

            // Ensure we clean up any menu canvas / subscriptions when hub is destroyed.
            EndSettingsMenu();
        }

        #endregion
    }
}
