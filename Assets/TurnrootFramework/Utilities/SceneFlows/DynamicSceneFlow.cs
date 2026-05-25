using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Represents a state segment in a scene flow with an ID and event callbacks.
    /// </summary>
    [Serializable]
    public class FlowSegment
    {
        public string stateId = "";
        public UnityEvent onSegmentReached;
    }

    public enum SceneFlowFlagTriggerTiming
    {
        SceneStart,
        SceneEnd,
        BattleCompleted,
        UnitRecruited,
        SupportLevelChanged,
    }

    public enum SceneFlowFlagKeySource
    {
        Existing,
        Custom,
    }

    [Serializable]
    public struct SceneFlowFlagTrigger
    {
        public SceneFlowFlagTriggerTiming timing;
        public SceneFlowFlagKeySource keySource;
        public string existingKey;
        public string customKey;
        public bool value;

        public string ResolveKey() =>
            keySource == SceneFlowFlagKeySource.Custom ? customKey : existingKey;
    }

    /// <summary>
    /// Manages sequential scene flow progression through defined state segments.
    /// </summary>
    public class DynamicSceneFlow : MonoBehaviour
    {
        public List<FlowSegment> segments = new();

        [Header("Scene Flow Flags")]
        [Tooltip(
            "Flag updates to apply to SceneFlowBrain when the selected runtime timing event occurs."
        )]
        public List<SceneFlowFlagTrigger> sceneFlowFlagTriggers = new();

        protected int _index = 0;
        public FlowSegment CurrentSegment => segments.Count > _index ? segments[_index] : null;

        [HideInInspector]
        public Brain brain;

        [HideInInspector]
        public LoadingController loadingController;

        [HideInInspector]
        public ConversationController conversationController;

        // Progress (0..1) for UI elements that expect normalized values
        public UnityEvent<float> OnLoadedAmountChanged = new();
        public event Action<float> OnLoadedAmountChangedAction;

        public UnityEvent StartPreLoading = new();
        public UnityEvent SceneReadyAfterLoad = new();

        private int _lastInvokedIndex = -1;
        private bool _sceneStartTriggersApplied;
        private bool _brainEventsSubscribed;

        protected virtual void Awake()
        {
            Brain.OnBrainReady += HandleBrainReady;
            TryBindBrain(FindFirstObjectByType<Brain>());
        }

        protected virtual void OnEnable() => TryBindBrain(brain ?? FindFirstObjectByType<Brain>());

        protected virtual void OnDisable()
        {
            UnsubscribeFromBrainEvents();
            UnsubscribeFromLoadingController();
        }

        protected virtual void OnDestroy()
        {
            Brain.OnBrainReady -= HandleBrainReady;
            UnsubscribeFromBrainEvents();
            UnsubscribeFromLoadingController();
        }

        private void TryBindBrain(Brain candidateBrain)
        {
            if (candidateBrain == null || ReferenceEquals(candidateBrain, brain))
            {
                return;
            }

            brain = candidateBrain;
            loadingController = brain.GetComponent<LoadingController>();

            SubscribeToBrainEvents();
            SubscribeToLoadingController();

            ApplySceneStartTriggersIfNeeded();
        }

        protected int Index
        {
            get => _index;
            set
            {
                StopAllCoroutines();
                _index = value;
                OnSegmentReached(_index);
            }
        }

        protected void Start()
        {
            ApplySceneStartTriggersIfNeeded();
            _ = StartCoroutine(RunNextFrame(StartScene));
        }

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
            if (loadingController != null)
            {
                loadingController.OnProgressChanged += HandleLoadingProgressChanged;
            }
        }

        protected void UnsubscribeFromLoadingController()
        {
            if (loadingController != null)
            {
                loadingController.OnProgressChanged -= HandleLoadingProgressChanged;
            }
        }

        protected void HandleLoadingProgressChanged(float percentage) =>
            ReportLoadingProgress(percentage);

        protected void HandleSceneLoadProgress(float progress) => ReportLoadingProgress(progress);

        protected void HandleSceneChanged(string sceneName, string displayName) =>
            ApplySceneStartTriggersIfNeeded();

        protected void HandleSceneTransitionStarted(string sceneName, string displayName) =>
            ApplyFlagTriggers(SceneFlowFlagTriggerTiming.SceneEnd);

        protected void HandleSceneTransitionCompleted(string sceneName, string displayName) =>
            SceneReadyAfterLoad?.Invoke();

        protected void HandleBattleCompleted(Turnroot.Gameplay.Combat.BattleExitType exitType) =>
            ApplyFlagTriggers(SceneFlowFlagTriggerTiming.BattleCompleted);

        protected void HandleHubCharacterRecruitCompleted(CharacterInstance character) =>
            ApplyFlagTriggers(SceneFlowFlagTriggerTiming.UnitRecruited);

        protected void HandleSupportLevelIncreased(
            CharacterInstance source,
            SupportRelationshipInstance relationship
        ) => ApplyFlagTriggers(SceneFlowFlagTriggerTiming.SupportLevelChanged);
        #endregion

        protected virtual void ApplyFlagTriggers(SceneFlowFlagTriggerTiming timing)
        {
            if (sceneFlowFlagTriggers == null || sceneFlowFlagTriggers.Count == 0)
            {
                return;
            }

            var flowBrain = brain?.sceneFlowBrain;
            if (flowBrain == null)
            {
                return;
            }

            for (int i = 0; i < sceneFlowFlagTriggers.Count; i++)
            {
                var trigger = sceneFlowFlagTriggers[i];
                if (trigger.timing != timing)
                {
                    continue;
                }

                string key = trigger.ResolveKey();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                flowBrain.SetCustomFlag(key, trigger.value);
            }
        }

        protected void ApplySceneStartTriggersIfNeeded()
        {
            if (_sceneStartTriggersApplied)
            {
                return;
            }

            var flowBrain = brain?.sceneFlowBrain;
            if (flowBrain == null)
            {
                return;
            }

            _sceneStartTriggersApplied = true;
            ApplyFlagTriggers(SceneFlowFlagTriggerTiming.SceneStart);
        }

        private void HandleBrainReady(Brain readyBrain) => TryBindBrain(readyBrain);

        public void ReportLoadingProgress(float percentage)
        {
            OnLoadedAmountChanged?.Invoke(percentage);
            OnLoadedAmountChangedAction?.Invoke(percentage);
        }

        #region Save File Management Reroutes
        public void UpdateSaveFileName(string fileName) =>
            brain.PublishUpdateSaveFileName(fileName);

        public void UpdateSaveFileProgress(int progress) =>
            brain.PublishUpdateSaveFileProgress(progress);

        public void SetSaveFileCurrentScene(string sceneName) =>
            brain.PublishSetSaveFileCurrentScene(sceneName);

        public void SwitchActiveSaveFile(SaveFileSubfolders subfolder) =>
            brain.PublishSwitchActiveSaveFile(subfolder);

        #endregion

        #region Conversation Management Reroutes

        public void StartConversation() => conversationController?.StartConversation();

        public void AdvanceConversation() => conversationController?.NextLayer();

        public void StartConversationAtIndex(int index) =>
            conversationController?.StartConversationAtIndex(index);

        public void NextConversation() => conversationController?.IncrementConversationIndex();

        public void PreviousConversation() => conversationController?.DecrementConversationIndex();

        public void ChooseBranch(int targetNodeId) =>
            conversationController?.ChooseBranchTarget(targetNodeId);

        #endregion

        protected void StartScene()
        {
            ApplySceneStartTriggersIfNeeded();
            Index = 0;

            if (CurrentSegment != null && !string.IsNullOrEmpty(CurrentSegment.stateId))
            {
                $"DynamicSceneFlow: Starting scene with state '{CurrentSegment.stateId}'".LogInfo();
                SetBrainStateFromSegment(CurrentSegment.stateId);
            }
            else
            {
                $"DynamicSceneFlow: Starting scene but no segment state defined (segments: {segments.Count})".LogWarning();
            }
        }

        protected void SetBrainStateFromSegment(string stateId)
        {
            if (brain?.stateBrain == null || string.IsNullOrEmpty(stateId))
            {
                if (brain == null)
                {
                    "DynamicSceneFlow: Cannot set state - brain is null".LogError(
                        "DynamicSceneFlow.SetBrainStateFromSegment"
                    );
                }
                else if (brain.stateBrain == null)
                {
                    "DynamicSceneFlow: Cannot set state - stateBrain is null".LogError(
                        "DynamicSceneFlow.SetBrainStateFromSegment"
                    );
                }
                return;
            }

            if (stateId.Contains("."))
            {
                var parts = stateId.Split('.');
                if (parts.Length == 2)
                {
                    $"DynamicSceneFlow: Activating child state {parts[0]}.{parts[1]}".LogInfo();
                    brain.stateBrain.ActivateChildStateByFullPath(parts[0], parts[1]);
                    return;
                }
            }

            brain.stateBrain.ActivateHighLevelState(stateId);
        }

        protected void HandleStateChanged(BrainState newState)
        {
            if (newState != null)
            {
                ActivateSegmentByState(newState);
            }
        }

        public void ActivateSegmentByState(BrainState state)
        {
            string fullStatePath = state.GetFullPath() ?? "";
            int foundIndex = segments.FindIndex(s => s.stateId == fullStatePath);

            if (foundIndex != -1)
            {
                Index = foundIndex;
            }
        }

        public OperationResult Progress()
        {
            if (Index + 1 < segments.Count)
            {
                Index++;
                var segment = CurrentSegment;
                if (segment != null && !string.IsNullOrEmpty(segment.stateId))
                {
                    SetBrainStateFromSegment(segment.stateId);
                }
                return OperationResult.Successful();
            }
            return OperationResult.Failure("No more segments to progress to.");
        }

        protected virtual void OnSegmentReached(int segmentIndex)
        {
            // ignore if same segment invoked consecutively
            if (segmentIndex == _lastInvokedIndex)
            {
                $"DynamicSceneFlow: skipping repeated segment {segmentIndex}".LogInfo();
                return;
            }

            _lastInvokedIndex = segmentIndex;

            if (segmentIndex < segments.Count)
            {
                CurrentSegment?.onSegmentReached?.Invoke();
            }
        }

        protected IEnumerator RunNextFrame(Action action)
        {
            yield return null;
            action();
        }
    }
}
