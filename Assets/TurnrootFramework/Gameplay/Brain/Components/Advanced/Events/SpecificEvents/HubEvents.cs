using System;
using Turnroot.Characters;
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

        /// <summary>
        /// Fired when the avatar begins interacting with a hub character (on visit, before welcome dialogue).
        /// </summary>
        public event Action<CharacterInstance> OnHubCharacterInteracted;

        /// <summary>
        /// Fired when the avatar completes a ChitChat talk interaction with a hub character.
        /// </summary>
        public event Action<CharacterInstance> OnHubCharacterTalked;

        /// <summary>
        /// Fired when the post-one-shot recruit completion sequence finishes (UI/audio fanfare etc.)
        /// and the hub interaction flow is ready to return to the actions menu.
        /// Subscribe in <see cref="HubCharacterInteraction"/> to gate the return.
        /// </summary>
        public event Action<CharacterInstance> OnHubCharacterRecruitCompleted;

        public void PublishHubSublocationVisited(HubSublocationName name) =>
            OnHubSublocationVisited?.Invoke(name);

        public void PublishHubSublocationTutorialCompleted() =>
            OnHubSublocationTutorialCompleted?.Invoke();

        public void PublishHubSublocationInputModeChange(HubManager.HubInputMode mode) =>
            OnHubSublocationInputModeChange?.Invoke(mode);

        public void PublishHubCharacterInteracted(CharacterInstance character) =>
            OnHubCharacterInteracted?.Invoke(character);

        public void PublishHubCharacterTalked(CharacterInstance character) =>
            OnHubCharacterTalked?.Invoke(character);

        public void PublishHubCharacterRecruitCompleted(CharacterInstance character) =>
            OnHubCharacterRecruitCompleted?.Invoke(character);
    }
}
