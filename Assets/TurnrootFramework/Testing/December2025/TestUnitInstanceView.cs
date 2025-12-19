using Turnroot.Characters;
using UnityEngine;

public class TestUnitInstanceView : MonoBehaviour
{
    private static readonly WaitForSeconds _wait = new(0.6f);
    private Coroutine moveCoroutine;

    [Header("Data Reference")]
    public CharacterData CharacterData;

    public CharacterInstance CharacterDataInstance { get; private set; }

    private ICharacterAIData Data => CharacterDataInstance.ToAIData();

    [Header("Debug Info")]
    public string DisplayName;
    public Vector2Int CurrentGridCoordinates;

    public AStarModified aStarModified;
    public MapGrid TestingGrid;
    public MapGridPoint CurrentGridPoint;

    private void Awake()
    {
        aStarModified = new AStarModified();
        CharacterDataInstance = CharacterInstance.Create(CharacterData);
        Debug.Log(
            $"TestUnitInstanceView Awake: Created {CharacterDataInstance.Id} for {CharacterData.DisplayName}"
        );
    }

    private void Update()
    {
        // Keep inspector in sync for debugging
        if (CharacterDataInstance != null && TestingGrid != null)
        {
            DisplayName = CharacterDataInstance.CharacterTemplate?.DisplayName;
            CurrentGridPoint = CharacterDataInstance.UnitPositionToMapGridPoint(
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

    public void MoveUnitToPoint(Vector2Int MoveToPoint)
    {
        Debug.Log("MoveUnitToPoint called");
        MapGridPoint MovePoint = TestingGrid.GetGridPoint(MoveToPoint.x, MoveToPoint.y);
        if (aStarModified != null && TestingGrid != null)
        {
            Debug.Log(
                $"TestingGrid: {TestingGrid}, "
                    + $"CurrentGridPoint: {CurrentGridPoint}, "
                    + $"Movement: {Data.GetStat(Turnroot.Characters.Stats.UnboundedStatType.Movement)}, "
                    + $"Infantry: {Data.MovementType == MovementType.Infantry}, "
                    + $"Flying: {Data.MovementType == MovementType.Flying}, "
                    + $"Riding: {Data.MovementType == MovementType.Riding}, "
                    + $"Magic: {false}, "
                    + $"Armored: {Data.MovementType == MovementType.Armored}"
            );
            var reachable = aStarModified.GetReachable(
                TestingGrid,
                CurrentGridPoint,
                Data.Movement,
                Data.MovementType == MovementType.Infantry,
                Data.MovementType == MovementType.Flying,
                Data.MovementType == MovementType.Riding,
                false, // TODO: Fix magic movement
                Data.MovementType == MovementType.Armored
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
