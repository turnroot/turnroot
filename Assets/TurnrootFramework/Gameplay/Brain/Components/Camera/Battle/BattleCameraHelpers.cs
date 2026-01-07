using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class CameraBrain : BrainComponent
    {
        private void HandleBattleMapCameraPan()
        {
            if (_battleMapCamera == null || MapGrid == null)
            {
                return;
            }

            Vector3 currentPos = _battleMapCamera.transform.position;

            if (
                Vector3.Distance(currentPos, _targetCameraPosition)
                > UiSettings.CameraPanStopDistance
            )
            {
                float smoothTime = UiSettings?.CameraPanSpeed ?? 0.3f;
                smoothTime = 1.005f - smoothTime;
                switch (gameSpeed)
                {
                    case GameplayPlayerSettings.GameSpeed.Fast:
                        smoothTime *= 0.85f;
                        break;
                    case GameplayPlayerSettings.GameSpeed.VeryFast:
                        smoothTime *= 0.7f;
                        break;
                }

                Vector3 newPos = Vector3.SmoothDamp(
                    currentPos,
                    _targetCameraPosition,
                    ref _currentVelocity,
                    smoothTime
                );

                _battleMapCamera.transform.position = newPos;
            }
        }

        public void SetBattleMapCamera(Camera cam)
        {
            _battleMapCamera = cam;
            if (cam != null)
            {
                _targetCameraPosition = cam.transform.position;
            }
        }

        public void InitializeBattleMapCamera(BattleGameObject battleObject)
        {
            var battleObjectCameras = battleObject.GetComponentsInChildren<Camera>();
            foreach (var cam in battleObjectCameras)
            {
                if (cam.CompareTag("BattleMapCamera"))
                {
                    SetBattleMapCamera(cam);
                    break;
                }
            }
        }

        public Vector2Int SetBattleGridCameraNeutralCenter()
        {
            Debug.Log($"[CAMERA] SetCameraNeutralCenter() called");

            if (MapGrid == null)
            {
                Debug.LogError("[CAMERA] MapGrid is null");
                return Vector2Int.zero;
            }

            if (_battleMapCamera == null)
            {
                InitializeBattleMapCamera(BattleObject);
                if (_battleMapCamera == null)
                {
                    Debug.LogError("[CAMERA] BattleMapCamera is null");
                    return Vector2Int.zero;
                }
            }

            // Initialize target position to current camera position
            _targetCameraPosition = _battleMapCamera.transform.position;

            Vector3 cameraCenter = _battleMapCamera.transform.position;
            Ray ray = new Ray(cameraCenter, _battleMapCamera.transform.forward);

            Vector3 targetPoint = Physics.Raycast(
                ray,
                out RaycastHit hit,
                200f,
                BattleObject.GroundLayerMask
            )
                ? hit.point
                : cameraCenter;

            Debug.Log(
                $"[CAMERA] Current camera is at {_battleMapCamera.transform.position}, target point: {targetPoint}"
            );

            var allGridPoints = MapGrid.GetAllGridPoints();
            if (allGridPoints == null || allGridPoints.Count == 0)
            {
                Debug.LogWarning("[CAMERA] No grid points found");
                return Vector2Int.zero;
            }

            MapGridPoint closestPoint = null;
            float closestDistance = float.MaxValue;

            foreach (var gridPoint in allGridPoints)
            {
                Vector3 gridWorldPos = MapGrid.GetTerrainAdjustedWorldPosition(
                    gridPoint.CoordinatesInt
                );
                float distance = Vector3.Distance(targetPoint, gridWorldPos);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = gridPoint;
                }
            }

            if (closestPoint == null)
            {
                Debug.LogWarning("[CAMERA] SetCameraNeutralCenter: No closest point found");
                return Vector2Int.zero;
            }

            Debug.Log(
                $"[CAMERA] SetCameraNeutralCenter: Found closest grid point {closestPoint.CoordinatesInt} at distance {closestDistance}"
            );
            return closestPoint.CoordinatesInt;
        }

        private void HandleCursorMoved(Vector2Int gridPos)
        {
            if (MapGrid == null || UiSettings == null || _battleMapCamera == null)
            {
                return;
            }

            if (_inCombat)
            {
                // Get the world position of the cursor
                Vector3 cursorWorldPos = MapGrid.GetTerrainAdjustedWorldPosition(gridPos);

                // Calculate where the cursor appears on screen in viewport space (0-1)
                Vector3 cursorViewportPos = _battleMapCamera.WorldToViewportPoint(cursorWorldPos);

                float marginX = UiSettings.CameraPanSafeZone.x;
                float marginY = UiSettings.CameraPanSafeZone.y;

                Vector2 displacement = Vector2.zero;

                // Check if cursor is outside safe zone and calculate how much to move camera
                if (cursorViewportPos.x < marginX)
                {
                    displacement.x = cursorViewportPos.x - marginX;
                }
                else if (cursorViewportPos.x > (1f - marginX))
                {
                    displacement.x = cursorViewportPos.x - (1f - marginX);
                }

                if (cursorViewportPos.y < marginY)
                {
                    displacement.y = cursorViewportPos.y - marginY;
                }
                else if (cursorViewportPos.y > (1f - marginY))
                {
                    displacement.y = cursorViewportPos.y - (1f - marginY);
                }

                if (displacement != Vector2.zero)
                {
                    // Convert viewport displacement to world space
                    // We need to know how much world space = viewport space at cursor's distance
                    float distanceToTerrain = Vector3.Distance(
                        _battleMapCamera.transform.position,
                        cursorWorldPos
                    );

                    // Get camera right and up vectors in world space
                    Vector3 cameraRight = _battleMapCamera.transform.right;
                    Vector3 cameraUp = _battleMapCamera.transform.up;

                    // Project camera vectors onto horizontal plane
                    cameraRight.y = 0;
                    cameraRight.Normalize();
                    cameraUp.y = 0;
                    cameraUp.Normalize();

                    // Scale displacement by distance and field of view
                    float fovFactor =
                        Mathf.Tan(_battleMapCamera.fieldOfView * 0.5f * Mathf.Deg2Rad)
                        * distanceToTerrain;
                    float horizontalScale = fovFactor * _battleMapCamera.aspect;
                    float verticalScale = fovFactor;

                    Vector3 worldDisplacement =
                        cameraRight * (displacement.x * horizontalScale)
                        + cameraUp * (displacement.y * verticalScale);

                    _targetCameraPosition += worldDisplacement;

                    Debug.Log(
                        $"[CAMERA] Cursor at viewport {cursorViewportPos}, moving camera by {worldDisplacement}"
                    );
                }
            }
        }
    }
}
