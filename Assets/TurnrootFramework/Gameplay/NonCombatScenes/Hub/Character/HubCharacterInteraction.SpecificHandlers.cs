using System.Collections.Generic;
using System.Linq;
using Turnroot.Conversations;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public partial class HubCharacterInteraction : MonoBehaviour
    {
        private GiftItemRowUiRefs[] _giftChoiceRows;
        private ObjectItem[] _giftItems;
        private int _giftChoiceIndex;

        private void HandleTalk()
        {
            InputProvider.OnInput -= HandleInput;
            var currentChapter = CharacterManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;
            var oneShot = CharacterManager.GetDailyOneShotForType(
                ActiveCharacter,
                currentChapter,
                HubCharacterOneShotType.ChitChat
            );
            PlayOneShotThen(oneShot, OnChitChatFinished);
        }

        private void HandleMeal() { }

        private void HandleSpa() { }

        private void HandleDance() { }

        private void HandleGift()
        {
            // 1. pull up gift choice ui (shopui-ish, probably just a VL with instances of ItemRowPrefab)
            var container = GiftChoiceParentContainer;
            // Clear any leftover rows from a previous visit.
            foreach (Transform child in container.transform)
            {
                Destroy(child.gameObject);
            }
            var storehouse = CharacterManager._brain.storehouseBrain;
            var materials = storehouse.GetAllMaterials();
            var gifts = materials
                .Where(m => m.Key.IsGiftSubtype() && m.Value > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            // 1a. populate list from storehouse
            var rows = new List<GiftItemRowUiRefs>();
            var itemList = new List<ObjectItem>();
            foreach (var gift in gifts)
            {
                var itemRow = Instantiate(GiftItemRowPrefab, container.transform);
                var refs = itemRow.GetComponent<GiftItemRowUiRefs>();
                refs.Initialize(gift.Key, gift.Value);
                rows.Add(refs);
                itemList.Add(gift.Key);
            }
            _giftChoiceRows = rows.ToArray();
            _giftItems = itemList.ToArray();
            _giftChoiceIndex = 0;
            // 2. redirect input to that ui until a choice is made
            InputProvider.OnInput -= HandleInput;
            HideActionsMenu();
            GiftChoiceMenuFade.Show();
            if (_giftChoiceRows.Length > 0)
            {
                _giftChoiceRows[0]
                    .BroadcastMessage("Select", SendMessageOptions.DontRequireReceiver);
            }

            InputProvider.OnInput += HandleGiftInput;
        }

        private void HandleGiftInput(string action)
        {
            if (_giftChoiceRows == null || _giftChoiceRows.Length == 0)
            {
                return;
            }

            InputProvider.Navigate(
                action,
                _giftChoiceRows,
                ref _giftChoiceIndex,
                _giftChoiceRows.Length,
                OnGiftChosen
            );
        }

        private void OnGiftChosen()
        {
            InputProvider.OnInput -= HandleGiftInput;
            var chosenGift = _giftItems[_giftChoiceIndex];
            // 3. on submit, remove 1x of gift from storehouse
            CharacterManager._brain.storehouseBrain.ConsumeMaterials(chosenGift, 1);
            // clean up gift list
            foreach (Transform child in GiftChoiceParentContainer.transform)
            {
                Destroy(child.gameObject);
            }

            _giftChoiceRows = null;
            _giftItems = null;
            GiftChoiceMenuFade.Hide();
            // 4. adjust support points
            AdjustSupportPointsBasedOnGift(chosenGift);
            // 5. play reaction one shot
            var currentChapter = CharacterManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;
            var oneShot = CharacterManager.GetDailyOneShotForType(
                ActiveCharacter,
                currentChapter,
                chosenGift.UnitsLove.Contains(ActiveCharacter.CharacterTemplate)
                    ? HubCharacterOneShotType.GetGiftLove
                    : HubCharacterOneShotType.GetGiftDislike
            );

            PlayOneShotThen(oneShot, OnGiftOneshotFinished);
        }

        private void AdjustSupportPointsBasedOnGift(ObjectItem gift)
        {
            var positive = GameplayGeneralSettings.Instance.GiftSupportPointsUnitLikes;
            var negative = GameplayGeneralSettings.Instance.GiftSupportPointsUnitDislikes;
            var reaction = gift.UnitsLove.Contains(ActiveCharacter.CharacterTemplate)
                ? positive
                : negative;
            $"Gift {gift.Name} given to {ActiveCharacter.CharacterTemplate.DisplayName}, reaction: {reaction}".LogInfo();
            var basePoints = reaction * gift.GiftRank;
            $"Base support points: {basePoints}".LogInfo();
            CharacterManager._brain.charactersBrain.AwardHubSupportPointsAvatarPairing(
                ActiveCharacter,
                basePoints
            );
            if (reaction == positive)
            {
                SupportUpTimeline.Play();
            }
        }

        private void HandleLostItem() { }

        private void HandleSupport() { }

        private void HandleRecruit()
        {
            //
        }

        private void HandleTrain() { }

        private void OnChitChatFinished()
        {
            UnsubscribeOneShotFinished(OnChitChatFinished);
            if (ActiveCharacter?.CharacterTemplate != null)
            {
                HubDayStateStore.MarkChitChatHappenedToday(
                    CharacterManager._brain,
                    ActiveCharacter.CharacterTemplate.FullName
                );
            }
            CharacterManager._brain.PublishHubCharacterTalked(ActiveCharacter);
            InputProvider.OnInput -= HandleInput;
            InputProvider.OnInput += HandleInput;
            SetUpActionsMenuChoices();
        }

        private void OnGiftOneshotFinished()
        {
            UnsubscribeOneShotFinished(OnGiftOneshotFinished);
            // 6a. support points ui
            // 7. return back to action choice menu
            ShowActionsMenu();
            InputProvider.OnInput -= HandleInput;
            InputProvider.OnInput += HandleInput;
            // 8. save storehouse and support points to LTM
            CharacterManager._brain.storehouseBrain.SaveCurrentStorehouse();
            var avatar = CharacterManager._brain.gamewideContextBrain?.GetOrCreateAvatarInstance();
            if (avatar != null)
            {
                CharacterManager._brain.gamewideContextBrain.PersistCharacter(avatar);
                $"Persisted avatar with updated support points: {avatar.CharacterTemplate.FullName}, {ActiveCharacter.CharacterTemplate.DisplayName}, SupportPoints: {avatar.GetSupportRelationship(ActiveCharacter.CharacterTemplate)?.SupportPoints}".LogInfo();
            }
        }

        private void PlayOneShotThen(OneShot oneShot, UnityAction onFinished)
        {
            if (!string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                var cc = FindFirstObjectByType<ConversationController>();
                if (cc != null)
                {
                    _subscribedController = cc;
                    cc.OnAnyConversationFinished.AddListener(onFinished);
                }
                CharacterManager
                    ._brain?.audioBrain?.GetOrCreateOneShotPlayer()
                    ?.PlayOneShot(oneShot);
            }
            else
            {
                onFinished?.Invoke();
            }
        }

        private void UnsubscribeOneShotFinished(UnityAction onFinished)
        {
            if (_subscribedController != null)
            {
                _subscribedController.OnAnyConversationFinished.RemoveListener(onFinished);
                _subscribedController = null;
            }
        }
    }
}
