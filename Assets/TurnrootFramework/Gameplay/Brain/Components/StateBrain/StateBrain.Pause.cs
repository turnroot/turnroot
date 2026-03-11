using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;

namespace Turnroot.Gameplay.Brain
{
    public partial class StateBrain : BrainComponent
    {
        #region Pause/Resume

        public void Pause() => SetPausedState(true);

        public void Resume() => SetPausedState(false);

        private bool SetPausedState(bool isPaused)
        {
            var pausedState = FindHighLevelState(BrainStateNames.Paused);
            if (pausedState == null)
            {
                return OperationResult.Failure("Paused state not found.").Success;
            }

            if (isPaused)
            {
                _savedStateBeforePause = CurrentState;
                SetCurrentState(pausedState);
                TimeManager.PauseGame();
                Brain.PublishPaused(_savedStateBeforePause);
            }
            else
            {
                if (_savedStateBeforePause != null)
                {
                    SetCurrentState(_savedStateBeforePause);
                    TimeManager.ResumeGame();
                    Brain.PublishResumed(_savedStateBeforePause);
                    _savedStateBeforePause = null;
                }
                else if (CurrentState != null)
                {
                    CurrentState.IsActive = false;
                }
            }

            return true;
        }

        #endregion
    }
}
