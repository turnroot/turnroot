using Turnroot.Characters;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Brain Event Handlers

        private void HandleSublocationInputModeChange(HubInputMode mode) => SetInputMode(mode);

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
            dock.RefreshShipsForNewDay(gameDate);

            var statuses = dock.PublishDockedShipStatuses();
            if (statuses == null || statuses.Length == 0)
            {
                return;
            }

            // Ensure we have a cached baseline; if none exists, treat all as undocked (so first check can notify correctly)
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

        public void CheckRareItems()
        {
            var rareItemStrings = shopsManager.RefreshShopsForNewDay(gameDate);

            foreach (var message in rareItemStrings)
            {
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }
                notifications.SetMessage(message);
                foreach (var type in notifications.types)
                {
                    if (
                        type.category.ToLower() == itemNotificationTypeName
                        || type.name.ToLower() == itemNotificationTypeName
                    )
                    {
                        notifications.Send(System.Array.IndexOf(notifications.types, type));
                        break;
                    }
                }
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
