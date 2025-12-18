using NaughtyAttributes;
using Turnroot.Characters;
using UnityEngine;

public class TestUnitInstanceView : MonoBehaviour
{
    [Header("Data Reference")]
    public CharacterInstance CharacterInstance;

    [Header("Debug Info")]
    public string DisplayName;
    public Vector2Int CurrentGridCoordinates;

    public Vector2Int MoveToPoint;

    public AStarModified aStarModified;
    public MapGrid TestingGrid;
    public MapGridPoint CurrentGridPoint;

    void Update()
    {
        // Keep inspector in sync for debugging
        if (CharacterInstance != null && TestingGrid != null)
        {
            DisplayName = CharacterInstance.CharacterTemplate?.DisplayName;
            CurrentGridPoint = CharacterInstance.UnitPositionToMapGridPoint(
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
            var path = aStarModified.AStarSearch(
                TestingGrid,
                CurrentGridPoint,
                MovePoint,
                CharacterInstance.ToAIData().MovementType == MovementType.Infantry,
                CharacterInstance.ToAIData().MovementType == MovementType.Flying,
                CharacterInstance.ToAIData().MovementType == MovementType.Riding,
                false, // TODO: Fix magic movement
                CharacterInstance.ToAIData().MovementType == MovementType.Armored
            );
            if (path != null && path.Count > 0)
            {
                var index = 0;
                while (index < path.Count)
                {
                    Debug.Log("Stepping to: " + path[index].CoordinatesInt());
                    Step(path[index]);
                    index++;
                }
            }
            else
            {
                Debug.Log("No path found");
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
        }
    }
}
