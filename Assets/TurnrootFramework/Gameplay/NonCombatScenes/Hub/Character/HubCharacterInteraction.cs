using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public enum CharacterInteractionOptionType
    {
        Train,
        Talk,
        Meal,
        Spa,
        Dance,
        Gift,
        LostItem,
        Support,
        Recruit,
    }

    [System.Serializable]
    public struct CharacterInteractionOption
    {
        public UiChoice Choice;
        public CharacterInteractionOptionType OptionType;
    }

    [RequireComponent(typeof(HubCharacterManager))]
    public class HubCharacterInteraction : MonoBehaviour
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

        #endregion

        #region Runtime State

        public CharacterInstance ActiveCharacter { get; private set; }

        private UiChoice[] _navigableChoices;
        private int _currentChoiceIndex;

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

        #region Menu Management

        public void ShowActionsMenu()
        {
            SetUpActionsMenuChoices();
            ActionsMenuFade.Show();
        }

        public void HideActionsMenu()
        {
            ActionsMenuFade.Hide();
        }

        public void SetUpActionsMenuChoices()
        {
            if (ActiveCharacter == null)
            {
                $"Trying to set up actions menu choices for {gameObject.name} but ActiveCharacter is null.".LogError();
                return;
            }

            foreach (var option in AllPossibleChoices)
            {
                option.Choice?.gameObject.SetActive(false);
            }

            _navigableChoices = AllPossibleChoices
                .Where(c =>
                    c.OptionType switch
                    {
                        CharacterInteractionOptionType.Train => CanTrain,
                        CharacterInteractionOptionType.Talk => CanChat(),
                        CharacterInteractionOptionType.Meal => CanGoToMeal,
                        CharacterInteractionOptionType.Spa => CanGoToSpa,
                        CharacterInteractionOptionType.Dance => CanGoToDance,
                        CharacterInteractionOptionType.Gift => CanGiveGift(),
                        CharacterInteractionOptionType.LostItem => CanTryLostItem(),
                        CharacterInteractionOptionType.Support => CanSupport(),
                        CharacterInteractionOptionType.Recruit => CanTryRecruit(),
                        _ => false,
                    }
                )
                .Select(c => c.Choice)
                .ToArray();

            foreach (var choice in _navigableChoices)
            {
                choice?.gameObject.SetActive(true);
            }

            _currentChoiceIndex = 0;
            UpdateChoiceSelection();
        }

        private void UpdateChoiceSelection()
        {
            if (_navigableChoices == null || _navigableChoices.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _navigableChoices.Length; i++)
            {
                if (_navigableChoices[i] == null)
                {
                    continue;
                }

                if (i == _currentChoiceIndex)
                {
                    _navigableChoices[i].Select();
                }
                else
                {
                    _navigableChoices[i].Deselect();
                }
            }
        }

        #endregion

        #region Availability Checks

        public bool CanTrain;

        public bool CanSupport()
        {
            if (ActiveCharacter == null)
            {
                return false;
            }

            var avatar = CharacterManager._brain?.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar == null)
            {
                return false;
            }

            var rel = avatar.GetSupportRelationship(ActiveCharacter.CharacterTemplate);
            return rel != null && rel.SupportPoints >= 100;
        }

        public bool CanGoToMeal;
        public bool CanGoToSpa;
        public bool CanGoToDance;

        public bool CanTryLostItem()
        {
            var storehouseBrain = CharacterManager._brain.storehouseBrain;
            var items = storehouseBrain.GetStoredItems();
            return items.Any(i =>
                i?.Template != null && i.Template.Subtype == ObjectSubtype.LostItem
            );
        }

        public bool CanChat()
        {
            var oneShots = CharacterManager.ChapterOneshots;
            var currentChapter = CharacterManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;
            var chapterOneShots = oneShots.FirstOrDefault(c => c.ChapterNumber == currentChapter);
            if (chapterOneShots.Entries == null)
            {
                return false;
            }

            var chitChatOneShots = chapterOneShots.Entries.Where(e =>
                e.Type == HubCharacterOneShotType.ChitChat
            );
            return chitChatOneShots.Any(e => e.Character == ActiveCharacter.CharacterTemplate);
        }

        public bool CanGiveGift()
        {
            var storehouseBrain = CharacterManager._brain.storehouseBrain;
            var items = storehouseBrain.GetStoredItems();
            return items.Any(i => i?.Template != null && i.Template.Subtype == ObjectSubtype.Gift);
        }

        public bool CanTryRecruit()
        {
            if (ActiveCharacter == null)
            {
                return false;
            }

            var rosterInstance =
                CharacterManager._brain.gamewideContextBrain.GetPersistentPlayerTeamRosterInstance();
            var alreadyRecruited =
                rosterInstance != null
                && rosterInstance.Instances.Any(u =>
                    u?.CharacterTemplate == ActiveCharacter.CharacterTemplate
                );

            return !alreadyRecruited && ActiveCharacter.CharacterTemplate.IsRecruitable;
        }

        #endregion

        #region Input

        public void HandleInput(string action)
        {
            if (action is "Back" or InputActionConstants.Cancel)
            {
                CharacterManager.NotifyCharacterExited();
                return;
            }

            if (_navigableChoices == null || _navigableChoices.Length == 0)
            {
                return;
            }

            if (
                action
                is InputActionConstants.Submit
                    or InputActionConstants.Select
                    or InputActionConstants.Confirm
                    or InputActionConstants.Start
            )
            {
                _navigableChoices[_currentChoiceIndex]
                    ?.BroadcastMessage("Select", SendMessageOptions.DontRequireReceiver);
                return;
            }

            InputProvider.Navigate(
                action,
                _navigableChoices,
                ref _currentChoiceIndex,
                _navigableChoices.Length,
                () =>
                    _navigableChoices[_currentChoiceIndex]
                        ?.BroadcastMessage("Select", SendMessageOptions.DontRequireReceiver)
            );

            UpdateChoiceSelection();
        }

        #endregion
    }
}
