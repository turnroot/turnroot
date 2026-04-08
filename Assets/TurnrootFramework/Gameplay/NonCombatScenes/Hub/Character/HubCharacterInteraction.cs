using Turnroot.Characters;
using Turnroot.Conversations;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    [RequireComponent(typeof(HubCharacterManager))]
    public partial class HubCharacterInteraction : MonoBehaviour
    {
        #region Inspector

        [Tooltip("UIFade for the character actions menu shown after the welcome dialogue.")]
        public UIFade ActionsMenuFade;

        [Tooltip(
            "All possible choices to show in the actions menu for this character. The actual shown choices is a dynamic subset."
        )]
        public CharacterInteractionOption[] AllPossibleChoices;

        public UiInputProvider InputProvider;
        public UIFade BackButtonFade;
        public GameObject GiftChoiceParentContainer;
        public GameObject GiftItemRowPrefab;
        public UIFade GiftChoiceMenuFade;
        public int MaxVisibleGiftChoices = 8;

        #endregion

        #region Runtime State

        public CharacterInstance ActiveCharacter { get; private set; }

        private UiChoice[] _navigableChoices;
        private int _currentChoiceIndex;
        private ConversationController _subscribedController;

        #endregion

        #region Properties

        public HubCharacterManager CharacterManager => GetComponent<HubCharacterManager>();

        #endregion

        #region Initialization

        public void Initialize(CharacterInstance character)
        {
            ActiveCharacter = character;
            if (InputProvider == null || BackButtonFade == null || ActionsMenuFade == null)
            {
                $"HubCharacterInteraction on {gameObject.name} is missing a reference.".LogError();
            }
        }

        #endregion
    }
}
