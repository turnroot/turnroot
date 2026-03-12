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

        /// <summary>
        /// Triggered when a hub sublocation transition begins and the input mode should
        /// be pushed to the manager.  The associated <see cref="HubInputMode"/>
        /// identifies the new input state.
        /// </summary>
        public event Action<HubManager.HubInputMode> OnHubSublocationInputModeChange;

        public void PublishHubSublocationVisited(HubSublocationName name) =>
            OnHubSublocationVisited?.Invoke(name);

        public void PublishHubSublocationTutorialCompleted() =>
            OnHubSublocationTutorialCompleted?.Invoke();

        public void PublishHubSublocationInputModeChange(HubManager.HubInputMode mode) =>
            OnHubSublocationInputModeChange?.Invoke(mode);
    }
}
