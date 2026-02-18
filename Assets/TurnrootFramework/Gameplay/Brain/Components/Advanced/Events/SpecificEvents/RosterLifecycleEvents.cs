using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Roster Lifecycle Events

        public event Action OnRostersReady;
        public event Action OnRostersFailed;

        public void PublishRostersReady() => OnRostersReady?.Invoke();

        public void PublishRostersFailed() => OnRostersFailed?.Invoke();

        #endregion
    }
}
