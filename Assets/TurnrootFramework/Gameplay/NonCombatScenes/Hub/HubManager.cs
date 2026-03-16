using NaughtyAttributes;
using TMPro;
using Turnroot.Characters;
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
        [Tooltip("Selectable UI elements corresponding to each hub location.")]
        public UiChoice[] LocationChoices;

        [BoxGroup("Core")]
        [Tooltip("Optional input provider used for navigating hub choices.")]
        public UiInputProvider InputProvider;

        [HorizontalLine()]
        [BoxGroup("Camera & Fade")]
        [Tooltip("Fade used when returning from a sublocation back to the hub.")]
        public UIFade HubFadeToBlack;

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

        [System.Serializable]
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

        // Cache the sampled height for each spawn point transform.
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

            // When entering a sublocation, hide the other hub locations.
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
                // Match hub entry behavior: set mode to location + refresh UI
                SetInputMode(HubInputMode.Location);
                UpdateChoiceSelection();
                UpdateDateText();

                // Re-randomize hub camera position (like initial hub load)
                if (GeneralCamera != null && cameraPoints != null && cameraPoints.Length > 0)
                {
                    int idx = Random.Range(0, cameraPoints.Length);
                    Transform dest = cameraPoints[idx];
                    GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
                }

                HubActionsFade.Show();
                GeneralCamera.fieldOfView = HubMainFov;
                BackButtonFade.Hide();

                // Refresh birthday notifications / other hub notifications
                _brain?.charactersBrain.CheckBirthdays();

                CurrentSubLocation = null;

                // Restore all sublocations when returning to the hub.
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

        public void Start()
        {
            gameDate = GameplayGeneralSettings.Instance.StartingGameDate;

            _brain = FindFirstObjectByType<Brain.Brain>();

            if (_brain == null)
            {
                "HubManager: No Brain found".LogError();
                return;
            }

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
                    IncrementGameDateForHubLoad();
                }
            }

            Initialize();
            for (int i = 0; i < subLocations.Length; i++)
            {
                subLocations[i].Initialize(_brain);
            }
        }

        public void Initialize()
        {
            _brain.OnGameDateChanged += HandleGameDateChanged;
            _brain.OnCharacterBirthdayThisWeek += HandleCharacterBirthdayThisWeek;
            _brain.OnHubSublocationInputModeChange += HandleSublocationInputModeChange;
            UpdateDateText();
            _brain.charactersBrain.CheckBirthdays();

            pastShipDockedStatuses = LoadDockShipStatuses();

            dock?.UpdateDailyVoyageStatuses();

            CheckShipsDocked();

            CheckRareItems();

            UpdateChapterNumberAndNameText(
                _brain.saveFileBrain.ActiveSaveFile.ChapterNumber,
                _brain.saveFileBrain.ActiveSaveFile.ChapterName
            );
            SetInputMode(HubInputMode.Location);

            if (GameplayGeneralSettings.Instance.HubHasTeamLocations)
            {
                // Initialize team location assignments before initializing individual sublocations.
                GetComponent<HubTeamLocations>().Initialize(_brain, subLocations);
            }
            else
            {
                GetComponent<HubTeamLocations>().gameObject.SetActive(false);
            }

            // Determine ground heights for spawn points (used by HubSubLocation spawn positioning)
            CacheSpawnPointHeights();

            for (int i = 0; i < subLocations.Length; i++)
            {
                subLocations[i].Initialize(_brain);
                LocationChoices[i].CanBeSelected = subLocations[i].CanBeVisitedToday();
            }

            UpdateChoiceSelection();

            if (GeneralCamera == null || cameraPoints == null || cameraPoints.Length == 0)
            {
                return;
            }

            int idx = Random.Range(0, cameraPoints.Length);
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
        }

        #endregion
    }
}
