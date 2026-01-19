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

        public Brain brain;

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
        }

        private void OnDestroy() => UnsubscribeFromBrainEvents();

        private void SubscribeToBrainEvents()
        {
            if (brain != null)
            {
                brain.OnStateChanged += HandleStateChanged;
            }
        }

        private void UnsubscribeFromBrainEvents()
        {
            if (brain != null)
            {
                brain.OnStateChanged -= HandleStateChanged;
            }
        }

        private void StartScene()
        {
            Index = 0;

            // Set the Brain state to match the first segment
            if (CurrentSegment != null && !string.IsNullOrEmpty(CurrentSegment.stateId))
            {
                SetBrainStateFromSegment(CurrentSegment.stateId);
            }
        }

        /// <summary>
        /// Activates the brain state corresponding to the given state ID.
        /// Handles both hierarchical states ("Parent.Child") and top-level states.
        /// </summary>
        private void SetBrainStateFromSegment(string stateId)
        {
            if (brain?.stateBrain == null || string.IsNullOrEmpty(stateId))
            {
                return;
            }

            // Check if this is a hierarchical state (contains a dot, e.g. "Combat.PreBattle")
            if (stateId.Contains("."))
            {
                var parts = stateId.Split('.');
                if (parts.Length == 2)
                {
                    string parentStateName = parts[0];
                    string childStateName = parts[1];

                    // Directly activate the child state, which will automatically set the parent
                    brain.stateBrain.ActivateChildStateByFullPath(parentStateName, childStateName);
#if UNITY_EDITOR
                    Debug.Log($"DynamicSceneFlow: Activated hierarchical state '{stateId}'");
#endif
                    return;
                }
            }

            // Otherwise it's a top-level state
            brain.stateBrain.ActivateHighLevelState(stateId);
#if UNITY_EDITOR
            Debug.Log($"DynamicSceneFlow: Activated top-level state '{stateId}'");
#endif
        }

        /// <summary>
        /// Handles brain state changes by finding and activating the corresponding flow segment.
        /// </summary>
        private void HandleStateChanged(BrainState newState)
        {
            if (newState == null)
            {
                return;
            }

            // Find and activate the segment that matches the new brain state
            ActivateSegmentByState(newState);
        }

        /// <summary>
        /// Finds and activates the flow segment that corresponds to the given brain state.
        /// </summary>
        public void ActivateSegmentByState(BrainState state)
        {
            if (state == null)
            {
                return;
            }

            // Build the full state path (e.g., "Combat.PreBattle" for hierarchical states)
            string fullStatePath = GetFullStatePath(state);

            // Find segment with matching state ID
            int foundIndex = segments.FindIndex(s => s.stateId == fullStatePath);
            if (foundIndex != -1)
            {
                Index = foundIndex;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"DynamicSceneFlow: No segment found for state '{fullStatePath}'."
                );
#endif
            }
        }

        /// <summary>
        /// Gets the full path of a brain state (e.g., "Combat.PreBattle" for hierarchical states).
        /// </summary>
        private string GetFullStatePath(BrainState state) => state?.GetFullPath() ?? "";

        /// <summary>
        /// Called when a new segment is reached. Invokes the segment's event callbacks.
        /// </summary>
        private void OnSegmentReached(int segmentIndex)
        {
            if (segmentIndex >= segments.Count)
            {
                return;
            }

            var segment = CurrentSegment;
            segment?.onSegmentReached?.Invoke();
        }

        /// <summary>
        /// Utility method to execute an action on the next frame.
        /// </summary>
        private IEnumerator RunNextFrame(Action action)
        {
            yield return null;
            action();
        }
    }
}
