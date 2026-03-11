using System;
using Turnroot.Gameplay.NonCombatScenes.Hub;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        // events related to visiting and tutorial completion inside hub sublocations

        /// <summary>
        /// Fired when the player visits a hub sublocation (Market, Cafe, etc.).
        /// The <see cref="HubSublocationName"/> identifies which location.
        /// </summary>
        public event Action<HubSublocationName> OnHubSublocationVisited;

        /// <summary>
        /// Fired when a sublocation tutorial finishes, so callers can re-enable input
        /// or progress story logic.
        /// </summary>
        public event Action OnHubSublocationTutorialCompleted;

        public void PublishHubSublocationVisited(HubSublocationName name) =>
            OnHubSublocationVisited?.Invoke(name);

        public void PublishHubSublocationTutorialCompleted() =>
            OnHubSublocationTutorialCompleted?.Invoke();
    }
}
