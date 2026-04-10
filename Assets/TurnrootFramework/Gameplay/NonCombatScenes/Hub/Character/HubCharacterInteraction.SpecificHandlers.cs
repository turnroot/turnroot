using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
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

        private OneShotPlayer OneShotPlayer =>
            CharacterManager._brain.audioBrain.GetOrCreateOneShotPlayer();

        private int CurrentChapter =>
            CharacterManager._brain.saveFileBrain.ActiveSaveFile.ChapterNumber;

        #region Menu Helpers

        private void ResubscribeInput()
        {
            InputProvider.OnInput -= HandleInput;
            InputProvider.OnInput += HandleInput;
        }

        private void ReturnToActionsMenu(bool persistSupportPoints = true)
        {
            ShowActionsMenu();
            ResubscribeInput();
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
            var oneShot = CharacterManager.GetDailyOneShotForType(
                ActiveCharacter,
                CurrentChapter,
                HubCharacterOneShotType.ChitChat
            );
            OneShotPlayer.PlayOneShotThen(oneShot, OnChitChatFinished);
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

        private void HandleLostItem() { }

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
            OneShotPlayer.UnsubscribeOneShotFinished(OnChitChatFinished);
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
            var oneShot = CharacterManager.GetDailyOneShotForType(
                ActiveCharacter,
                CurrentChapter,
                chosenGift.UnitsLove.Contains(ActiveCharacter.CharacterTemplate)
                    ? HubCharacterOneShotType.GetGiftLove
                    : HubCharacterOneShotType.GetGiftDislike
            );

            OneShotPlayer.PlayOneShotThen(oneShot, OnGiftOneshotFinished);
        }

        private void OnGiftOneshotFinished()
        {
            OneShotPlayer.UnsubscribeOneShotFinished(OnGiftOneshotFinished);
            // 6a. support points ui
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

        #region Gift Support Point Adjustment

        private void AdjustSupportPointsBasedOnGift(ObjectItem gift)
        {
            var positive = GameplayGeneralSettings.Instance.GiftSupportPointsUnitLikes;
            var negative = GameplayGeneralSettings.Instance.GiftSupportPointsUnitDislikes;
            var reaction = gift.UnitsLove.Contains(ActiveCharacter.CharacterTemplate)
                ? positive
                : negative;
            $"Gift {gift.Name} given to {ActiveCharacter.CharacterTemplate.DisplayName}, reaction: {reaction}".LogInfo();
            var basePoints = reaction * gift.GiftRank;
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
