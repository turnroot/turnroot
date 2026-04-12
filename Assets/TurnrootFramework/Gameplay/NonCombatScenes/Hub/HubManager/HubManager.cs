using System;
using NaughtyAttributes;
using TMPro;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.UI.Components.Notifications;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(UiInputProvider))]
    [RequireComponent(typeof(HubTeamLocations))]
    [RequireComponent(typeof(HubSubInput))]
    [RequireComponent(typeof(SpecificUiHandler))]
    /// <remarks>
    /// This may need editing for your project, but if you aren't making major logic changes, you should
    /// be able to wrangle it to work for you just with UI changes and inspector stuff
    /// </remarks>
    public partial class HubManager : MonoBehaviour
    {
        #region Fields

        [BoxGroup("Core")]
        [HideInInspector]
        public Brain.Brain _brain;

        [BoxGroup("Core")]
        [Tooltip("Text element used to display the current hub date (day/month/year).")]
        public TextMeshProUGUI dateText;

        [BoxGroup("Core")]
        [InfoBox("Selectable UI elements corresponding to each hub location")]
        public UiChoice[] LocationChoices;

        [BoxGroup("Core")]
        public UiChoice EndDay;

        [BoxGroup("Core")]
        public UiChoice Settings;

        [BoxGroup("Core")]
        [InfoBox("Loading screen controller used during scene transitions")]
        public LoadingScreenController LoadingScreen;

        // Runtime list used for navigation (locations + end day + settings).
        private UiChoice[] _navigableChoices;

        [BoxGroup("Core")]
        [InfoBox("Input provider used for navigating hub choices.")]
        public UiInputProvider InputProvider;

        [BoxGroup("Core")]
        [InfoBox("Prefab containing the menu canvas used while settings is open.")]
        public GameObject MenuCanvasPrefab;

        [BoxGroup("Core")]
        public AudioClip HubBackgroundMusic;

        private GameObject _menuCanvasInstance;
        private bool _settingsMenuOpen;
        private Action _menuDepthChangedHandler;

        [HorizontalLine()]
        [BoxGroup("Camera & Fade")]
        [Tooltip("Fade used when returning from a sublocation back to the hub.")]
        public UIFade HubFadeToBlack;

        [BoxGroup("Camera & Fade")]
        [Tooltip(
            "The main overlay/HUD to hide when opening any vendor UI and restore when closing it."
        )]
        public UIFade MainOverlayUiFade;

        [BoxGroup("Camera & Fade")]
        [Tooltip("Fade used to show/hide the hub action UI.")]
        public UIFade HubActionsFade;

        [BoxGroup("Camera & Fade")]
        [Tooltip("Fade used to show/hide the back button UI.")]
        public UIFade BackButtonFade;

        [BoxGroup("Camera & Fade")]
        [Tooltip("Field of view used for the hub camera when not in a sublocation.")]
        public float HubMainFov;

        [Header("Spawn Point Sampling")]
        [Tooltip("Collider used to sample terrain height for unit spawn points.")]
        public MeshCollider SpawnGroundCollider;

        [Tooltip("Raycast distance used when sampling spawn-point height.")]
        public float SpawnPointRaycastDistance = 20f;

        [HorizontalLine]
        [BoxGroup("Notifications")]
        public NotificationsHelper notifications;

        [BoxGroup("Notifications")]
        public Dock dock;

        private DockShipStatus[] pastShipDockedStatuses;

        private const string dockShipStatusLtmKey = "Hub_DockedShipStatuses";

        [Serializable]
        private class DockShipStatusContainer
        {
            public DockShipStatus[] statuses;
        }

        [HorizontalLine]
        [BoxGroup("Hub Content")]
        [Tooltip("All sublocation areas that can be visited from the hub.")]
        public HubSubLocation[] subLocations;

        [BoxGroup("Hub Content")]
        public ShopsManager shopsManager;

        [BoxGroup("Hub Content")]
        [Tooltip("UI text used to show the current chapter number and name.")]
        public TextMeshProUGUI ChapterNumberAndNameText;

        [BoxGroup("Hub Content")]
        [Tooltip("Format string used for chapter number/name display.")]
        public string ChapterNumberAndNameFormat = "Chapter {0}: {1}";

        [HideInInspector]
        public GameDate gameDate;

        [BoxGroup("Camera & Fade")]
        [Tooltip("Possible camera positions for randomizing the hub camera on load.")]
        public Transform[] cameraPoints;

        [BoxGroup("Camera & Fade")]
        public Camera GeneralCamera;

        [HorizontalLine]
        [BoxGroup("Input")]
        [Tooltip("Current input mode for the hub (location selection, sublocation choice, etc.).")]
        public HubInputMode CurrentInputMode = HubInputMode.None;

        public HubInputMode PreviousInputMode { get; private set; } = HubInputMode.None;

        public HubSubLocation CurrentSubLocation { get; private set; }

        public enum HubInputMode
        {
            None,
            Location,
            Chosen,
            MarketChoice,
            CafeChoice,
            Battlefields,
            Docks,
            Training,
        }

        private readonly System.Collections.Generic.Dictionary<
            Transform,
            float
        > _spawnPointHeights = new();

        public void SetCurrentSubLocation(HubSubLocation subLocation)
        {
            CurrentSubLocation = subLocation;

            if (subLocations == null)
            {
                return;
            }

            foreach (var loc in subLocations)
            {
                if (loc == null)
                {
                    continue;
                }

                loc.gameObject.SetActive(loc == subLocation);
            }
        }

        public void TransitionBackToHub(UIFade fadeToBlack = null)
        {
            void DoReturnToHub()
            {
                // Hide  POIs
                if (CurrentSubLocation != null)
                {
                    foreach (var poi in CurrentSubLocation.GetComponentsInChildren<HubPoiUi>())
                    {
                        poi.Hide();
                    }
                }

                SetInputMode(HubInputMode.Location);
                UpdateChoiceSelection();
                UpdateDateText();
                _brain.audioBrain.SetMusic(HubBackgroundMusic);

                if (GeneralCamera != null && cameraPoints != null && cameraPoints.Length > 0)
                {
                    int idx = UnityEngine.Random.Range(0, cameraPoints.Length);
                    Transform dest = cameraPoints[idx];
                    GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
                }

                HubActionsFade.Show();
                GeneralCamera.fieldOfView = HubMainFov;
                BackButtonFade.Hide();

                _brain?.charactersBrain.CheckBirthdays();

                CurrentSubLocation = null;

                if (subLocations != null)
                {
                    foreach (var loc in subLocations)
                    {
                        if (loc != null)
                        {
                            loc.gameObject.SetActive(true);
                        }
                    }
                }
            }

            if (fadeToBlack == null)
            {
                DoReturnToHub();
                return;
            }

            UnityEngine.Events.UnityAction onVisible = null;
            UnityEngine.Events.UnityAction onHidden = null;

            onVisible = () =>
            {
                fadeToBlack.OnVisible.RemoveListener(onVisible);
                DoReturnToHub();
                fadeToBlack.Hide();
            };

            onHidden = () =>
            {
                fadeToBlack.OnHidden.RemoveListener(onHidden);
            };

            fadeToBlack.OnVisible.AddListener(onVisible);
            fadeToBlack.OnHidden.AddListener(onHidden);
            fadeToBlack.Show();
        }

        private int currentIndex = 0;

        private const string birthdayNotificationTypeName = "birthday";
        private const string shipNotificationTypeName = "ship";
        private const string itemNotificationTypeName = "items";

        public void UpdateChapterNumberAndNameText(int chapterNumber, string chapterName)
        {
            if (ChapterNumberAndNameText != null)
            {
                ChapterNumberAndNameText.text = string.Format(
                    ChapterNumberAndNameFormat,
                    chapterNumber,
                    chapterName
                );
            }
        }

        public HubSubInput SublocationInput => GetComponent<HubSubInput>();

        public SpecificUiHandler SpecificUiInputHandler => GetComponent<SpecificUiHandler>();

        #endregion


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

            if (GameplayGeneralSettings.Instance.HubHasTeamLocations)
            {
                GetComponent<HubTeamLocations>().Initialize(_brain, subLocations);
            }
            else
            {
                GetComponent<HubTeamLocations>().gameObject.SetActive(false);
            }

            CacheSpawnPointHeights();

            for (int i = 0; i < subLocations.Length; i++)
            {
                subLocations[i].Initialize(_brain);
                LocationChoices[i].CanBeSelected = subLocations[i].CanBeVisitedToday();
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
