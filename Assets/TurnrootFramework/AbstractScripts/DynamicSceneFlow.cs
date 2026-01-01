using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class FlowSegment
{
    public string segmentName = "segment";
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
            OnStateChange(_index);
        }
    }

    private void Start() => _ = StartCoroutine(RunNextFrame(StartScene));

    public void SetBrainHighLevelState(string stateName)
    {
        var stateBrain = brain?.stateBrain;

        stateBrain.ActivateHighLevelState(NormalizeStateName(stateName));
    }

    public void SetBrainChildState(string stateName)
    {
        var stateBrain = brain?.stateBrain;

        var normalized = NormalizeStateName(stateName);

        if (
            stateBrain.CurrentState == null
            || stateBrain.CurrentState.Name != BrainStateNames.Combat
        )
        {
            stateBrain.ActivateHighLevelState(BrainStateNames.Combat);
        }

        stateBrain.ActivateChildState(normalized);
    }

    private string NormalizeStateName(string s) =>
        string.IsNullOrEmpty(s) ? s : s.Replace("-", "").Replace(" ", "");

    private void StartScene() => Index = 0;

    public void ProgressState() => Index++;

    public void DegressState()
    {
        if (Index > 0)
        {
            Index--;
        }
    }

    public void SetState(int state) => Index = state;

    public void SetState(string segmentName)
    {
        int foundIndex = segments.FindIndex(s => s.segmentName == segmentName);
        if (foundIndex != -1)
        {
            Index = foundIndex;
        }
        else
        {
            Debug.LogWarning($"DynamicSceneFlow: Segment '{segmentName}' not found.");
        }
    }

    private void OnStateChange(int state)
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
