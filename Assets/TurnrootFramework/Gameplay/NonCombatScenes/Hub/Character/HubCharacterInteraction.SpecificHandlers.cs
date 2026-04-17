using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Conversations;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public partial class HubCharacterInteraction : MonoBehaviour
    {
        private MonoBehaviour[] _activeItemRows;
        private ObjectItem[] _items;
        private int _activeItemChoiceIndex;
        private Action _activeOnItemChosen;
        private bool _subMenuActive;
        private Conversation _activeChitChatConversation;

        private OneShotPlayer OneShotPlayer =>
            CharacterManager._brain.audioBrain.GetOrCreateOneShotPlayer();

        private int CurrentChapter =>
            CharacterManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;

        #region Menu Helpers

        private void ReturnToActionsMenu(bool persistSupportPoints = true)
        {
            ShowActionsMenu();
            if (persistSupportPoints)
            {
                PersistAvatarSupportPoints();
            }
        }

        private void PersistAvatarSupportPoints()
        {
            var avatar = CharacterManager._brain.gamewideContextBrain.GetOrCreateAvatarInstance();
            if (avatar != null)
            {
                CharacterManager._brain.gamewideContextBrain.PersistCharacter(avatar);
                $"Persisted avatar with updated support points: {avatar.CharacterTemplate.FullName}, {ActiveCharacter.CharacterTemplate.DisplayName}, SupportPoints: {avatar.GetSupportRelationship(ActiveCharacter.CharacterTemplate)?.SupportPoints}".LogInfo();
            }
        }

        #endregion

        #region Action Handlers

        private void HandleTalk()
        {
            InputProvider.OnInput -= HandleInput;
            var conversation = CharacterManager.GetRandomUnplayedChitChatConversation(
                ActiveCharacter,
                CurrentChapter
            );

            if (conversation == null)
            {
                // Exhausted — should not normally reach here since CanChat() guards it,
                // but fall back gracefully.
                ReturnToActionsMenu();
                return;
            }

            _activeChitChatConversation = conversation;

            var controller = UnityEngine.Object.FindFirstObjectByType<ConversationController>();
            if (controller == null)
            {
                $"HubCharacterInteraction: No ConversationController found in scene. Cannot play chitchat conversation.".LogWarning();
                ReturnToActionsMenu();
                return;
            }

            controller.PlayConversationDirect(conversation, OnChitChatFinished);
        }

        private void HandleMeal() { }

        private void HandleSpa() { }

        private void HandleDance() { }

        private void HandleGift() =>
            OpenItemChoiceMenu(
                m => m.IsGiftSubtype(),
                (item, qty) =>
                {
                    var go = Instantiate(GiftItemRowPrefab, ItemChoiceParentContainer.transform);
                    var refs = go.GetComponent<GiftItemRowUiRefs>();
                    refs.Initialize(item, qty);
                    return refs;
                },
                OnGiftChosen
            );

        private void HandleItemInput(string action)
        {
            if (_activeItemRows == null || _activeItemRows.Length == 0)
            {
                return;
            }

            InputProvider.Navigate(
                action,
                _activeItemRows,
                ref _activeItemChoiceIndex,
                _activeItemRows.Length,
                _activeOnItemChosen
            );
        }

        private void HandleLostItem() =>
            OpenItemChoiceMenu(
                m => m.IsLostItemSubtype(),
                (item, qty) =>
                {
                    var go = Instantiate(LostItemRowPrefab, ItemChoiceParentContainer.transform);
                    var refs = go.GetComponent<LostItemUiRowRefs>();
                    refs.Initialize(item, qty);
                    return refs;
                },
                OnLostItemChosen
            );

        private void OpenItemChoiceMenu(
            Func<ObjectItem, bool> filter,
            Func<ObjectItem, int, MonoBehaviour> createAndInitRow,
            Action onChosen
        )
        {
            var container = ItemChoiceParentContainer;
            foreach (Transform child in container.transform)
            {
                Destroy(child.gameObject);
            }

            var materials = CharacterManager._brain.storehouseBrain.GetAllMaterials();
            var rows = new List<MonoBehaviour>();
            var itemList = new List<ObjectItem>();
            foreach (var kv in materials.Where(m => filter(m.Key) && m.Value > 0))
            {
                rows.Add(createAndInitRow(kv.Key, kv.Value));
                itemList.Add(kv.Key);
            }

            _activeItemRows = rows.ToArray();
            _items = itemList.ToArray();
            _activeItemChoiceIndex = 0;
            _activeOnItemChosen = onChosen;
            _subMenuActive = true;

            HideActionsMenu();
            GiftChoiceMenuFade.Show();

            if (_activeItemRows.Length > 0)
            {
                _activeItemRows[0]
                    .BroadcastMessage("Select", SendMessageOptions.DontRequireReceiver);
            }

            InputProvider.OnInput += HandleItemInput;
        }

        private void HandleSupport() { }

        private void HandleRecruit()
        {
            var canRecruit = CharacterManager._brain.charactersBrain.CanRecruit(ActiveCharacter);
            if (canRecruit == false)
            {
                var oneShot = CharacterManager.GetDailyOneShotForType(
                    ActiveCharacter,
                    CurrentChapter,
                    HubCharacterOneShotType.RecruitFail
                );
                var basePoints = GameplayGeneralSettings.Instance.RecruitFailureSupportPoints;
                CharacterManager._brain.charactersBrain.AwardHubSupportPointsAvatarPairing(
                    ActiveCharacter,
                    basePoints
                );
                OneShotPlayer.PlayOneShotThen(oneShot, OnRecruitFailedOneShotFinished);
                return;
            }
            else
            {
                CharacterManager._brain.charactersBrain.Recruit(ActiveCharacter);
                var oneShot = CharacterManager.GetDailyOneShotForType(
                    ActiveCharacter,
                    CurrentChapter,
                    HubCharacterOneShotType.RecruitSucceed
                );
                OneShotPlayer.PlayOneShotThen(oneShot, OnRecruitSucceededOneShotFinished);
            }
        }

        private void HandleTrain() { }

        #endregion

        #region Action Callbacks

        private void OnChitChatFinished()
        {
            if (_activeChitChatConversation != null)
            {
                CharacterManager._brain.conversationalBrain.MarkConversationCompleted(
                    _activeChitChatConversation
                );
                _activeChitChatConversation = null;
            }

            if (ActiveCharacter.CharacterTemplate != null)
            {
                HubDayStateStore.MarkChitChatHappenedToday(
                    CharacterManager._brain,
                    ActiveCharacter.CharacterTemplate.FullName
                );
            }

            CharacterManager._brain.PublishHubCharacterTalked(ActiveCharacter);
            ReturnToActionsMenu(false);
        }

        private void CleanUpItems()
        {
            foreach (Transform child in ItemChoiceParentContainer.transform)
            {
                Destroy(child.gameObject);
            }

            _activeItemRows = null;
            _items = null;
            _subMenuActive = false;
            GiftChoiceMenuFade.Hide();
        }

        private void OnGiftChosen()
        {
            InputProvider.OnInput -= HandleItemInput;
            var chosenGift = _items[_activeItemChoiceIndex];
            // 3. on submit, remove 1x of gift from storehouse
            CharacterManager._brain.storehouseBrain.ConsumeMaterials(chosenGift, 1);
            // clean up gift list
            CleanUpItems();

            // 4. adjust support points
            AdjustSupportPointsBasedOnItem(chosenGift);
            // 5. play reaction one shot
            var oneShot = CharacterManager.GetDailyOneShotForType(
                ActiveCharacter,
                CurrentChapter,
                chosenGift.UnitsLove.Contains(ActiveCharacter.CharacterTemplate)
                    ? HubCharacterOneShotType.GetGiftLove
                    : HubCharacterOneShotType.GetGiftDislike
            );

            OneShotPlayer.PlayOneShotThen(oneShot, OnItemOneshotFinished);
        }

        private void OnLostItemChosen()
        {
            InputProvider.OnInput -= HandleItemInput;
            var chosenLostItem = _items[_activeItemChoiceIndex];
            // IS ITEM THEIRS???
            var isTheirs =
                chosenLostItem.BelongsTo != null
                && chosenLostItem.BelongsTo.Equals(ActiveCharacter.CharacterTemplate);
            if (isTheirs)
            {
                CharacterManager._brain.storehouseBrain.ConsumeMaterials(chosenLostItem, 1);
            }
            // always clean up
            CleanUpItems();

            if (isTheirs)
            {
                AdjustSupportPointsBasedOnItem(chosenLostItem, false);
            }
            // 5. play reaction one shot
            var oneShot = CharacterManager.GetDailyOneShotForType(
                ActiveCharacter,
                CurrentChapter,
                isTheirs
                    ? HubCharacterOneShotType.GetLostItemMine
                    : HubCharacterOneShotType.GetLostItemNotMine
            );

            OneShotPlayer.PlayOneShotThen(oneShot, OnItemOneshotFinished);
        }

        private void OnItemOneshotFinished()
        {
            OneShotPlayer.UnsubscribeOneShotFinished(OnItemOneshotFinished);
            // 7. return back to action choice menu
            ReturnToActionsMenu(true);
            // 8. save storehouse and support points to LTM
            CharacterManager._brain.storehouseBrain.SaveCurrentStorehouse();
        }

        private void OnRecruitFailedOneShotFinished()
        {
            OneShotPlayer.UnsubscribeOneShotFinished(OnRecruitFailedOneShotFinished);
            ReturnToActionsMenu(true);
        }

        private void OnRecruitSucceededOneShotFinished()
        {
            OneShotPlayer.UnsubscribeOneShotFinished(OnRecruitSucceededOneShotFinished);
            CharacterManager._brain.OnHubCharacterRecruitCompleted +=
                OnRecruitCompleteSequenceFinished;
            CharacterManager._brain.charactersBrain.PlayRecruitCompleteSequence(ActiveCharacter);
        }

        private void OnRecruitCompleteSequenceFinished(CharacterInstance _)
        {
            CharacterManager._brain.OnHubCharacterRecruitCompleted -=
                OnRecruitCompleteSequenceFinished;
            ReturnToActionsMenu(true);
        }

        #endregion

        #region Support Point Adjustment

        private void AdjustSupportPointsBasedOnItem(ObjectItem item, bool isGift = true)
        {
            var positive = isGift
                ? GameplayGeneralSettings.Instance.GiftSupportPointsUnitLikes
                : GameplayGeneralSettings.Instance.LostItemIsUnits;
            var negative = isGift
                ? GameplayGeneralSettings.Instance.GiftSupportPointsUnitDislikes
                : GameplayGeneralSettings.Instance.LostItemIsNotUnits;
            var reaction = item.UnitsLove.Contains(ActiveCharacter.CharacterTemplate)
                ? positive
                : negative;
            var basePoints = reaction * item.GiftRank;
            CharacterManager._brain.charactersBrain.AwardHubSupportPointsAvatarPairing(
                ActiveCharacter,
                basePoints
            );
            if (reaction == positive)
            {
                SupportUpTimeline.Play();
            }
        }

        #endregion
    }
}
