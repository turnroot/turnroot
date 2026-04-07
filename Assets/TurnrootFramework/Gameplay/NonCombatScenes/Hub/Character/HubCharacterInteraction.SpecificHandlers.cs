using System.Linq;
using Turnroot.Characters.Stats;
using Turnroot.Conversations;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
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

        private void HandleGift()
        {
            // 1. pull up gift choice ui (shopui-ish, probably just a VL with instances of ItemRowPrefab)
            // 1a. populate list from storehouse
            // 2. redirect input to that ui until a choice is made
            // 3. on submit, remove 1x of gift from storehouse
            ObjectItem chosenGift = null;
            // 4. check gift reaction
            AdjustSupportPointsBasedOnGift(chosenGift);
            // 5. play reaction one shot
            // 6. adjust support points
            // 6a. support points ui
            // 7. return back to action chioce menu
            // 8.  make sure to save everything in LTM
        }

        private void AdjustSupportPointsBasedOnGift(ObjectItem gift)
        {
            var positive = GameplayGeneralSettings.Instance.GiftSupportPointsUnitLikes;
            var negative = GameplayGeneralSettings.Instance.GiftSupportPointsUnitDislikes;
            var reaction = gift.UnitsLove.Contains(ActiveCharacter.CharacterTemplate)
                ? positive
                : negative;
            var basePoints = reaction * gift.GiftRank;
            CharacterManager._brain.charactersBrain.AwardHubSupportPointsAvatarPairing(
                ActiveCharacter,
                basePoints
            );
        }

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
