using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        /// <summary>
        /// Fired once UI input actions have been initialized and are ready for use.
        /// </summary>
        public event Action OnInputsReady;

        /// <summary>
        /// Notify listeners that input actions are ready.
        /// </summary>
        public void NotifyInputsReady()
        {
            OnInputsReady?.Invoke();
        }
    }
}
