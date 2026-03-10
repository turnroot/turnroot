using System.Collections.Generic;
using System.IO;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Brain.Snapshots;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    [System.Serializable]
    public struct SaveFile
    {
        public string FileName;
        public Sprite AvatarPortrait;
        public AvatarBody AvatarBodyType;
        public int Progress;

        // stored in UTC, never local time; ensures stable play‑time calculations across
        // timezone changes and clock drift
        // previously stored the last write timestamp; now unused because
        // playtime is tracked using delta time.  kept here in case legacy
        // data must be migrated, but ignored otherwise.
        // public System.DateTime LastModified;
        public string CurrentScene;

        public string ChapterName;
        public int ChapterNumber;
        public int playTimeSeconds;
        public string LtmSubfolderPath;
        public Snapshot BookmarkSnapshot;
    }

    [System.Serializable]
    internal class SaveFilesData
    {
        public List<SaveFile> saveFiles = new();
    }

    public enum AvatarBody
    {
        None,
        MalePresenting,
        FemalePresenting,
    }

    public enum SaveFileSubfolders
    {
        A,
        B,
        C,
        D,
        E,
        F,
    }

    /// <summary>
    /// Manages save file operations within the brain framework.
    /// </summary>
    public class SaveFileBrain : BrainComponent
    {
        [HideInInspector]
        public List<SaveFile> SaveFiles = new();

        [HideInInspector]
        public SaveFileSubfolders ActiveSaveFileSubfolderPath;

        [HideInInspector]
        public SaveFile ActiveSaveFile =>
            SaveFiles.Find(sf =>
                sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
            );

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        private bool hasCheckedSaveFiles = false;

        // accumulator for seconds elapsed since last saved tick
        private float _playtimeAccumulator = 0f;

        // timer used to decide when to flush the current slot to disk
        private float _saveIntervalTimer = 0f;

        /// <summary>
        /// Fired whenever the active save slot's playTimeSeconds value changes.
        /// Carries the new total seconds for convenience.
        /// </summary>
        public event System.Action<int> OnActiveSaveFilePlaytimeUpdated;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnUpdateSaveFileName += HandleUpdateSaveFileName;
            Brain.OnUpdateSaveFileProgress += HandleUpdateSaveFileProgress;
            Brain.OnSetSaveFileCurrentScene += HandleSetSaveFileCurrentScene;
            Brain.OnSwitchActiveSaveFile += HandleSwitchActiveSaveFile;
            Brain.OnSetSaveFileChapter += HandleSetSaveFileChapter;

            // whenever the active scene actually changes we want to flush the
            // save batch; this ensures play‑time ticks forward before the new
            // scene begins so the player doesn't lose a few seconds when they
            // transition between levels.
            Brain.OnSceneChanged += HandleSceneChanged;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnUpdateSaveFileName -= HandleUpdateSaveFileName;
            Brain.OnUpdateSaveFileProgress -= HandleUpdateSaveFileProgress;
            Brain.OnSetSaveFileCurrentScene -= HandleSetSaveFileCurrentScene;
            Brain.OnSwitchActiveSaveFile -= HandleSwitchActiveSaveFile;
            Brain.OnSetSaveFileChapter -= HandleSetSaveFileChapter;

            Brain.OnSceneChanged -= HandleSceneChanged;
        }

        protected override void Awake()
        {
            base.Awake();
            // clear accumulator in case domain reload is disabled and the instance
            // survives across play-mode sessions, which would otherwise preserve
            // leftover seconds from the previous run.
            _playtimeAccumulator = 0f;
        }

        private void Update()
        {
            if (SaveFiles == null || SaveFiles.Count == 0)
            {
                return;
            }

            int activeIndex = SaveFiles.FindIndex(sf =>
                sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
            );
            if (activeIndex < 0)
            {
                return;
            }

            _playtimeAccumulator += Time.deltaTime;
            if (_playtimeAccumulator >= 1f)
            {
                int toAdd = (int)_playtimeAccumulator;
                var saveFile = SaveFiles[activeIndex];
                saveFile.playTimeSeconds += toAdd;
                if (saveFile.playTimeSeconds < 0)
                {
                    saveFile.playTimeSeconds = 0;
                }

                SaveFiles[activeIndex] = saveFile;
                _playtimeAccumulator -= toAdd;

                OnActiveSaveFilePlaytimeUpdated?.Invoke(saveFile.playTimeSeconds);

                // accumulate for periodic persistence
                _saveIntervalTimer += toAdd;
                if (_saveIntervalTimer >= 59f)
                {
                    SaveActiveFile();
                    _saveIntervalTimer = 0f;
                }
            }
        }

        protected void Start()
        {
            if (!hasCheckedSaveFiles)
            {
                LoadSaveFiles();
                hasCheckedSaveFiles = true;
            }
        }

        public OperationResult LoadSaveFiles()
        {
            try
            {
                SaveFiles = GetSavefileData();
                _playtimeAccumulator = 0f;
                // ActiveSaveFileSubfolderPath will be set when user selects a save file
                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"Failed to load save files: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads save file metadata from TurnrootBrain/structured/.turnrootsavefilesdata
        /// </summary>
        public List<SaveFile> GetSavefileData()
        {
            var structuredPath = Path.Combine(
                Application.persistentDataPath,
                "TurnrootBrain",
                "structured"
            );

            var saveFilesDataPath = Path.Combine(structuredPath, ".turnrootsavefilesdata");
            var obPath = Path.Combine(structuredPath, ".turnrootob");

            // Ensure structured directory exists
            if (!Directory.Exists(structuredPath))
            {
                Directory.CreateDirectory(structuredPath);
            }

            // Ensure .turnrootob exists
            if (!File.Exists(obPath))
            {
                var guid = System.Guid.NewGuid().ToString("N");
                var base64 = System.Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(guid)
                );
                File.WriteAllText(obPath, base64);
            }

            // Read save files data
            if (!File.Exists(saveFilesDataPath))
            {
                return new List<SaveFile>();
            }

            try
            {
                string encryptedData = File.ReadAllText(saveFilesDataPath);
                string json = Brain.DecodeString(encryptedData);

                if (string.IsNullOrEmpty(json))
                {
                    "Failed to decrypt save files data".LogWarning();
                    return new List<SaveFile>();
                }

                var data = JsonUtility.FromJson<SaveFilesData>(json);
                var list = data?.saveFiles ?? new List<SaveFile>();

                // guard against corrupted playtime values; zero them so the UI
                // doesn't display nonsense and logic doesn't propagate the bad data.
                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    if (entry.playTimeSeconds < 0)
                    {
                        $"Clamping negative playTimeSeconds for slot {i} ({entry.playTimeSeconds}).".LogWarning();
                        entry.playTimeSeconds = 0;
                    }
                    // if playTime is absurdly large (due to previous overflow) cap it
                    if (entry.playTimeSeconds > int.MaxValue / 2)
                    {
                        $"Capping excessive playTimeSeconds for slot {i} ({entry.playTimeSeconds}).".LogWarning();
                        entry.playTimeSeconds = 0;
                    }
                    list[i] = entry;
                }

                return list;
            }
            catch (System.Exception ex)
            {
                $"Failed to parse save files data: {ex.Message}".LogWarning();
                return new List<SaveFile>();
            }
        }

        /// <summary>
        /// Creates a new save file in the specified subfolder.
        /// </summary>
        public void CreateNewSaveFile(SaveFileSubfolders subfolder)
        {
            // Get current scene name, default to "GameStart" if none
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
            {
                sceneName = "GameStart";
            }

            var newSaveFile = new SaveFile
            {
                FileName = "Unnamed",
                AvatarBodyType = AvatarBody.None,
                AvatarPortrait = null,
                ChapterName = "Prologue",
                ChapterNumber = 0,
                Progress = 0,
                CurrentScene = sceneName,
                playTimeSeconds = 0,
                LtmSubfolderPath = subfolder.ToString().ToLower(),
                BookmarkSnapshot = null,
            };

            SaveFiles.Add(newSaveFile);
            ActiveSaveFileSubfolderPath = subfolder;
            SaveAllFiles();

            // Publish event to initialize LongTermMemory with the new subfolder
            Brain.PublishLongTermMemorySubfolderSet(subfolder.ToString().ToLower());
        }

        /// <summary>
        /// Saves all save files metadata to TurnrootBrain/structured/.turnrootsavefilesdata
        /// </summary>
        public void SaveAllFiles()
        {
            try
            {
                var structuredPath = Path.Combine(
                    Application.persistentDataPath,
                    "TurnrootBrain",
                    "structured"
                );

                var saveFilesDataPath = Path.Combine(structuredPath, ".turnrootsavefilesdata");

                // flush accumulated seconds into active save file
                if (SaveFiles.Count > 0)
                {
                    int activeIndex = SaveFiles.FindIndex(sf =>
                        sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
                    );
                    if (activeIndex >= 0 && _playtimeAccumulator >= 1f)
                    {
                        var saveFile = SaveFiles[activeIndex];
                        int toAdd = (int)_playtimeAccumulator;
                        saveFile.playTimeSeconds += toAdd;
                        _playtimeAccumulator -= toAdd;
                        if (saveFile.playTimeSeconds < 0)
                        {
                            saveFile.playTimeSeconds = 0;
                        }

                        SaveFiles[activeIndex] = saveFile;
                    }
                }

                // metadata timestamps are no longer relevant; skip update

                var data = new SaveFilesData { saveFiles = SaveFiles };
                string json = JsonUtility.ToJson(data, true);
                string encryptedData = Brain.EncodeString(json);

                if (string.IsNullOrEmpty(encryptedData))
                {
                    "Failed to encrypt save files data".LogError();
                    return;
                }

                File.WriteAllText(saveFilesDataPath, encryptedData);
            }
            catch (System.Exception ex)
            {
                $"Failed to save files data: {ex.Message}".LogError();
            }
        }

        #region Helpers

        private void SaveActiveFile()
        {
            // only update and write the currently active slot
            if (SaveFiles.Count == 0)
                return;
            int activeIndex = SaveFiles.FindIndex(sf =>
                sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
            );
            if (activeIndex < 0)
                return;
            WriteSaveFilesData();
        }

        private void WriteSaveFilesData()
        {
            var structuredPath = Path.Combine(
                Application.persistentDataPath,
                "TurnrootBrain",
                "structured"
            );

            var saveFilesDataPath = Path.Combine(structuredPath, ".turnrootsavefilesdata");
            var data = new SaveFilesData { saveFiles = SaveFiles };
            string json = JsonUtility.ToJson(data, true);
            string encryptedData = Brain.EncodeString(json);

            if (string.IsNullOrEmpty(encryptedData))
            {
                "Failed to encrypt save files data".LogError();
                return;
            }

            File.WriteAllText(saveFilesDataPath, encryptedData);
        }

        #endregion

        #region Event Handlers

        private void HandleUpdateSaveFileName(string fileName)
        {
            if (SaveFiles.Count == 0)
            {
                "Cannot update save file name: no save files exist.".LogWarning();
                return;
            }

            int activeIndex = SaveFiles.FindIndex(sf =>
                sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
            );

            if (activeIndex >= 0)
            {
                var saveFile = SaveFiles[activeIndex];
                saveFile.FileName = fileName;
                SaveFiles[activeIndex] = saveFile;
                SaveAllFiles();
                $"Updated save file name to: {fileName}".LogInfo();
            }
            else
            {
                "Could not find active save file to update name.".LogWarning();
            }
        }

        private void HandleUpdateSaveFileProgress(int progress)
        {
            if (SaveFiles.Count == 0)
            {
                "Cannot update save file progress: no save files exist.".LogWarning();
                return;
            }

            // Update the active save file
            int activeIndex = SaveFiles.FindIndex(sf =>
                sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
            );

            if (activeIndex >= 0)
            {
                var saveFile = SaveFiles[activeIndex];
                saveFile.Progress = progress;
                SaveFiles[activeIndex] = saveFile;
                SaveAllFiles();
                $"Updated save file progress to: {progress}".LogInfo();
            }
            else
            {
                "Could not find active save file to update progress.".LogWarning();
            }
        }

        private void HandleSetSaveFileCurrentScene(string sceneName)
        {
            if (SaveFiles.Count == 0)
            {
                "Cannot set save file current scene: no save files exist.".LogWarning();
                return;
            }

            // Update the active save file
            int activeIndex = SaveFiles.FindIndex(sf =>
                sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
            );

            if (activeIndex >= 0)
            {
                var saveFile = SaveFiles[activeIndex];
                saveFile.CurrentScene = sceneName;
                SaveFiles[activeIndex] = saveFile;
                SaveAllFiles();
                $"Updated save file current scene to: {sceneName}".LogInfo();
            }
            else
            {
                "Could not find active save file to update scene.".LogWarning();
            }
        }

        private void HandleSwitchActiveSaveFile(SaveFileSubfolders subfolder)
        {
            // Save current data before switching
            SaveAllFiles();

            // switch accumulator so it doesn't carry over between slots
            _playtimeAccumulator = 0f;

            // Switch to the new save file
            ActiveSaveFileSubfolderPath = subfolder;
            string subfolderPath = subfolder.ToString().ToLower();

            // Reinitialize LongTermMemory with the new subfolder
            Brain.PublishLongTermMemorySubfolderSet(subfolderPath);

            $"Switched active save file to: {subfolder} (subfolder: {subfolderPath})".LogInfo();
        }

        private void HandleSetSaveFileChapter(string chapterName, int chapterNumber)
        {
            if (SaveFiles.Count == 0)
            {
                "Cannot set save file chapter: no save files exist.".LogWarning();
                return;
            }

            // Update the active save file
            int activeIndex = SaveFiles.FindIndex(sf =>
                sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
            );

            if (activeIndex >= 0)
            {
                var saveFile = SaveFiles[activeIndex];
                saveFile.ChapterName = chapterName;
                saveFile.ChapterNumber = chapterNumber;
                SaveFiles[activeIndex] = saveFile;
                SaveAllFiles();
                $"Updated save file chapter to: {chapterName} (Chapter {chapterNumber})".LogInfo();
            }
            else
            {
                "Could not find active save file to update chapter.".LogWarning();
            }
        }

        private void HandleSceneChanged(string sceneName, string displayName)
        {
            // simply trigger a save so the playtime is updated before the new
            // scene begins.  we don't change the save file's current scene here
            // (that is handled by other brain events) – this is purely for time
            // accounting.
            SaveAllFiles();
        }

        #endregion
    }
}
