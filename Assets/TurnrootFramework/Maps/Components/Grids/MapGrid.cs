using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class MapGrid : MonoBehaviour
{
    [SerializeField]
    private float _gridScale = 1f;

    [SerializeField]
    private Vector3 _gridOffset = Vector3.zero;

    [SerializeField]
    private int _gridWidth = 10;

    [SerializeField]
    private int _gridHeight = 10;

    [SerializeField]
    private Dictionary<Vector2Int, GameObject> _gridPoints = new();

    [Button("Create Grid Points")]
    public void CreateChildrenPoints()
    {
        if (_gridPoints.Count > 0)
        {
            ClearGrid();
        }
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                var point = new GameObject($"Point_{x}_{y}");
                point.AddComponent<MapGridPoint>();
                point.transform.parent = transform;
                point.transform.localPosition = new Vector3(x * _gridScale, 0, y * _gridScale) + _gridOffset;
                _gridPoints[new Vector2Int(x, y)] = point;
            }
        }
    }

    public void ClearGrid()
    {
        foreach (var point in _gridPoints.Values)
        {
            if (point != null)
            {
                Destroy(point);
            }
        }
        _gridPoints.Clear();
    }
}
