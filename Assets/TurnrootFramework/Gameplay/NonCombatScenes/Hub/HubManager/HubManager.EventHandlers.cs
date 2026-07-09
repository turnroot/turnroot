using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Event Handlers

        public void HandleGameDateChanged(int year, int month, int day)
        {
            if (
                !ValidateRequired(
                    nameof(HandleGameDateChanged),
                    (_brain, nameof(_brain)),
                    (_brain?.charactersBrain, "_brain.charactersBrain")
                )
            )
            {
                return;
            }

            gameDate = new GameDate(year, month, day);
            _brain.charactersBrain.CheckBirthdays();
            $"HubManager: Game date changed to {gameDate.year}/{gameDate.month}/{gameDate.day}".LogInfo();
        }

        public void HandleCharacterBirthdayThisWeek(CharacterInstance character, GameDate date)
        {
            if (
                !ValidateRequired(
                    nameof(HandleCharacterBirthdayThisWeek),
                    (character, nameof(character)),
                    (character?.CharacterTemplate, "character.CharacterTemplate")
                )
            )
            {
                return;
            }

            int bdDay = character.CharacterTemplate.BirthdayDay;
            int bdMonth = character.CharacterTemplate.BirthdayMonth;

            string message =
                $"It's <b>{character.CharacterTemplate.DisplayName}</b>'s birthday this week, on the {bdDay}{GameDate.GetDaySuffix(bdDay)}";

            if (gameDate.day == bdDay && gameDate.month == bdMonth)
            {
                message = $"Today is <b>{character.CharacterTemplate.DisplayName}</b>'s birthday!";
            }

            SendTypedNotification(
                message,
                birthdayNotificationTypeName,
                nameof(HandleCharacterBirthdayThisWeek)
            );
        }

        public void CheckShipsDocked()
        {
            if (!ValidateRequired(dock, nameof(dock), nameof(CheckShipsDocked)))
            {
                return;
            }

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

            var previousByShipName = new Dictionary<string, bool>(pastShipDockedStatuses.Length);
            for (int i = 0; i < pastShipDockedStatuses.Length; i++)
            {
                var previous = pastShipDockedStatuses[i];
                if (string.IsNullOrEmpty(previous.ShipName))
                {
                    continue;
                }

                previousByShipName[previous.ShipName] = previous.IsDocked;
            }

            for (int i = 0; i < statuses.Length; i++)
            {
                var current = statuses[i];
                bool wasDocked =
                    !string.IsNullOrEmpty(current.ShipName)
                    && previousByShipName.TryGetValue(current.ShipName, out var previousDocked)
                    && previousDocked;

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
            if (!ValidateRequired(nameof(CheckRareItems), (shopsManager, nameof(shopsManager))))
            {
                return;
            }

            var rareItemStrings = shopsManager.RefreshShopsForNewDay(gameDate);
            if (rareItemStrings == null || rareItemStrings.Length == 0)
            {
                return;
            }

            foreach (var message in rareItemStrings)
            {
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }

                SendTypedNotification(message, itemNotificationTypeName, nameof(CheckRareItems));
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

            json = _brain.DecodeString(json);
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
            _brain.ltm.Remember(
                dockShipStatusLtmKey,
                _brain.EncodeString(JsonUtility.ToJson(container))
            );
        }

        private void SendShipNotification(string shipName, bool isDocked)
        {
            string action = isDocked ? "docked at" : "left";
            SendTypedNotification(
                $"<i>{shipName}</i> has {action} the harbor",
                shipNotificationTypeName,
                nameof(SendShipNotification)
            );
        }

        private void SendTypedNotification(string message, string typeName, string context)
        {
            if (
                !ValidateRequired(
                    context,
                    (notifications, nameof(notifications)),
                    (notifications?.types, "notifications.types")
                )
            )
            {
                return;
            }

            notifications.SetMessage(message);

            for (int i = 0; i < notifications.types.Length; i++)
            {
                var type = notifications.types[i];
                if (type == null)
                {
                    continue;
                }

                if (
                    string.Equals(
                        type.category,
                        typeName,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                    || string.Equals(type.name, typeName, System.StringComparison.OrdinalIgnoreCase)
                )
                {
                    notifications.Send(i);
                    return;
                }
            }
        }

        #endregion
    }
}
