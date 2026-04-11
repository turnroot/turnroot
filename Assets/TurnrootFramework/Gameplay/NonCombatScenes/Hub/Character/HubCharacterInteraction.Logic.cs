using System.Linq;
using Turnroot.Conversations;
using Turnroot.Gameplay.Objects.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public partial class HubCharacterInteraction : MonoBehaviour
    {
        #region Menu Management

        public void ShowActionsMenu()
        {
            SetUpActionsMenuChoices();
            ActionsMenuFade.Show();
            BackButtonFade?.Show();
        }

        public void HideActionsMenu()
        {
            ActionsMenuFade.Hide();
            BackButtonFade?.Hide();
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
            var materials = CharacterManager._brain.storehouseBrain.GetAllMaterials();
            return materials.Any(kvp =>
                kvp.Key != null && kvp.Key.IsLostItemSubtype() && kvp.Value > 0
            );
        }

        public bool CanChat()
        {
            if (
                ActiveCharacter?.CharacterTemplate != null
                && HubDayStateStore.HasChitChatHappenedToday(
                    ActiveCharacter.CharacterTemplate.FullName
                )
            )
            {
                return false;
            }

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
            var materials = storehouseBrain.GetAllMaterials();
            return materials.Any(kvp =>
                kvp.Key != null && kvp.Key.Subtype == ObjectSubtype.Gift && kvp.Value > 0
            );
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
                var selectedOption = AllPossibleChoices.First(c =>
                    c.Choice == _navigableChoices[_currentChoiceIndex]
                );

                switch (selectedOption.OptionType)
                {
                    case CharacterInteractionOptionType.Talk:
                        HandleTalk();
                        break;
                    case CharacterInteractionOptionType.Meal:
                        HandleMeal();
                        break;
                    case CharacterInteractionOptionType.Spa:
                        HandleSpa();
                        break;
                    case CharacterInteractionOptionType.Dance:
                        HandleDance();
                        break;
                    case CharacterInteractionOptionType.Gift:
                        HandleGift();
                        break;
                    case CharacterInteractionOptionType.LostItem:
                        HandleLostItem();
                        break;
                    case CharacterInteractionOptionType.Support:
                        HandleSupport();
                        break;
                    case CharacterInteractionOptionType.Recruit:
                        HandleRecruit();
                        break;
                    case CharacterInteractionOptionType.Train:
                        HandleTrain();
                        break;
                }
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

        private void OnDisable()
        {
            var audioBrain =
                CharacterManager._brain != null ? CharacterManager._brain.audioBrain : null;
            if (audioBrain != null)
            {
                audioBrain.GetComponent<OneShotPlayer>()?.ClearPendingCallback();
            }
            if (CharacterManager._brain != null)
            {
                CharacterManager._brain.OnHubCharacterRecruitCompleted -=
                    OnRecruitCompleteSequenceFinished;
            }
        }

        #endregion
    }
}
