using System;
using System.Collections.Generic;
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
        /// Fired when the underlying keyset changes in JsonPlayerPrefs.
        /// Subscribers should use this to invalidate caches or react to keyset changes.
        /// The int value is the keyCacheVersion from JsonPlayerPrefs.
        /// </summary>
        public event Action<int> OnKeySetChanged;

        /// <summary>
        /// Expose the runtime key cache version from JsonPlayerPrefs.
        /// </summary>
        public int KeyCacheVersion => prefs?.KeyCacheVersion ?? 0;

        public void Awake()
        {
            var brain = GetComponent<Brain>();
            if (brain != null)
            {
                brain.OnLongTermMemorySubfolderSet += HandleSubfolderSet;
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

            var brain = gameObject.GetComponent<Brain>();
            brain?.PublishLtmKeyCacheUpdated(version);
        }

        /// <summary>
        /// Stores a string value in long-term memory.
        /// Changes are written to disk immediately unless deferred mode is active.
        /// </summary>
        public string Remember(string key, string value)
        {
            if (prefs == null)
            {
                Debug.LogWarning("LongTermMemory: Attempted to Remember before initialization.");
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
                Debug.LogWarning("LongTermMemory: Attempted to RememberInt before initialization.");
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
                Debug.LogWarning(
                    "LongTermMemory: Attempted to RememberBool before initialization."
                );
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
                Debug.LogWarning("LongTermMemory: Attempted to Recall before initialization.");
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
                Debug.LogWarning("LongTermMemory: Attempted to RecallInt before initialization.");
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
                Debug.LogWarning("LongTermMemory: Attempted to RecallBool before initialization.");
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
                int intValue = prefs.GetInt(key, -1);
                if (intValue != -1)
                {
                    RememberInt(key + "_deleted", intValue);
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
