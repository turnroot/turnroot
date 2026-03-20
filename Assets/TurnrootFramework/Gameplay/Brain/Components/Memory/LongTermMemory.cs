using System;
using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components
{
    /// <summary>
    /// Manages long-term memory storage and retrieval for the brain system.
    /// Provides batched save operations and event notifications for key set changes.
    /// </summary>
    public class LongTermMemory : MonoBehaviour
    {
        private JsonPlayerPrefs prefs;
        private bool isDirty;
        private bool isInitialized = false;

        /// <summary>
        /// True when the prefs object has been set up and is ready to use.
        /// </summary>
        public bool Initialized => isInitialized;
        private Brain _brain;

        /// <summary>
        /// Fired when the underlying keyset changes in JsonPlayerPrefs.
        /// Subscribers should use this to invalidate caches or react to keyset changes.
        /// The int value is the keyCacheVersion from JsonPlayerPrefs.
        /// </summary>
        public event Action<int> OnKeySetChanged;

        // use Turnroot.Utilities.GameDate defined in Utilities/GameDate.cs

        /// <summary>
        /// Expose the runtime key cache version from JsonPlayerPrefs.
        /// </summary>
        public int KeyCacheVersion => prefs?.KeyCacheVersion ?? 0;

        public void Awake()
        {
            _brain = GetComponent<Brain>();
            if (_brain != null)
            {
                _brain.OnLongTermMemorySubfolderSet += HandleSubfolderSet;
            }
        }

        private void HandleSubfolderSet(string subfolder)
        {
            if (!isInitialized)
            {
                InitializePrefs(subfolder);
                isInitialized = true;

                // Notify other brain components that LTM is ready
                var brain = GetComponent<Brain>();
                brain?.PublishLongTermMemoryInitialized();
            }
        }

        private void InitializePrefs(string subfolder)
        {
            var prefsPath = System.IO.Path.Combine(
                Application.persistentDataPath,
                "TurnrootBrain",
                subfolder,
                ".turnrootdata"
            );

            prefs = new JsonPlayerPrefs(prefsPath);
            prefs.OnKeySetChanged += HandlePrefsKeySetChanged;

            var obDir = System.IO.Path.GetDirectoryName(prefsPath);
            var obPath = System.IO.Path.Combine(obDir, ".turnrootob");
            if (!System.IO.Directory.Exists(obDir))
            {
                System.IO.Directory.CreateDirectory(obDir);
            }

            if (!System.IO.File.Exists(obPath))
            {
                var guid = Guid.NewGuid().ToString("N");
                var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(guid));
                System.IO.File.WriteAllText(obPath, base64);
            }
        }

        private void HandlePrefsKeySetChanged(int version)
        {
            OnKeySetChanged?.Invoke(version);

            _brain?.PublishLtmKeyCacheUpdated(version);
        }

        /// <summary>
        /// Stores a string value in long-term memory.
        /// Changes are written to disk immediately unless deferred mode is active.
        /// </summary>
        public string Remember(string key, string value)
        {
            if (prefs == null)
            {
                "LongTermMemory: Attempted to Remember before initialization.".LogWarning();
                return value;
            }
            prefs.SetString(key, value);
            SaveImmediate();
            return value;
        }

        /// <summary>
        /// Stores an integer value in long-term memory.
        /// </summary>
        public int RememberInt(string key, int value)
        {
            if (prefs == null)
            {
                "LongTermMemory: Attempted to RememberInt before initialization.".LogWarning();
                return value;
            }
            prefs.SetInt(key, value);
            SaveImmediate();
            return value;
        }

        /// <summary>
        /// Stores a boolean value in long-term memory.
        /// </summary>
        public bool RememberBool(string key, bool value)
        {
            if (prefs == null)
            {
                "LongTermMemory: Attempted to RememberBool before initialization.".LogWarning();
                return value;
            }
            prefs.SetBool(key, value);
            SaveImmediate();
            return value;
        }

        /// <summary>
        /// Retrieves a string value from long-term memory.
        /// </summary>
        /// <returns>The string value, or null if the key does not exist.</returns>
        public string Recall(string key)
        {
            if (prefs == null)
            {
                "LongTermMemory: Attempted to Recall before initialization.".LogWarning();
                return null;
            }
            return prefs.GetString(key, null);
        }

        /// <summary>
        /// Retrieves an integer value from long-term memory.
        /// </summary>
        /// <returns>The integer value, or -1 if the key does not exist.</returns>
        public int RecallInt(string key)
        {
            if (prefs == null)
            {
                "LongTermMemory: Attempted to RecallInt before initialization.".LogWarning();
                return -1;
            }
            return prefs.GetInt(key, -1);
        }

        /// <summary>
        /// Retrieves a boolean value from long-term memory.
        /// </summary>
        /// <returns>The boolean value, or false if the key does not exist.</returns>
        public bool RecallBool(string key)
        {
            if (prefs == null)
            {
                "LongTermMemory: Attempted to RecallBool before initialization.".LogWarning();
                return false;
            }
            return prefs.GetBool(key, false);
        }

        /// <summary>
        /// Removes a value from long-term memory.
        /// </summary>
        /// <param name="key">The key to forget.</param>
        /// <param name="permanent">If true, saves the deleted value with a "_deleted" suffix.</param>
        public void Forget(string key, bool permanent = false)
        {
            if (permanent)
            {
                string value = prefs.GetString(key, null);
                if (value != null)
                {
                    Remember(key + "_deleted", value);
                }
            }

            prefs.DeleteKey(key);
            SaveImmediate();
        }

        public void ForgetInt(string key, bool permanent = false)
        {
            if (permanent)
            {
                int value = prefs.GetInt(key, -1);
                if (value != -1)
                {
                    RememberInt(key + "_deleted", value);
                }
            }

            prefs.DeleteKey(key);
            SaveImmediate();
        }

        public void ForgetBool(string key, bool permanent = false)
        {
            if (permanent)
            {
                if (prefs.HasKey(key))
                {
                    bool boolValue = prefs.GetBool(key, false);
                    RememberBool(key + "_deleted", boolValue);
                }
            }

            prefs.DeleteKey(key);
            SaveImmediate();
        }

        public List<string> RecallKeysByPrefix(string prefix)
        {
            var keys = new List<string>();
            if (prefs == null)
            {
                return keys;
            }

            foreach (var key in prefs.GetAllKeys())
            {
                if (key != null && key.StartsWith(prefix))
                {
                    keys.Add(key);
                }
            }

            return keys;
        }

        /// <summary>
        /// Encodes a raw key using the same obfuscation mechanism that LongTermMemory uses internally.
        /// This allows you to compute the persisted key name that appears in the underlying JSON.
        /// </summary>
        public static string EncodeKey(string key)
        {
            var result = DeviceDataCipher.EncryptToBase64(key);
            return result.Success ? result.Value : key;
        }

        /// <summary>
        /// Decodes an encoded storage key back into its original raw form.
        /// </summary>
        public static string DecodeKey(string encodedKey)
        {
            var result = DeviceDataCipher.DecryptFromBase64(encodedKey);
            return result.Success ? result.Value : encodedKey;
        }

        #region Game Date Support

        /// <summary>
        /// Store a calendar date in long–term memory and publish an event.
        /// </summary>
        public void SetGameDate(int year, Month month, int day)
        {
            if (prefs == null)
            {
                "LongTermMemory: Attempted to set game date before initialization.".LogWarning();
                return;
            }

            prefs.SetInt(LtmKeys.GameDateYear, year);
            prefs.SetInt(LtmKeys.GameDateMonth, (int)month + 1);
            prefs.SetInt(LtmKeys.GameDateDay, day);
            SaveImmediate();

            _brain?.PublishGameDateChanged(year, (int)month + 1, day);
        }

        /// <summary>
        /// Retrieve the stored game date. Fields will be default if not present.
        /// </summary>
        public GameDate GetGameDate()
        {
            var result = GameDate.Default;
            if (prefs == null)
            {
                "LongTermMemory: Attempted to get game date before initialization.".LogWarning();
                return result;
            }

            result.year = prefs.GetInt(LtmKeys.GameDateYear, 0);
            int monthValue = prefs.GetInt(LtmKeys.GameDateMonth, 1);
            result.month = Mathf.Clamp(monthValue, 1, 12);
            result.day = prefs.GetInt(LtmKeys.GameDateDay, 1);
            return result;
        }

        #endregion

        private void SaveImmediate() => prefs.Save();

        private void OnDestroy()
        {
            var brain = GetComponent<Brain>();
            if (brain != null)
            {
                brain.OnLongTermMemorySubfolderSet -= HandleSubfolderSet;
            }

            if (prefs != null)
            {
                prefs.OnKeySetChanged -= HandlePrefsKeySetChanged;
            }
        }
    }
}
