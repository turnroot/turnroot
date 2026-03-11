using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class SaveFileBrain
    {
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

        private void HandleSceneChanged(string sceneName, string displayName) =>
            // simply trigger a save so the playtime is updated before the new
            // scene begins.  we don't change the save file's current scene here
            // (that is handled by other brain events) – this is purely for time
            // accounting.
            SaveAllFiles();

        #endregion
    }
}
