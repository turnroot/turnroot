using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities.SceneFlows;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    public partial class DynamicSceneFlow : MonoBehaviour
    {
        #region Save File Management Reroutes
        public void UpdateSaveFileName(string fileName) =>
            brain.PublishUpdateSaveFileName(fileName);

        public void UpdateSaveFileProgress(int progress) =>
            brain.PublishUpdateSaveFileProgress(progress);

        public void SetSaveFileCurrentScene(string sceneName) =>
            brain.PublishSetSaveFileCurrentScene(sceneName);

        public void SwitchActiveSaveFile(SaveFileSubfolders subfolder) =>
            brain.PublishSwitchActiveSaveFile(subfolder);

        #endregion
    }
}