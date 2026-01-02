using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using UnityEngine;
using UnityEngine.Events;

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

    private void OnDestroy()
    {
        UnsubscribeFromBrainEvents();
    }

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

    private void SetBrainStateFromSegment(string stateId)
    {
        if (brain?.stateBrain == null || string.IsNullOrEmpty(stateId))
        {
            return;
        }

        // Check if this is a child state (contains a dot, e.g. "Combat.PreBattle")
        if (stateId.Contains("."))
        {
            var parts = stateId.Split('.');
            if (parts.Length == 2)
            {
                string parentStateName = parts[0];
                string childStateName = parts[1];

                // Activate parent state first
                brain.stateBrain.ActivateHighLevelState(parentStateName);

                // Activate child state next frame to ensure parent is set
                StartCoroutine(ActivateChildStateNextFrame(childStateName));
                return;
            }
        }

        // Otherwise it's a top-level state
        brain.stateBrain.ActivateHighLevelState(stateId);
    }

    private IEnumerator ActivateChildStateNextFrame(string childStateName)
    {
        yield return null;
        brain.stateBrain.ActivateChildState(childStateName);
    }

    private void HandleStateChanged(BrainState newState)
    {
        if (newState == null)
        {
            return;
        }

        // When Brain state changes, find and activate the matching segment
        ActivateSegmentByState(newState);
    }

    public void ActivateSegmentByState(BrainState state)
    {
        if (state == null)
        {
            return;
        }

        // Build the full state path: if the state has a parent, use "Parent.Child" format
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
            Debug.LogWarning($"DynamicSceneFlow: No segment found for state '{fullStatePath}'.");
#endif
        }
    }

    private string GetFullStatePath(BrainState state)
    {
        return state?.GetFullPath() ?? "";
    }

    private void OnSegmentReached(int state)
    {
        if (state >= segments.Count)
        {
            return;
        }

        var segment = CurrentSegment;
        segment.onSegmentReached?.Invoke();
    }

    private IEnumerator RunNextFrame(Action action)
    {
        yield return null;
        action();
    }
}
