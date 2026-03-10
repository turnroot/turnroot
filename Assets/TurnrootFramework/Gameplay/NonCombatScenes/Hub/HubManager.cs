using TMPro;
using Turnroot.Characters;
using Turnroot.GameSettings;
using Turnroot.UI.Components.Notifications;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubManager : MonoBehaviour
    {
        #region Fields
        private Brain.Brain _brain;

        public TextMeshProUGUI dateText;

        public NotificationsHelper notifications;

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

        #region Unity Lifecycle
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
                Debug.Log(
                    $"HubManager: Current game date from LTM is {gameDate.year}/{gameDate.month}/{gameDate.day}"
                );
            }

            Initialize();
        }

        public void Initialize()
        {
            _brain.OnGameDateChanged += HandleGameDateChanged;
            _brain.OnCharacterBirthdayThisWeek += HandleCharacterBirthdayThisWeek;            UpdateDateText();
            _brain.charactersBrain.CheckBirthdays();
            UpdateChapterNumberAndNameText(
                _brain.saveFileBrain.ActiveSaveFile.ChapterNumber,
                _brain.saveFileBrain.ActiveSaveFile.ChapterName
            );
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
