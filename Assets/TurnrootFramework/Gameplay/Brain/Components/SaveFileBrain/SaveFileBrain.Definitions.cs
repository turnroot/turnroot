using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Snapshots;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [System.Serializable]
    public struct SaveFile
    {
        public string FileName;
        public Sprite AvatarPortrait;
        public AvatarBody AvatarBodyType;

        public Color AvatarHairColor;
        public Color AvatarEyeColor;
        public Color AvatarSkinColor;
        public int Progress;
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
    public partial class SaveFileBrain : BrainComponent
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
    }
}
