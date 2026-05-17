using System;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        public static event Action<Brain> OnBrainReady;

        public static void PublishBrainReady(Brain brain) => OnBrainReady?.Invoke(brain);
    }
}
