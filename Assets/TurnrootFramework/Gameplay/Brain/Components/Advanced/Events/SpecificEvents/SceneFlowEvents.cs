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

        #endregion
    }
}
