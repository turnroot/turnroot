using System;
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

        /// <summary>
        /// Sets the quantity for a given shop item on this hub day.
        /// </summary>
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

            if (_currentState.ShopStock == null)
            {
                _currentState.ShopStock = new System.Collections.Generic.List<ShopStockEntry>();
            }

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

        public static void MarkDailyUpdatesProcessed(Brain.Brain brain)
        {
            if (brain?.ltm == null || _currentState == null)
            {
                return;
            }

            _currentState.HasProcessedDailyUpdates = true;
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
            brain.ltm.Remember(key, JsonUtility.ToJson(_currentState));
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
                try
                {
                    var loaded = JsonUtility.FromJson<HubDayState>(json);
                    if (loaded != null)
                    {
                        _currentState = loaded;
                    }
                }
                catch (Exception e)
                {
                    $"HubDayStateStore: Failed to parse saved hub state for {key}: {e.Message}".LogWarning();
                }
            }

            if (_currentState == null)
            {
                // Create a new deterministic seed for this day.
                int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                _currentState = new HubDayState
                {
                    Year = date.year,
                    Month = date.month,
                    Day = date.day,
                    Seed = seed,
                };

                string outJson = JsonUtility.ToJson(_currentState);
                brain.ltm.Remember(key, outJson);
            }
        }

        private static string GetKey(GameDate date) =>
            $"{HubDayStateKeyPrefix}{date.year:0000}{date.month:00}{date.day:00}";

        /// <summary>
        /// Returns the encoded key used internally by LongTermMemory for the specified date.
        /// </summary>
        public static string GetEncodedKey(GameDate date)
        {
            var raw = GetKey(date);
            return LongTermMemory.EncodeKey(raw);
        }

        /// <summary>
        /// Decodes a stored LTM key string back into the raw HubDayState key.
        /// </summary>
        public static string DecodeKey(string encodedKey) => LongTermMemory.DecodeKey(encodedKey);

        [Serializable]
        private class HubDayState
        {
            public int Year;
            public int Month;
            public int Day;
            public int Seed;
            public bool HasProcessedDailyUpdates;

            // Weather state is kept in LTM so the same weather can be used across sessions.
            public WeatherType Weather;
            public bool HasWeather;
            public int SkyboxIndex = -1;
            public System.Collections.Generic.List<ShopStockEntry> ShopStock;
        }

        [Serializable]
        public class ShopStockEntry
        {
            public string ShopKey;
            public string ItemKey;
            public int Quantity;
        }
    }
}
