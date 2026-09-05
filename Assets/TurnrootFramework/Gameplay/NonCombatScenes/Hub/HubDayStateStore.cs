using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using Turnroot.Utilities.Weather;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    /// <summary>
    /// Stores and loads per-day hub state in LongTermMemory.
    ///
    /// The goal is to make hub state (weather, shop stock, unit placement, etc.)
    /// deterministic for a given date, even across editor restarts.
    /// </summary>
    public static class HubDayStateStore
    {
        private const string HubDayStateKeyPrefix = "HubDayState_";
        private const string ExploreTutorialSeenKey = "HubExploreTutorialSeen";

        private static HubDayState _currentState;

        public static bool IsInitialized => _currentState != null;

        public static bool IsInitializedForDate(GameDate date) =>
            _currentState != null
            && _currentState.Year == date.year
            && _currentState.Month == date.month
            && _currentState.Day == date.day;

        public static int Seed => _currentState?.Seed ?? 0;
        public static bool HasProcessedDailyUpdates =>
            _currentState?.HasProcessedDailyUpdates ?? false;

        public static WeatherType Weather => _currentState?.Weather ?? WeatherType.Sunny;

        public static bool HasWeather => _currentState?.HasWeather ?? false;

        public static int SkyboxIndex => _currentState?.SkyboxIndex ?? -1;

        public static bool HasSkyboxIndex =>
            _currentState != null && _currentState.SkyboxIndex >= 0;

        public static bool HasSeenExploreTutorial(Brain.Brain brain)
        {
            return TryGetLongTermMemory(brain, nameof(HasSeenExploreTutorial), out var ltm)
                && ltm.RecallBool(ExploreTutorialSeenKey);
        }

        public static void MarkExploreTutorialSeen(Brain.Brain brain)
        {
            if (!TryGetLongTermMemory(brain, nameof(MarkExploreTutorialSeen), out var ltm))
            {
                return;
            }

            ltm.RememberBool(ExploreTutorialSeenKey, true);
        }

        public static void SetSkyboxIndex(Brain.Brain brain, int index)
        {
            if (!TryGetMutableState(brain, nameof(SetSkyboxIndex), out var ltm))
            {
                return;
            }

            _currentState.SkyboxIndex = index;
            SaveState(brain, ltm);
        }

        public static void SetShopItemQuantity(
            Brain.Brain brain,
            string shopName,
            string itemName,
            int quantity
        )
        {
            if (!TryGetMutableState(brain, nameof(SetShopItemQuantity), out var ltm))
            {
                return;
            }

            _currentState.ShopStock ??= new List<ShopStockEntry>();

            string shopKey = LongTermMemory.EncodeKey(shopName);
            string itemKey = LongTermMemory.EncodeKey(itemName);

            var existing = _currentState.ShopStock.Find(x =>
                x.ShopKey == shopKey && x.ItemKey == itemKey
            );
            if (existing != null)
            {
                existing.Quantity = quantity;
            }
            else
            {
                _currentState.ShopStock.Add(
                    new ShopStockEntry
                    {
                        ShopKey = shopKey,
                        ItemKey = itemKey,
                        Quantity = quantity,
                    }
                );
            }

            SaveState(brain, ltm);
        }

        public static int GetShopItemQuantity(
            string shopName,
            string itemName,
            int defaultValue = 0
        )
        {
            if (_currentState == null || _currentState.ShopStock == null)
            {
                return defaultValue;
            }

            string shopKey = LongTermMemory.EncodeKey(shopName);
            string itemKey = LongTermMemory.EncodeKey(itemName);

            var entry = _currentState.ShopStock.Find(x =>
                x.ShopKey == shopKey && x.ItemKey == itemKey
            );
            return entry?.Quantity ?? defaultValue;
        }

        public static bool HasShopStock(string shopName)
        {
            if (_currentState == null || _currentState.ShopStock == null)
            {
                return false;
            }

            string shopKey = LongTermMemory.EncodeKey(shopName);
            return _currentState.ShopStock.Exists(x => x.ShopKey == shopKey);
        }

        public static void MarkDailyUpdatesProcessed(Brain.Brain brain)
        {
            if (!TryGetMutableState(brain, nameof(MarkDailyUpdatesProcessed), out var ltm))
            {
                return;
            }

            _currentState.HasProcessedDailyUpdates = true;
            SaveState(brain, ltm);
        }

        public static bool HasInteractionHappenedToday(string characterFullName) =>
            HasRecordedToday(_currentState?.InteractionDoneIds, characterFullName);

        public static void MarkInteractionHappenedToday(Brain.Brain brain, string characterFullName)
        {
            MarkRecordedToday(
                brain,
                nameof(MarkInteractionHappenedToday),
                characterFullName,
                ref _currentState.InteractionDoneIds
            );
        }

        public static bool HasChitChatHappenedToday(string characterFullName) =>
            HasRecordedToday(_currentState?.ChitChatDoneIds, characterFullName);

        public static void MarkChitChatHappenedToday(Brain.Brain brain, string characterFullName)
        {
            MarkRecordedToday(
                brain,
                nameof(MarkChitChatHappenedToday),
                characterFullName,
                ref _currentState.ChitChatDoneIds
            );
        }

        public static bool HasRecruitmentAttemptHappenedToday(string characterFullName) =>
            HasRecordedToday(_currentState?.RecruitAttemptDoneIds, characterFullName);

        public static void MarkRecruitmentAttemptHappenedToday(
            Brain.Brain brain,
            string characterFullName
        )
        {
            MarkRecordedToday(
                brain,
                nameof(MarkRecruitmentAttemptHappenedToday),
                characterFullName,
                ref _currentState.RecruitAttemptDoneIds
            );
        }

        public static bool HasTeamPlacements() =>
            _currentState?.TeamPlacements != null && _currentState.TeamPlacements.Count > 0;

        public static Dictionary<int, HubSublocationName> GetTeamPlacements() =>
            !HasTeamPlacements()
                ? null
                : _currentState.TeamPlacements.ToDictionary(e => e.RosterIndex, e => e.Location);

        public static void SaveTeamPlacements(
            Brain.Brain brain,
            Dictionary<int, HubSublocationName> map
        )
        {
            if (!TryGetMutableState(brain, nameof(SaveTeamPlacements), out var ltm) || map == null)
            {
                return;
            }

            _currentState.TeamPlacements = map.Select(static kv => new TeamPlacementEntry
                {
                    RosterIndex = kv.Key,
                    Location = kv.Value,
                })
                .ToList();

            SaveState(brain, ltm);
        }

        public static bool HasNonRosterPlacements() =>
            _currentState?.NonRosterPlacements != null
            && _currentState.NonRosterPlacements.Count > 0;

        public static Dictionary<string, HubSublocationName> GetNonRosterPlacements()
        {
            return !HasNonRosterPlacements()
                ? null
                : _currentState.NonRosterPlacements.ToDictionary(
                    e => e.CharacterKey,
                    e => e.Location
                );
        }

        public static void SaveNonRosterPlacements(
            Brain.Brain brain,
            Dictionary<string, HubSublocationName> map
        )
        {
            if (
                !TryGetMutableState(brain, nameof(SaveNonRosterPlacements), out var ltm)
                || map == null
            )
            {
                return;
            }

            _currentState.NonRosterPlacements = map.Select(static kv => new NonRosterPlacementEntry
                {
                    CharacterKey = kv.Key,
                    Location = kv.Value,
                })
                .ToList();

            SaveState(brain, ltm);
        }

        public static void SetWeather(Brain.Brain brain, WeatherType weather)
        {
            if (!TryGetMutableState(brain, nameof(SetWeather), out var ltm))
            {
                return;
            }

            _currentState.Weather = weather;
            _currentState.HasWeather = true;
            SaveState(brain, ltm);
        }

        private static void SaveState(Brain.Brain brain, LongTermMemory ltm)
        {
            string key = GetKey(
                new GameDate
                {
                    year = _currentState.Year,
                    month = _currentState.Month,
                    day = _currentState.Day,
                }
            );
            ltm.Remember(key, brain.EncodeString(JsonUtility.ToJson(_currentState)));
        }

        public static void Initialize(Brain.Brain brain, GameDate date)
        {
            if (!TryGetLongTermMemory(brain, nameof(Initialize), out var ltm))
            {
                return;
            }

            string key = GetKey(date);
            string json = ltm.Recall(key);

            if (!string.IsNullOrEmpty(json))
            {
                json = brain.DecodeString(json);
            }

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<HubDayState>(json);
                    if (loaded != null)
                    {
                        _currentState = loaded;
                        int teamCount = _currentState.TeamPlacements?.Count ?? 0;
                        int nonRosterCount = _currentState.NonRosterPlacements?.Count ?? 0;
                        $"[HubDiag] HubDayStateStore.Initialize: Loaded existing state for key={key} | Weather={_currentState.Weather} HasWeather={_currentState.HasWeather} HasProcessedDailyUpdates={_currentState.HasProcessedDailyUpdates} TeamPlacements={teamCount} NonRosterPlacements={nonRosterCount} Seed={_currentState.Seed}".LogInfo(
                            "HubDayStateStore.Initialize"
                        );
                    }
                    else
                    {
                        $"[HubDiag] HubDayStateStore.Initialize: JSON deserialized to null for key={key}".LogWarning(
                            "HubDayStateStore.Initialize"
                        );
                    }
                }
                catch (Exception e)
                {
                    $"HubDayStateStore: Failed to parse saved hub state for {key}: {e.Message}".LogWarning();
                }
            }
            else
            {
                $"[HubDiag] HubDayStateStore.Initialize: No saved JSON found in LTM for key={key}".LogInfo(
                    "HubDayStateStore.Initialize"
                );
            }

            if (_currentState == null)
            {
                int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                _currentState = new HubDayState
                {
                    Year = date.year,
                    Month = date.month,
                    Day = date.day,
                    Seed = seed,
                };

                $"[HubDiag] HubDayStateStore.Initialize: Created FRESH state for key={key} Seed={seed}".LogInfo(
                    "HubDayStateStore.Initialize"
                );
                string outJson = brain.EncodeString(JsonUtility.ToJson(_currentState));
                ltm.Remember(key, outJson);
            }
        }

        private static bool TryGetLongTermMemory(
            Brain.Brain brain,
            string context,
            out LongTermMemory ltm
        )
        {
            ltm = null;

            if (
                !ValidationHelper.ValidateNotNull(
                    context,
                    (brain, nameof(brain)),
                    (brain?.ltm, "brain.ltm")
                )
            )
            {
                return false;
            }

            ltm = brain.ltm;
            return true;
        }

        private static bool TryGetMutableState(
            Brain.Brain brain,
            string context,
            out LongTermMemory ltm
        )
        {
            ltm = null;

            return TryGetLongTermMemory(brain, context, out ltm)
                && ValidationHelper.ValidateNotNull(_currentState, nameof(_currentState), context);
        }

        private static bool HasRecordedToday(List<string> values, string key) =>
            values != null && values.Contains(key);

        private static void MarkRecordedToday(
            Brain.Brain brain,
            string context,
            string key,
            ref List<string> values
        )
        {
            if (!TryGetMutableState(brain, context, out var ltm) || string.IsNullOrEmpty(key))
            {
                return;
            }

            values ??= new List<string>();
            if (values.Contains(key))
            {
                return;
            }

            values.Add(key);
            SaveState(brain, ltm);
        }

        private static string GetKey(GameDate date) =>
            $"{HubDayStateKeyPrefix}{date.year:0000}{date.month:00}{date.day:00}";

        public static string GetEncodedKey(GameDate date)
        {
            var raw = GetKey(date);
            return LongTermMemory.EncodeKey(raw);
        }

        public static string DecodeKey(string encodedKey) => LongTermMemory.DecodeKey(encodedKey);

        [Serializable]
        private class HubDayState
        {
            public int Year;
            public int Month;
            public int Day;
            public int Seed;
            public bool HasProcessedDailyUpdates;

            public WeatherType Weather;
            public bool HasWeather;
            public int SkyboxIndex = -1;
            public List<ShopStockEntry> ShopStock;

            public List<string> ChitChatDoneIds;

            public List<string> RecruitAttemptDoneIds;

            public List<string> InteractionDoneIds;
            public List<TeamPlacementEntry> TeamPlacements;
            public List<NonRosterPlacementEntry> NonRosterPlacements;
        }

        [Serializable]
        public class ShopStockEntry
        {
            public string ShopKey;
            public string ItemKey;
            public int Quantity;
        }

        [Serializable]
        public class TeamPlacementEntry
        {
            public int RosterIndex;
            public HubSublocationName Location;
        }

        [Serializable]
        public class NonRosterPlacementEntry
        {
            public string CharacterKey;
            public HubSublocationName Location;
        }
    }
}
