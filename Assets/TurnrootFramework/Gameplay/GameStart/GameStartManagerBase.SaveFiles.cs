using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.GameStart
{
    public abstract partial class GameStartManagerBase
    {
        /// <summary>
        /// Populates each save file slot from <see cref="SaveFileSlots"/> using data from
        /// <see cref="SaveFileBrain.SaveFiles"/>. Also selects the currently active slot.
        /// </summary>
        /// <param name="preserveIndex">
        /// When <c>true</c> the current <see cref="currentIndex"/> is retained and the
        /// selection visual refreshed in-place (used when a playtime tick fires while the
        /// player is already navigating the list).
        /// </param>
        protected void InitializeSaveFiles(bool preserveIndex = false)
        {
            if (SaveFileSlots == null || SaveFileSlots.Length == 0)
            {
                "GameStartManagerBase: No SaveFileSlots assigned.".LogWarning(
                    "GameStartManagerBase"
                );
                return;
            }

            int remembered = currentIndex;
            var saveFiles = saveFileBrain.SaveFiles;

            for (int i = 0; i < SaveFileSlots.Length; i++)
            {
                var slot = SaveFileSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (i < saveFiles.Count)
                {
                    slot.UpdateDisplay(saveFiles[i]);

                    // auto-select the active subfolder's slot on a fresh init
                    if ((int)saveFileBrain.ActiveSaveFileSubfolderPath == i && !preserveIndex)
                    {
                        slot.Select();
                        currentIndex = i;
                    }
                    else
                    {
                        slot.Deselect();
                    }
                }
                else
                {
                    // empty/new slot
                    slot.UpdateDisplay(new SaveFile());
                    slot.Deselect();

                    if (SaveFileSlots[0] != null && !preserveIndex)
                    {
                        SaveFileSlots[0].Select();
                    }
                }
            }

            if (preserveIndex && _currentInputMode == InputModeNames.SaveFiles)
            {
                currentIndex = Mathf.Clamp(remembered, 0, SaveFileSlots.Length - 1);
                UpdateSaveFileSelectionVisual();
            }
        }

        private void UpdateSaveFileSelectionVisual()
        {
            for (int i = 0; i < SaveFileSlots.Length; i++)
            {
                var slot = SaveFileSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (i == currentIndex)
                {
                    slot.Select();
                }
                else
                {
                    slot.Deselect();
                }
            }
        }

        private void HandleSaveFileInput(string action)
        {
            if (_currentInputMode != InputModeNames.SaveFiles || !enabled)
            {
                return;
            }

            if (InputProvider == null)
            {
                return;
            }

            InputProvider.Navigate(
                action,
                SaveFileSlots,
                ref currentIndex,
                SaveFileSlots.Length,
                () =>
                {
                    if (currentIndex >= saveFileBrain.SaveFiles.Count)
                    {
                        var subfolderEnum = (SaveFileSubfolders)currentIndex;
                        saveFileBrain.CreateNewSaveFile(subfolderEnum);
                        currentIndex = saveFileBrain.SaveFiles.Count - 1;
                        InitializeSaveFiles();
                    }

                    var selectedSaveFile = saveFileBrain.SaveFiles[currentIndex];

                    if (
                        System.Enum.TryParse<SaveFileSubfolders>(
                            selectedSaveFile.LtmSubfolderPath,
                            ignoreCase: true,
                            out var subfolder
                        )
                    )
                    {
                        saveFileBrain.ActiveSaveFileSubfolderPath = subfolder;
                        saveFileBrain.Brain.PublishLongTermMemorySubfolderSet(
                            selectedSaveFile.LtmSubfolderPath
                        );
                    }

                    SetInputMode(InputModeNames.None);
                    enabled = false;
                    SaveFilesFade?.Hide();

                    if (
                        !string.IsNullOrEmpty(selectedSaveFile.FileName)
                        && selectedSaveFile.FileName != "Unnamed"
                    )
                    {
                        "GameStartManagerBase: Loading existing save file.".LogInfo(
                            "GameStartManagerBase"
                        );
                        OnExistingSaveFileChosen();
                    }
                    else
                    {
                        OnNewSaveFileChosen();
                    }
                }
            );
        }
    }
}
