using NaughtyAttributes;
using TMPro;
using Turnroot.Characters;
using Turnroot.GameSettings;
using Turnroot.UI;
using Turnroot.UI.Components.Notifications;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(UiInputProvider))]
    /// <remarks>
    /// This may need editing for your project, but if you aren't making major logic changes, you should
    /// be able to wrangle it to work for you just with UI changes and inspector stuff
    /// </remarks>
    public class HubManager : MonoBehaviour
    {
        #region Fields
        private Brain.Brain _brain;

        public TextMeshProUGUI dateText;

        public UiChoice[] LocationChoices;

        [BoxGroup("Input")]
        public UiInputProvider InputProvider;

        private enum InputMode
        {
            None,
            Location,
            MarketChoice,
            FishMarket,
            Blacksmith,
            Armory,
            Staples,
            SecretShop,
            CafeChoice,
            GroupLunch,
            PersonalLunch,
            Battlefields,
            Docks,
            Training,
        }

        private InputMode _currentInputMode = InputMode.None;
        private int currentIndex = 0;

        public NotificationsHelper notifications;

        public HubSubLocation[] subLocations;

        public TextMeshProUGUI ChapterNumberAndNameText;
        public string ChapterNumberAndNameFormat = "Chapter {0}: {1}";
        private const string birthdayNotificationTypeName = "birthday";

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

        private GameDate gameDate;

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
                            selectedLocation.PlayerVisit();
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
                            selectedLocation.PlayerVisit();
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
                InputProvider.OnInput += HandleInput;
        }

        private void OnDisable()
        {
            if (InputProvider != null)
                InputProvider.OnInput -= HandleInput;
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
                gameDate = ltm.GetGameDate();
                $"HubManager: Current game date from LTM is {gameDate.year}/{gameDate.month}/{gameDate.day}".LogInfo();
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
            UpdateDateText();
            _brain.charactersBrain.CheckBirthdays();
            UpdateChapterNumberAndNameText(
                _brain.saveFileBrain.ActiveSaveFile.ChapterNumber,
                _brain.saveFileBrain.ActiveSaveFile.ChapterName
            );
            SetInputMode(InputMode.Location);

            for (int i = 0; i < subLocations.Length; i++)
            {
                subLocations[i].Initialize(_brain);
                LocationChoices[i].CanBeSelected = subLocations[i].CanBeVisitedToday();
            }

            UpdateChoiceSelection();
        }

        public void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnGameDateChanged -= HandleGameDateChanged;
                _brain.OnCharacterBirthdayThisWeek -= HandleCharacterBirthdayThisWeek;
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput(string action)
        {
            switch (_currentInputMode)
            {
                case InputMode.Location:
                    HandleLocationInput(action);
                    break;
            }
        }

        private void SetInputMode(InputMode mode)
        {
            _currentInputMode = mode;
            currentIndex = 0;
        }

        #endregion


        #region Helpers
        public void UpdateDateText()
        {
            if (dateText != null)
            {
                Month month = (Month)gameDate.month;
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
        #endregion
    }
}
