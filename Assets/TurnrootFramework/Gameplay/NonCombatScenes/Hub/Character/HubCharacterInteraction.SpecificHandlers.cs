using Turnroot.Conversations;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public partial class HubCharacterInteraction : MonoBehaviour
    {
        private void HandleTalk()
        {
            // Don't allow other inputs until dialogue is done
            InputProvider.OnInput -= HandleInput;

            var currentChapter = CharacterManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;
            var oneShot = CharacterManager.GetDailyOneShotForType(
                ActiveCharacter,
                currentChapter,
                HubCharacterOneShotType.ChitChat
            );

            if (!string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                var cc = FindFirstObjectByType<ConversationController>();
                if (cc != null)
                {
                    _subscribedController = cc;
                    cc.OnAnyConversationFinished.AddListener(OnChitChatFinished);
                }

                CharacterManager
                    ._brain?.audioBrain?.GetOrCreateOneShotPlayer()
                    ?.PlayOneShot(oneShot);
            }
            else
            {
                InputProvider.OnInput += HandleInput;
            }
        }

        private void HandleMeal() { }

        private void HandleSpa() { }

        private void HandleDance() { }

        private void HandleGift() { }

        private void HandleLostItem() { }

        private void HandleSupport() { }

        private void HandleRecruit() { }

        private void HandleTrain() { }

        private void OnChitChatFinished()
        {
            if (_subscribedController != null)
            {
                _subscribedController.OnAnyConversationFinished.RemoveListener(OnChitChatFinished);
                _subscribedController = null;
            }

            if (ActiveCharacter?.CharacterTemplate != null)
            {
                HubDayStateStore.MarkChitChatHappenedToday(
                    CharacterManager._brain,
                    ActiveCharacter.CharacterTemplate.FullName
                );
            }

            CharacterManager._brain?.PublishHubCharacterTalked(ActiveCharacter);
            InputProvider.OnInput += HandleInput;
            SetUpActionsMenuChoices();
        }
    }
}
