using System.Collections.Generic;
using System.IO;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    public partial class SaveFileBrain
    {
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
    }
}
