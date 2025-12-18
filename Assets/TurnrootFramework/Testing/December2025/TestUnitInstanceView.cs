using NaughtyAttributes;
using Turnroot.Characters;
using UnityEngine;

public class TestUnitInstanceView : MonoBehaviour
{
    private static readonly WaitForSeconds _wait = new(0.6f);
    private Coroutine moveCoroutine;

    [Header("Data Reference")]
    public CharacterData CharacterData;

    private CharacterInstance _characterInstance;

    [Header("Debug Info")]
    public string DisplayName;
    public Vector2Int CurrentGridCoordinates;

    public Vector2Int MoveToPoint;

    public AStarModified aStarModified;
    public MapGrid TestingGrid;
    public MapGridPoint CurrentGridPoint;

    private void Awake()
    {
        aStarModified = new AStarModified();
        _characterInstance = new CharacterInstance(CharacterData);
    }

    private void Update()
    {
        // Keep inspector in sync for debugging
        if (_characterInstance != null && TestingGrid != null)
        {
            DisplayName = _characterInstance.CharacterTemplate?.DisplayName;
            CurrentGridPoint = _characterInstance.UnitPositionToMapGridPoint(
                CurrentGridCoordinates,
                TestingGrid
            );
            if (CurrentGridPoint != null)
            {
                var worldLocation = TestingGrid.GetMapGridPointWorldLocation(CurrentGridPoint);
                transform.position = worldLocation;
            }
        }
    }

    [Button]
    public void MoveUnitToPoint()
    {
        Debug.Log("MoveUnitToPoint called");
        MapGridPoint MovePoint = TestingGrid.GetGridPoint(MoveToPoint.x, MoveToPoint.y);
        if (aStarModified != null && TestingGrid != null)
        {
            Debug.Log(
                $"TestingGrid: {TestingGrid}, "
                    + $"CurrentGridPoint: {CurrentGridPoint}, "
                    + $"Movement: {_characterInstance.ToAIData().GetStat(Turnroot.Characters.Stats.UnboundedStatType.Movement)}, "
                    + $"Infantry: {_characterInstance.ToAIData().MovementType == MovementType.Infantry}, "
                    + $"Flying: {_characterInstance.ToAIData().MovementType == MovementType.Flying}, "
                    + $"Riding: {_characterInstance.ToAIData().MovementType == MovementType.Riding}, "
                    + $"Magic: {false}, "
                    + // TODO: Fix magic movement
                    $"Armored: {_characterInstance.ToAIData().MovementType == MovementType.Armored}"
            );
            var reachable = aStarModified.GetReachable(
                TestingGrid,
                CurrentGridPoint,
                _characterInstance.ToAIData().Movement,
                _characterInstance.ToAIData().MovementType == MovementType.Infantry,
                _characterInstance.ToAIData().MovementType == MovementType.Flying,
                _characterInstance.ToAIData().MovementType == MovementType.Riding,
                false, // TODO: Fix magic movement
                _characterInstance.ToAIData().MovementType == MovementType.Armored
            );
            Debug.Log($"Reachable points count: {reachable.Count}");

            // Check if MovePoint is in reachable points
            if (reachable.ContainsKey(MovePoint))
            {
                var path = aStarModified.GetPathThroughReachable(
                    CurrentGridPoint,
                    MovePoint,
                    reachable
                );
                Debug.Log($"Path found with {path.Count} points.");
                if (moveCoroutine != null)
                {
                    StopCoroutine(moveCoroutine);
                }
                moveCoroutine = StartCoroutine(MoveAlongPathCoroutine(path));
            }
            else
            {
                Debug.Log("MovePoint is not reachable");
            }
        }
        else
        {
            Debug.Log("aStarModified is null");
            aStarModified = new AStarModified();
        }
    }

    public void Step(MapGridPoint point)
    {
        if (point != null)
        {
            CurrentGridPoint = point;
            CurrentGridCoordinates = point.CoordinatesInt();
            Debug.Log($"Stepped to point: {point.CoordinatesInt()}");
        }
    }

    private System.Collections.IEnumerator MoveAlongPathCoroutine(
        System.Collections.Generic.List<MapGridPoint> path
    )
    {
        if (path == null || path.Count == 0)
        {
            yield break;
        }
        // Skip the first point if it's the current position
        int startIdx = 0;
        if (CurrentGridPoint == path[0])
        {
            startIdx = 1;
        }

        for (int i = startIdx; i < path.Count; i++)
        {
            Step(path[i]);
            yield return _wait;
        }
    }
}
