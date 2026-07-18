using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        public static event Action<Brain> OnBrainReady;
        public static Brain ReadyBrain { get; private set; }

        public bool IsFullyInitialized { get; private set; }

        public static void PublishBrainReady(Brain brain)
        {
            if (brain == null)
            {
                return;
            }

            brain.IsFullyInitialized = true;
            ReadyBrain = brain;
            OnBrainReady?.Invoke(brain);
        }

        private void ClearBrainReadyState()
        {
            IsFullyInitialized = false;
            if (ReferenceEquals(ReadyBrain, this))
            {
                ReadyBrain = null;
            }
        }
    }
}
