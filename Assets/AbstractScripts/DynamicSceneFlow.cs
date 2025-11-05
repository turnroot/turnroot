using System;
using System.Collections;
using System.Collections.Generic;
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

    int Index
    {
        get { return _index; }
        set
        {
            StopAllCoroutines();
            _index = value;
            OnStateChange(_index);
        }
    }

    private void Start()
    {
        _ = StartCoroutine(RunNextFrame(StartScene));
    }

    private void StartScene()
    {
        Index = 0;
    }

    public void ProgressState()
    {
        Index++;
    }

    public void SetState(int state)
    {
        Index = state;
    }

    private void OnStateChange(int state)
    {
        if (state >= segments.Count)
        {
            Debug.Log("Scene Flow Completed");
            return;
        }

        var segment = CurrentSegment;
        Debug.Log($"Scene Flow State: {segment.segmentName}");

        segment.onSegmentReached?.Invoke();
    }

    IEnumerator RunNextFrame(Action action)
    {
        yield return null;
        action();
    }
}
