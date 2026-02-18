using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region State Events

        public event Action<BrainState> OnPaused;
        public event Action<BrainState> OnResumed;
        public event Action<BrainState> OnStateChanged;
        public event Action OnGameOver;
        public event Action OnHighLevelStatesInitialized;

        public void PublishPaused(BrainState prev) => OnPaused?.Invoke(prev);

        public void PublishResumed(BrainState prev) => OnResumed?.Invoke(prev);

        public void PublishStateChanged(BrainState newState) => OnStateChanged?.Invoke(newState);

        public void PublishGameOver() => OnGameOver?.Invoke();

        public void PublishHighLevelStatesInitialized() => OnHighLevelStatesInitialized?.Invoke();

        #endregion
    }
}
