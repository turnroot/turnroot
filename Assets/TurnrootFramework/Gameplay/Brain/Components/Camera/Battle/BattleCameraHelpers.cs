using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class CameraBrain : BrainComponent
    {
        private OperationResult ValidateRequiredComponents()
        {
            if (MapGrid == null)
            {
                return OperationResult.Failure("CameraBrain: MapGrid is null");
            }

            if (_battleMapCamera == null)
            {
                InitializeBattleMapCamera(BattleObject);
                if (_battleMapCamera == null)
                {
                    return OperationResult.Failure("CameraBrain: BattleMapCamera is null");
                }
            }

            return OperationResult.SuccessResult();
        }

        private Vector3 GetCameraTargetPoint()
        {
            Vector3 cameraCenter = _battleMapCamera.transform.position;
            Ray ray = new Ray(cameraCenter, _battleMapCamera.transform.forward);

            return Physics.Raycast(ray, out RaycastHit hit, 200f, BattleObject.GroundLayerMask)
                ? hit.point
                : cameraCenter;
        }

        private Vector3 GetWorldPosition(Vector2Int gridCoordinates)
        {
            var terrainAdjustedPos = MapGrid.GetTerrainAdjustedWorldPosition(gridCoordinates);

            // Apply the same offset as the cursor to match visual positioning
            // Cursor uses: worldPosition + new Vector3(0, 1f, -2f)
            var cursorVisualPos = terrainAdjustedPos + new Vector3(0, 1f, -2f);

            // Preserve camera's current Y position, use cursor's visual X and Z
            var cameraY = _battleMapCamera?.transform.position.y ?? terrainAdjustedPos.y;
            var worldPos = new Vector3(cursorVisualPos.x, cameraY, cursorVisualPos.z);

#if UNITY_EDITOR
            Debug.Log(
                $"[CAMERA] Grid {gridCoordinates} → Terrain {terrainAdjustedPos} → CursorVisual {cursorVisualPos} → CameraTarget {worldPos}"
            );
#endif
            return worldPos;
        }

        private Vector3 CalculateNewCameraPosition(Vector2Int cursorGridPos)
        {
            if (UiSettings.CameraPanTriesToCenterItself)
            {
                // Don't update center position here - do it after pan completes
                return GetWorldPosition(cursorGridPos);
            }
            else
            {
                Vector2Int panAmount = UiSettings.DistanceFromCenterCameraPan / 2;
                Vector2Int direction = cursorGridPos - _currentCameraCenterGridPosition;

                // Normalize direction to -1, 0, or 1
                direction.x = direction.x == 0 ? 0 : (direction.x > 0 ? 1 : -1);
                direction.y = direction.y == 0 ? 0 : (direction.y > 0 ? 1 : -1);

                Vector2Int newCameraCenterGridPos =
                    _currentCameraCenterGridPosition
                    + new Vector2Int(direction.x * panAmount.x, direction.y * panAmount.y);

                return GetWorldPosition(newCameraCenterGridPos);
            }
        }
    }
}
