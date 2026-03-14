using TMPro;
using Turnroot.Characters;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
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
    public class HubManager : MonoBehaviour
    {
        #region Fields
        [HideInInspector]
        public Brain.Brain _brain;
        public TextMeshProUGUI dateText;
        public UiChoice[] LocationChoices;
        public UiInputProvider InputProvider;

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

        public HubInputMode CurrentInputMode = HubInputMode.None;
        public HubInputMode PreviousInputMode { get; private set; } = HubInputMode.None;
        public HubSubLocation CurrentSubLocation { get; private set; }

        [Tooltip("Assigned fade used when returning from a sublocation back to the hub")]
        public UIFade HubFadeToBlack;
        public UIFade HubActionsFade;
        public UIFade BackButtonFade;
        public float HubMainFov;

        public void SetCurrentSubLocation(HubSubLocation subLocation) =>
            CurrentSubLocation = subLocation;

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

        public NotificationsHelper notifications;

        public Dock dock;
        private DockShipStatus[] pastShipDockedStatuses;

        private const string dockShipStatusLtmKey = "Hub_DockedShipStatuses";

        [System.Serializable]
        private class DockShipStatusContainer
        {
            public DockShipStatus[] statuses;
        }

        public HubSubLocation[] subLocations;

        public TextMeshProUGUI ChapterNumberAndNameText;
        public string ChapterNumberAndNameFormat = "Chapter {0}: {1}";
        private const string birthdayNotificationTypeName = "birthday";
        private const string shipNotificationTypeName = "ship";

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

        [HideInInspector]
        public GameDate gameDate;

        public Transform[] cameraPoints;
        public Camera GeneralCamera;

        public HubSubInput SublocationInput => GetComponent<HubSubInput>();

        public SpecificUiHandler SpecificUiInputHandler => GetComponent<SpecificUiHandler>();

        #endregion

        #region Input Actions
        public void HandleLocationInput(string action)
        {
            if (subLocations == null || subLocations.Length == 0)
            {
                "HubManager: No sublocations assigned".LogError();
                return;
            }

            if (InputProvider != null)
            {
                InputProvider.Navigate(
                    action,
                    LocationChoices,
                    ref currentIndex,
                    LocationChoices?.Length ?? 0,
                    () =>
                    {
                        var selectedLocation = subLocations[currentIndex];
                        if (selectedLocation.CanBeVisitedToday())
                        {
                            selectedLocation.PlayerVisit();
                        }
                    }
                );
            }
            else
            {
                UiChoiceHandler.HandleNavigation(
                    action,
                    LocationChoices,
                    ref currentIndex,
                    LocationChoices?.Length ?? 0,
                    () =>
                    {
                        var selectedLocation = subLocations[currentIndex];
                        if (selectedLocation.CanBeVisitedToday())
                        {
                            selectedLocation.PlayerVisit();
                        }
                    }
                );
            }

            UpdateChoiceSelection();
        }

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

            UpdateChapterNumberAndNameText(
                _brain.saveFileBrain.ActiveSaveFile.ChapterNumber,
                _brain.saveFileBrain.ActiveSaveFile.ChapterName
            );
            SetInputMode(HubInputMode.Location);

            for (int i = 0; i < subLocations.Length; i++)
            {
                subLocations[i].Initialize(_brain);
                LocationChoices[i].CanBeSelected = subLocations[i].CanBeVisitedToday();
            }

            UpdateChoiceSelection();
            if (GameplayGeneralSettings.Instance.HubHasTeamLocations)
            {
                GetComponent<HubTeamLocations>().Initialize();
            }
            else
            {
                GetComponent<HubTeamLocations>().gameObject.SetActive(false);
            }

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

        #region Brain Event Handlers

        private void HandleSublocationInputModeChange(HubInputMode mode)
        {
            SetInputMode(mode);
        }

        #endregion

        #region Input Handling

        private void HandleInput(string action)
        {
            switch (CurrentInputMode)
            {
                case HubInputMode.Location:
                    HandleLocationInput(action);
                    break;
                case HubInputMode.MarketChoice:
                case HubInputMode.CafeChoice:
                case HubInputMode.Docks:
                case HubInputMode.Training:
                case HubInputMode.Battlefields:
                    SublocationInput.HandleSubLocationInput(action);
                    break;
                case HubInputMode.Chosen:
                    SpecificUiInputHandler.HandleInput(action);
                    break;
            }
        }

        public void SetInputMode(HubInputMode mode)
        {
            if (mode != CurrentInputMode)
            {
                PreviousInputMode = CurrentInputMode;
            }

            CurrentInputMode = mode;
            currentIndex = 0;

            bool allowLook = mode switch
            {
                HubInputMode.Location => false,
                HubInputMode.MarketChoice => true,
                HubInputMode.CafeChoice => true,
                HubInputMode.Battlefields => false,
                HubInputMode.Docks => true,
                HubInputMode.Training => true,
                HubInputMode.Chosen => false,
                HubInputMode.None => false,
                _ => false,
            };

            SublocationInput.SetLookEnabled(allowLook);
        }

        public void RevertToPreviousInputMode()
        {
            if (PreviousInputMode == CurrentInputMode)
            {
                return;
            }

            SetInputMode(PreviousInputMode);
        }

        private void IncrementGameDateForHubLoad()
        {
            if (_brain?.ltm == null)
            {
                return;
            }

            GameDate current = _brain.ltm.GetGameDate();
            var dt = new System.DateTime(current.year, current.month, current.day);
            dt = dt.AddDays(1);

            _brain.ltm.SetGameDate(dt.Year, (Month)(dt.Month - 1), dt.Day);
            gameDate = new GameDate(dt.Year, dt.Month, dt.Day);
        }

        #endregion


        #region Helpers
        public void UpdateDateText()
        {
            if (dateText != null)
            {
                Month month = (Month)Mathf.Clamp(gameDate.month - 1, 0, 11);
                string daySuffix = GameDate.GetDaySuffix(gameDate.day);
                string monthName = month.ToString();
                dateText.text = $"{monthName} {gameDate.day}{daySuffix}";
            }
        }

        private void UpdateChoiceSelection()
        {
            if (LocationChoices == null || LocationChoices.Length == 0)
            {
                return;
            }

            for (int i = 0; i < LocationChoices.Length; i++)
            {
                if (LocationChoices[i] == null)
                {
                    continue;
                }

                if (i == currentIndex)
                {
                    LocationChoices[i].Select();
                }
                else
                {
                    LocationChoices[i].Deselect();
                }
            }
        }
        #endregion

        #region Event Handlers
        public void HandleGameDateChanged(int year, int month, int day)
        {
            gameDate = new GameDate(year, month, day);
            _brain.charactersBrain.CheckBirthdays();
            $"HubManager: Game date changed to {gameDate.year}/{gameDate.month}/{gameDate.day}".LogInfo();
        }

        public void HandleCharacterBirthdayThisWeek(CharacterInstance character, GameDate date)
        {
            int bdDay = character.CharacterTemplate.BirthdayDay;
            int bdMonth = character.CharacterTemplate.BirthdayMonth;

            string message =
                $"It's <b>{character.CharacterTemplate.DisplayName}</b>'s birthday this week, on the {bdDay}{GameDate.GetDaySuffix(bdDay)}";

            if (gameDate.day == bdDay && gameDate.month == bdMonth)
            {
                message = $"Today is <b>{character.CharacterTemplate.DisplayName}</b>'s birthday!";
            }

            notifications.SetMessage(message);
            foreach (var type in notifications.types)
            {
                if (
                    type.category.ToLower() == birthdayNotificationTypeName
                    || type.name.ToLower() == birthdayNotificationTypeName
                )
                {
                    notifications.Send(System.Array.IndexOf(notifications.types, type));
                    break;
                }
            }
        }

        public void CheckShipsDocked()
        {
            var statuses = dock.PublishDockedShipStatuses();
            if (statuses == null || statuses.Length == 0)
            {
                return;
            }

            // Ensure we have a cached baseline; if none exists, treat all as undocked (so first check can notify correctly).
            if (pastShipDockedStatuses == null || pastShipDockedStatuses.Length == 0)
            {
                pastShipDockedStatuses = new DockShipStatus[statuses.Length];
                for (int i = 0; i < statuses.Length; i++)
                {
                    pastShipDockedStatuses[i] = new DockShipStatus
                    {
                        ShipName = statuses[i].ShipName,
                        IsDocked = false,
                    };
                }
            }

            bool anyChange = false;

            for (int i = 0; i < statuses.Length; i++)
            {
                var current = statuses[i];
                var previous = System.Array.Find(
                    pastShipDockedStatuses,
                    s => s.ShipName == current.ShipName
                );

                bool wasDocked = previous.ShipName != null && previous.IsDocked;

                if (current.IsDocked != wasDocked)
                {
                    SendShipNotification(current.ShipName, current.IsDocked);
                    anyChange = true;
                }
            }

            if (anyChange)
            {
                pastShipDockedStatuses = statuses;
                SaveDockShipStatuses(statuses);
            }
        }

        private DockShipStatus[] LoadDockShipStatuses()
        {
            if (_brain?.ltm == null)
            {
                return new DockShipStatus[0];
            }

            string json = _brain.ltm.Recall(dockShipStatusLtmKey);
            if (string.IsNullOrEmpty(json))
            {
                return new DockShipStatus[0];
            }

            var container = JsonUtility.FromJson<DockShipStatusContainer>(json);
            return container?.statuses ?? new DockShipStatus[0];
        }

        private void SaveDockShipStatuses(DockShipStatus[] statuses)
        {
            if (_brain?.ltm == null)
            {
                return;
            }

            var container = new DockShipStatusContainer { statuses = statuses };
            _brain.ltm.Remember(dockShipStatusLtmKey, JsonUtility.ToJson(container));
        }

        private void SendShipNotification(string shipName, bool isDocked)
        {
            string action = isDocked ? "docked at" : "left";
            notifications.SetMessage($"<i>{shipName}</i> has {action} the harbor");

            foreach (var type in notifications.types)
            {
                if (
                    type.category.ToLower() == shipNotificationTypeName
                    || type.name.ToLower() == shipNotificationTypeName
                )
                {
                    notifications.Send(System.Array.IndexOf(notifications.types, type));
                    break;
                }
            }
        }
        #endregion
    }
}
