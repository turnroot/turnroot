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
        public System.DateTime LastModified;
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
        public SaveFile ActiveSaveFile => SaveFiles.Find(sf =>
            sf.LtmSubfolderPath == ActiveSaveFileSubfolderPath.ToString().ToLower()
        );

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        private bool hasCheckedSaveFiles = false;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnUpdateSaveFileName += HandleUpdateSaveFileName;
            Brain.OnUpdateSaveFileProgress += HandleUpdateSaveFileProgress;
            Brain.OnSetSaveFileCurrentScene += HandleSetSaveFileCurrentScene;
            Brain.OnSwitchActiveSaveFile += HandleSwitchActiveSaveFile;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnUpdateSaveFileName -= HandleUpdateSaveFileName;
            Brain.OnUpdateSaveFileProgress -= HandleUpdateSaveFileProgress;
            Brain.OnSetSaveFileCurrentScene -= HandleSetSaveFileCurrentScene;
            Brain.OnSwitchActiveSaveFile -= HandleSwitchActiveSaveFile;
        }

        protected override void Awake() => base.Awake();

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

                string subfolder = ActiveSaveFileSubfolderPath.ToString().ToLower();
                Brain.PublishLongTermMemorySubfolderSet(subfolder);

                if (SaveFiles.Count == 0)
                {
                    InitializeSaveFiles();
                }
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
                return data?.saveFiles ?? new List<SaveFile>();
            }
            catch (System.Exception ex)
            {
                $"Failed to parse save files data: {ex.Message}".LogWarning();
                return new List<SaveFile>();
            }
        }

        private void InitializeSaveFiles() => CreateNewSaveFile(SaveFileSubfolders.A);

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
                LastModified = System.DateTime.Now,
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

                // Update each save file's data before saving
                for (int i = 0; i < SaveFiles.Count; i++)
                {
                    var saveFile = SaveFiles[i];
                    var timeSinceModified = System.DateTime.Now - saveFile.LastModified;
                    saveFile.playTimeSeconds += (int)timeSinceModified.TotalSeconds;
                    saveFile.LastModified = System.DateTime.Now;
                    SaveFiles[i] = saveFile;
                }

                var data = new SaveFilesData { saveFiles = SaveFiles };
                string json = JsonUtility.ToJson(data, true);
                string encryptedData = Brain.EncodeString(json);

                if (string.IsNullOrEmpty(encryptedData))
                {
                    "Failed to encrypt save files data".LogError();
                    return;
                }

                File.WriteAllText(saveFilesDataPath, encryptedData);

                $"Saved {SaveFiles.Count} save file(s) to {saveFilesDataPath}".LogInfo();
            }
            catch (System.Exception ex)
            {
                $"Failed to save files data: {ex.Message}".LogError();
            }
        }

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

            // Switch to the new save file
            ActiveSaveFileSubfolderPath = subfolder;
            string subfolderPath = subfolder.ToString().ToLower();

            // Reinitialize LongTermMemory with the new subfolder
            Brain.PublishLongTermMemorySubfolderSet(subfolderPath);

            $"Switched active save file to: {subfolder} (subfolder: {subfolderPath})".LogInfo();
        }
        #endregion
    }
}
