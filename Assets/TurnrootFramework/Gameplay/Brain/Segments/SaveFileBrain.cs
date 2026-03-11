using Turnroot.Gameplay.Brain.Events;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages save file operations within the brain framework.
    /// </summary>
    public partial class SaveFileBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnUpdateSaveFileName += HandleUpdateSaveFileName;
            Brain.OnUpdateSaveFileProgress += HandleUpdateSaveFileProgress;
            Brain.OnSetSaveFileCurrentScene += HandleSetSaveFileCurrentScene;
            Brain.OnSwitchActiveSaveFile += HandleSwitchActiveSaveFile;
            Brain.OnSetSaveFileChapter += HandleSetSaveFileChapter;
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
    }
}

