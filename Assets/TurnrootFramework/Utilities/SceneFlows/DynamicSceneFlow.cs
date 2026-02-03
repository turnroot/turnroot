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
        protected int _index = 0;
        public FlowSegment CurrentSegment => segments.Count > _index ? segments[_index] : null;

        [HideInInspector]
        public Brain brain;

        [HideInInspector]
        public LoadingController loadingController;

        // Progress (0..1) for UI elements that expect normalized values
        public UnityEvent<float> OnLoadedAmountChanged = new();
        public event Action<float> OnLoadedAmountChangedAction;

        public UnityEvent StartPreLoading = new();

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
            loadingController = brain?.GetComponent<LoadingController>();
            SubscribeToBrainEvents();
            SubscribeToLoadingController();
            _ = StartCoroutine(RunNextFrame(StartScene));
        }

        protected void OnDestroy()
        {
            UnsubscribeFromBrainEvents();
            UnsubscribeFromLoadingController();
        }

        #region Event Subscriptions
        protected virtual void SubscribeToBrainEvents() => brain.OnStateChanged += HandleStateChanged;

        protected virtual void UnsubscribeFromBrainEvents() => brain.OnStateChanged -= HandleStateChanged;

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
        #endregion

        public void ReportLoadingProgress(float percentage)
        {
            OnLoadedAmountChanged?.Invoke(percentage);
            OnLoadedAmountChangedAction?.Invoke(percentage);
        }

        protected void StartScene()
        {
            Index = 0;

            if (CurrentSegment != null && !string.IsNullOrEmpty(CurrentSegment.stateId))
            {
                SetBrainStateFromSegment(CurrentSegment.stateId);
            }
        }

        protected void SetBrainStateFromSegment(string stateId)
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
