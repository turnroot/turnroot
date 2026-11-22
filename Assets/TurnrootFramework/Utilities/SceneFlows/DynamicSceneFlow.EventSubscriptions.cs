using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities.SceneFlows;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    public partial class DynamicSceneFlow : MonoBehaviour
    {
        #region Event Subscriptions
        protected virtual void SubscribeToBrainEvents()
        {
            if (brain != null && !_brainEventsSubscribed)
            {
                brain.OnStateChanged += HandleStateChanged;
                brain.OnSceneLoadProgress += HandleSceneLoadProgress;
                brain.OnSceneChanged += HandleSceneChanged;
                brain.OnSceneTransitionStarted += HandleSceneTransitionStarted;
                brain.OnSceneTransitionCompleted += HandleSceneTransitionCompleted;
                brain.OnBattleCompleted += HandleBattleCompleted;
                brain.OnHubCharacterRecruitCompleted += HandleHubCharacterRecruitCompleted;
                brain.OnSupportLevelIncreased += HandleSupportLevelIncreased;
                _brainEventsSubscribed = true;
            }
        }

        protected virtual void UnsubscribeFromBrainEvents()
        {
            if (brain != null && _brainEventsSubscribed)
            {
                brain.OnStateChanged -= HandleStateChanged;
                brain.OnSceneLoadProgress -= HandleSceneLoadProgress;
                brain.OnSceneChanged -= HandleSceneChanged;
                brain.OnSceneTransitionStarted -= HandleSceneTransitionStarted;
                brain.OnSceneTransitionCompleted -= HandleSceneTransitionCompleted;
                brain.OnBattleCompleted -= HandleBattleCompleted;
                brain.OnHubCharacterRecruitCompleted -= HandleHubCharacterRecruitCompleted;
                brain.OnSupportLevelIncreased -= HandleSupportLevelIncreased;
                _brainEventsSubscribed = false;
            }
        }

        protected void SubscribeToLoadingController()
        {
            if (loadingController != null && !_loadingControllerSubscribed)
            {
                loadingController.OnProgressChanged += HandleLoadingProgressChanged;
                _loadingControllerSubscribed = true;
            }
        }

        protected void UnsubscribeFromLoadingController()
        {
            if (loadingController != null && _loadingControllerSubscribed)
            {
                loadingController.OnProgressChanged -= HandleLoadingProgressChanged;
                _loadingControllerSubscribed = false;
            }
        }

        protected void HandleLoadingProgressChanged(float percentage) =>
            ReportLoadingProgress(percentage);

        protected void HandleSceneLoadProgress(float progress) => ReportLoadingProgress(progress);

        protected void HandleSceneChanged(string sceneName, string displayName) => StartScene();

        protected void HandleSceneTransitionStarted(string sceneName, string displayName) =>
            ApplyFlagTriggers(SceneFlowFlagTriggerTiming.SceneEnd);

        protected void HandleSceneTransitionCompleted(string sceneName, string displayName)
        {
            if (_pendingLoadingScreenRestore != null)
            {
                _pendingLoadingScreenRestore.showOnSceneTransitionStart = true;
                _pendingLoadingScreenRestore = null;
            }
            SceneReadyAfterLoad?.Invoke();
        }

        protected void HandleBattleCompleted(Gameplay.Combat.BattleExitType exitType) =>
            ApplyFlagTriggers(SceneFlowFlagTriggerTiming.BattleCompleted);

        protected void HandleHubCharacterRecruitCompleted(CharacterInstance character) =>
            ApplyFlagTriggers(SceneFlowFlagTriggerTiming.UnitRecruited);

        protected void HandleSupportLevelIncreased(
            CharacterInstance source,
            SupportRelationshipInstance relationship
        ) => ApplyFlagTriggers(SceneFlowFlagTriggerTiming.SupportLevelChanged);
        #endregion
    }
}