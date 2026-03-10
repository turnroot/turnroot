using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Scene Flow Events

        /// <summary>
        /// Fired when a scene transition begins.
        /// </summary>
        public event Action<string, string> OnSceneTransitionStarted;

        /// <summary>
        /// Fired when a scene transition completes successfully.
        /// </summary>
        public event Action<string, string> OnSceneTransitionCompleted;

        /// <summary>
        /// Fired when a scene transition is blocked (conditions not met).
        /// </summary>
        public event Action<string, string> OnSceneTransitionBlocked;

        /// <summary>
        /// Fired when the current scene changes.
        /// </summary>
        public event Action<string, string> OnSceneChanged;

        /// <summary>
        /// Fired during scene loading to report progress (0.0 to 1.0).
        /// </summary>
        public event Action<float> OnSceneLoadProgress;

        /// <summary>
        /// Fired when scene loading is complete and the new scene is ready to be displayed.
        /// UI should hide loading screens in response to this event.
        /// Published before the old scene is unloaded.
        /// </summary>
        public event Action<string, string> OnSceneReadyToDisplay;

        // fired whenever the in‑game calendar date is written to long‑term memory
        public event Action<int, int, int> OnGameDateChanged;

        public void PublishSceneTransitionStarted(string sceneName, string displayName) =>
            OnSceneTransitionStarted?.Invoke(sceneName, displayName);

        public void PublishSceneTransitionCompleted(string sceneName, string displayName) =>
            OnSceneTransitionCompleted?.Invoke(sceneName, displayName);

        public void PublishSceneTransitionBlocked(string sceneName, string reason) =>
            OnSceneTransitionBlocked?.Invoke(sceneName, reason);

        public void PublishSceneChanged(string sceneName, string displayName) =>
            OnSceneChanged?.Invoke(sceneName, displayName);

        public void PublishSceneLoadProgress(float progress) =>
            OnSceneLoadProgress?.Invoke(progress);

        public void PublishSceneReadyToDisplay(string sceneName, string displayName) =>
            OnSceneReadyToDisplay?.Invoke(sceneName, displayName);

        public void PublishGameDateChanged(int year, int month, int day) =>
            OnGameDateChanged?.Invoke(year, month, day);

        #endregion
    }
}
