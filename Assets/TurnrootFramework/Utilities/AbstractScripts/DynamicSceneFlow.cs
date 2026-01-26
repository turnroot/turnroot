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

        public void ReportLoadingProgress(int percentage)
        {
            OnLoadedAmountChanged?.Invoke(percentage);
            OnLoadedAmountChangedAction?.Invoke(percentage);
        }

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
            _ = StartCoroutine(RunNextFrame(StartScene));
            SubscribeToBrainEvents();
            loadingController = brain?.GetComponent<LoadingController>();
        }

        private void OnDestroy() => UnsubscribeFromBrainEvents();

        private void SubscribeToBrainEvents() => brain.OnStateChanged += HandleStateChanged;

        private void UnsubscribeFromBrainEvents() => brain.OnStateChanged -= HandleStateChanged;

        private void StartScene()
        {
            Index = 0;

            // Set the Brain state to match the first segment
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
                    string parentStateName = parts[0];
                    string childStateName = parts[1];

                    // Directly activate the child state, which will automatically set the parent
                    brain.stateBrain.ActivateChildStateByFullPath(parentStateName, childStateName);

                    TurnrootLogger.Log(
                        $"DynamicSceneFlow: Activated hierarchical state '{stateId}'"
                    );

                    return;
                }
            }

            // Otherwise it's a top-level state
            brain.stateBrain.ActivateHighLevelState(stateId);

            TurnrootLogger.Log($"DynamicSceneFlow: Activated top-level state '{stateId}'");
        }

        private void HandleStateChanged(BrainState newState)
        {
            if (newState == null)
            {
                return;
            }

            // Find and activate the segment that matches the new brain state
            ActivateSegmentByState(newState);
        }

        public void ActivateSegmentByState(BrainState state)
        {
            if (state == null)
            {
                return;
            }

            // Build the full state path (e.g., "Combat.PreBattle" for hierarchical states)
            string fullStatePath = GetFullStatePath(state);

            int foundIndex = segments.FindIndex(s => s.stateId == fullStatePath);
            if (foundIndex != -1)
            {
                Index = foundIndex;
            }
            else
            {
                TurnrootLogger.Log(
                    $"DynamicSceneFlow: No segment found for state '{fullStatePath}'.",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        public void HandlePreBattleTransitionToBattleCompleted() =>
            brain.stateBrain.HandlePreBattleTransitionToBattleCompleted();

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

        private string GetFullStatePath(BrainState state) => state?.GetFullPath() ?? "";

        private void OnSegmentReached(int segmentIndex)
        {
            if (segmentIndex >= segments.Count)
            {
                return;
            }

            var segment = CurrentSegment;
            segment?.onSegmentReached?.Invoke();
        }

        private IEnumerator RunNextFrame(Action action)
        {
            yield return null;
            action();
        }
    }
}
