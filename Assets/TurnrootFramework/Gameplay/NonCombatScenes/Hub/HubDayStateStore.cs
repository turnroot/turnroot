using System;
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

        private static HubDayState _currentState;

        public static bool IsInitialized => _currentState != null;

        public static int Seed => _currentState?.Seed ?? 0;
        public static bool HasProcessedDailyUpdates =>
            _currentState?.HasProcessedDailyUpdates ?? false;

        public static WeatherType Weather => _currentState?.Weather ?? WeatherType.Sunny;

        public static bool HasWeather => _currentState?.HasWeather ?? false;

        public static int SkyboxIndex => _currentState?.SkyboxIndex ?? -1;

        public static bool HasSkyboxIndex =>
            _currentState != null && _currentState.SkyboxIndex >= 0;

        public static void SetSkyboxIndex(Brain.Brain brain, int index)
        {
            if (brain?.ltm == null || _currentState == null)
            {
                return;
            }

            _currentState.SkyboxIndex = index;
            SaveState(brain);
        }

        public static void SetShopItemQuantity(
            Brain.Brain brain,
            string shopName,
            string itemName,
            int quantity
        )
        {
            if (brain?.ltm == null || _currentState == null)
            {
                return;
            }

            _currentState.ShopStock ??= new System.Collections.Generic.List<ShopStockEntry>();

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

            SaveState(brain);
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
            if (brain?.ltm == null || _currentState == null)
            {
                return;
            }

            _currentState.HasProcessedDailyUpdates = true;
            SaveState(brain);
        }

        public static bool HasInteractionHappenedToday(string characterFullName) =>
            _currentState?.InteractionDoneIds != null
            && _currentState.InteractionDoneIds.Contains(characterFullName);

        public static void MarkInteractionHappenedToday(Brain.Brain brain, string characterFullName)
        {
            if (
                brain?.ltm == null
                || _currentState == null
                || string.IsNullOrEmpty(characterFullName)
            )
            {
                return;
            }

            _currentState.InteractionDoneIds ??= new System.Collections.Generic.List<string>();
            if (!_currentState.InteractionDoneIds.Contains(characterFullName))
            {
                _currentState.InteractionDoneIds.Add(characterFullName);
                SaveState(brain);
            }
        }

        public static bool HasChitChatHappenedToday(string characterFullName) =>
            _currentState?.ChitChatDoneIds != null
            && _currentState.ChitChatDoneIds.Contains(characterFullName);

        public static void MarkChitChatHappenedToday(Brain.Brain brain, string characterFullName)
        {
            if (
                brain?.ltm == null
                || _currentState == null
                || string.IsNullOrEmpty(characterFullName)
            )
            {
                return;
            }

            _currentState.ChitChatDoneIds ??= new System.Collections.Generic.List<string>();
            if (!_currentState.ChitChatDoneIds.Contains(characterFullName))
            {
                _currentState.ChitChatDoneIds.Add(characterFullName);
                SaveState(brain);
            }
        }

        public static bool HasTeamPlacements() =>
            _currentState?.TeamPlacements != null && _currentState.TeamPlacements.Count > 0;

        public static System.Collections.Generic.Dictionary<
            int,
            HubSublocationName
        > GetTeamPlacements() => !HasTeamPlacements() ? null : _currentState.TeamPlacements.ToDictionary(e => e.RosterIndex, e => e.Location);

        public static void SaveTeamPlacements(
            Brain.Brain brain,
            System.Collections.Generic.Dictionary<int, HubSublocationName> map
        )
        {
            if (brain?.ltm == null || _currentState == null || map == null)
            {
                return;
            }

            _currentState.TeamPlacements = map.Select(static kv => new TeamPlacementEntry
                {
                    RosterIndex = kv.Key,
                    Location = kv.Value,
                })
                .ToList();

            SaveState(brain);
        }

        public static bool HasNonRosterPlacements() =>
            _currentState?.NonRosterPlacements != null
            && _currentState.NonRosterPlacements.Count > 0;

        public static System.Collections.Generic.Dictionary<
            string,
            HubSublocationName
        > GetNonRosterPlacements()
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
            System.Collections.Generic.Dictionary<string, HubSublocationName> map
        )
        {
            if (brain?.ltm == null || _currentState == null || map == null)
            {
                return;
            }

            _currentState.NonRosterPlacements = map.Select(static kv => new NonRosterPlacementEntry
                {
                    CharacterKey = kv.Key,
                    Location = kv.Value,
                })
                .ToList();

            SaveState(brain);
        }

        public static void SetWeather(Brain.Brain brain, WeatherType weather)
        {
            if (brain?.ltm == null || _currentState == null)
            {
                return;
            }

            _currentState.Weather = weather;
            _currentState.HasWeather = true;
            SaveState(brain);
        }

        private static void SaveState(Brain.Brain brain)
        {
            if (brain?.ltm == null || _currentState == null)
            {
                return;
            }

            string key = GetKey(
                new GameDate
                {
                    year = _currentState.Year,
                    month = _currentState.Month,
                    day = _currentState.Day,
                }
            );
            brain.ltm.Remember(key, brain.EncodeString(JsonUtility.ToJson(_currentState)));
        }

        public static void Initialize(Brain.Brain brain, GameDate date)
        {
            if (brain?.ltm == null)
            {
                return;
            }

            string key = GetKey(date);
            string json = brain.ltm.Recall(key);

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
                brain.ltm.Remember(key, outJson);
            }
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
            public System.Collections.Generic.List<ShopStockEntry> ShopStock;

            public System.Collections.Generic.List<string> ChitChatDoneIds;

            public System.Collections.Generic.List<string> InteractionDoneIds;
            public System.Collections.Generic.List<TeamPlacementEntry> TeamPlacements;
            public System.Collections.Generic.List<NonRosterPlacementEntry> NonRosterPlacements;
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
