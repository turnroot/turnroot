using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    [Serializable]
    public class FlowSegment
    {
        public string stateId = "";
        public UnityEvent onSegmentReached;
    }

    public class DynamicSceneFlow : MonoBehaviour
    {
        public List<FlowSegment> segments = new();
        private int _index = 0;
        public FlowSegment CurrentSegment => segments.Count > _index ? segments[_index] : null;

        [HideInInspector]
        public Brain brain;

        [HideInInspector]
        public LoadingController loadingController;

        public UnityEvent<int> OnLoadedAmountChanged = new();
        public event Action<int> OnLoadedAmountChangedAction;

        public UnityEvent StartPreLoading = new();

        // Float-based progress (0..1) for UI elements that expect normalized values
        public UnityEvent<float> OnLoadedAmountChangedFloat = new();
        public event Action<float> OnLoadedAmountChangedActionFloat;

        private int Index
        {
            get => _index;
            set
            {
                StopAllCoroutines();
                _index = value;
                OnSegmentReached(_index);
            }
        }

        private void Start()
        {
            loadingController = brain?.GetComponent<LoadingController>();
            SubscribeToBrainEvents();
            SubscribeToLoadingController();
            _ = StartCoroutine(RunNextFrame(StartScene));
        }

        private void OnDestroy()
        {
            UnsubscribeFromBrainEvents();
            UnsubscribeFromLoadingController();
        }

        #region Event Subscriptions
        private void SubscribeToBrainEvents()
        {
            brain.OnStateChanged += HandleStateChanged;
            brain.OnPrecomputeCompleted += HandlePrecomputeCompleted;
        }

        private void UnsubscribeFromBrainEvents()
        {
            brain.OnStateChanged -= HandleStateChanged;
            brain.OnPrecomputeCompleted -= HandlePrecomputeCompleted;
        }

        private void HandlePrecomputeCompleted() => HandlePreBattleTransitionToBattleCompleted();

        private void SubscribeToLoadingController()
        {
            if (loadingController != null)
            {
                loadingController.OnProgressChanged += HandleLoadingProgressChanged;
            }
        }

        private void UnsubscribeFromLoadingController()
        {
            if (loadingController != null)
            {
                loadingController.OnProgressChanged -= HandleLoadingProgressChanged;
            }
        }

        private void HandleLoadingProgressChanged(float percentage)
        {
            int percentInt = Mathf.RoundToInt(percentage * 100f);
            ReportLoadingProgress(percentInt);
            // Also report normalized float progress for UIs expecting 0..1
            ReportLoadingProgress(percentage);
        }
        #endregion

        public void ReportLoadingProgress(int percentage)
        {
            OnLoadedAmountChanged?.Invoke(percentage);
            OnLoadedAmountChangedAction?.Invoke(percentage);
        }

        public void ReportLoadingProgress(float percentage)
        {
            OnLoadedAmountChangedFloat?.Invoke(percentage);
            OnLoadedAmountChangedActionFloat?.Invoke(percentage);
        }

        private void StartScene()
        {
            Index = 0;

            if (CurrentSegment != null && !string.IsNullOrEmpty(CurrentSegment.stateId))
            {
                SetBrainStateFromSegment(CurrentSegment.stateId);
            }
        }

        private void SetBrainStateFromSegment(string stateId)
        {
            if (brain?.stateBrain == null || string.IsNullOrEmpty(stateId))
            {
                return;
            }

            if (stateId.Contains("."))
            {
                var parts = stateId.Split('.');
                if (parts.Length == 2)
                {
                    brain.stateBrain.ActivateChildStateByFullPath(parts[0], parts[1]);
                    return;
                }
            }

            brain.stateBrain.ActivateHighLevelState(stateId);
        }

        private void HandleStateChanged(BrainState newState)
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

        public void HandlePreBattleTransitionToBattleCompleted() =>
            brain?.stateBrain?.HandlePreBattleTransitionToBattleCompleted();

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

        private void OnSegmentReached(int segmentIndex)
        {
            if (segmentIndex < segments.Count)
            {
                CurrentSegment?.onSegmentReached?.Invoke();

                if (
                    CurrentSegment?.stateId != null
                    && CurrentSegment.stateId.Contains(BrainStateNames.PreBattleTransitionToBattle)
                )
                {
                    StartPreLoading.Invoke();

                    loadingController?.Initialize();

                    var loader =
                        FindFirstObjectByType<Gameplay.Combat.Precompute.BattlePrecomputeLoader>();
                    if (loader != null)
                    {
                        var initRes = loader.Initialize(
                            brain,
                            brain?.battleBrain?.BattleObject?.Context
                        );
                        if (!initRes.Success)
                        {
                            TurnrootLogger.Log(
                                $"DynamicSceneFlow: BattlePrecomputeLoader.Initialize failed: {initRes.ErrorMessage}",
                                TurnrootLogger.LogLevel.Warning
                            );
                            HandlePreBattleTransitionToBattleCompleted();
                        }
                        else
                        {
                            loader.ForceStartPrecomputeIfPossible();
                        }
                    }
                    else
                    {
                        HandlePreBattleTransitionToBattleCompleted();
                    }
                }
            }
        }

        private IEnumerator RunNextFrame(Action action)
        {
            yield return null;
            action();
        }
    }
}
