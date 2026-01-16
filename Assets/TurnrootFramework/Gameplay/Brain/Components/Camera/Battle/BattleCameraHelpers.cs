using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.PlayerSettings;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class CameraBrain : BrainComponent
    {
        public void MoveCameraToPosition(Vector2Int gridPosition)
        {
            Vector3 targetWorldPos = mapGrid.GetTerrainAdjustedWorldPosition(gridPosition);

            // Compute desired camera position so that the target world position appears at the camera's center.
            // We try to preserve the camera's Y (height) and compute the correct scalar along the camera forward
            // vector so that the line from camera to target is colinear with the camera forward direction.
            var cam = _battleMapCamera.transform;
            Vector3 f = cam.forward;
            Vector3 camPos = cam.position;
            const float eps = 1e-4f;
            float k;

            // Prefer solving using the Y component so we can keep camera height unchanged.
            if (Mathf.Abs(f.y) > eps)
            {
                // k is chosen so that (targetWorldPos - (targetWorldPos - k*f)).y == camPos.y
                // i.e., targetWorldPos.y - k*f.y == camPos.y => k = (targetWorldPos.y - camPos.y) / f.y
                k = (targetWorldPos.y - camPos.y) / f.y;
            }
            else if (Mathf.Abs(f.x) > eps)
            {
                // Fallback to X component if forward is nearly horizontal in Y.
                k = (targetWorldPos.x - camPos.x) / f.x;
            }
            else if (Mathf.Abs(f.z) > eps)
            {
                // Last fallback to Z component.
                k = (targetWorldPos.z - camPos.z) / f.z;
            }
            else
            {
                // Degenerate forward vector; fall back to previous origin-based approach.
                var originWorldPos = mapGrid.GetTerrainAdjustedWorldPosition(Vector2Int.zero);
                Vector3 cameraToOrigin = camPos - originWorldPos;
                k = Vector3.Dot(cameraToOrigin, f);
                if (Mathf.Abs(k) < 0.001f)
                {
                    k = cameraToOrigin.magnitude;
                }
            }

            Vector3 newPos = targetWorldPos - f * k;

            // Preserve camera height when possible (prevents camera dropping through terrain).
            newPos.y = camPos.y;

            _targetCameraPosition = newPos;
            _shouldMove = true;
        }

        private void HandleBattleMapCameraPan()
        {
            if (_battleMapCamera == null || mapGrid == null)
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
            if (battleObject == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "[CAMERA] InitializeBattleMapCamera called with null BattleObject"
                );
#endif
                return;
            }

            var battleObjectCameras = battleObject.GetComponentsInChildren<Camera>();
            foreach (var cam in battleObjectCameras)
            {
                if (cam == null)
                {
                    continue;
                }

                if (cam.CompareTag("BattleMapCamera"))
                {
                    SetBattleMapCamera(cam);
                    break;
                }
            }
        }

        public Vector2Int SetBattleGridCameraNeutralCenter()
        {
            // Prefer the battle context map grid, but fall back to the pre-battle preparation map if available.
            var mapGridToUse = mapGrid ?? Brain?.battleBrain?.PreparationObject?.MapGrid;
            if (mapGridToUse == null)
            {
                return Vector2Int.zero;
            }

            if (_battleMapCamera == null)
            {
                if (BattleObject != null)
                {
                    InitializeBattleMapCamera(BattleObject);
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        "[CAMERA] BattleObject is null; cannot initialize BattleMapCamera"
                    );
#endif
                }

                if (_battleMapCamera == null)
                {
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

            var allGridPoints = mapGridToUse.GetAllGridPoints();
            if (allGridPoints == null || allGridPoints.Count == 0)
            {
                return Vector2Int.zero;
            }

            MapGridPoint closestPoint = null;
            float closestDistance = float.MaxValue;

            foreach (var gridPoint in allGridPoints)
            {
                Vector3 gridWorldPos = mapGridToUse.GetTerrainAdjustedWorldPosition(
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
                return Vector2Int.zero;
            }
#if UNITY_EDITOR
            Debug.Log(
                $"[CAMERA] SetCameraNeutralCenter: Found closest grid point {closestPoint.CoordinatesInt} at distance {closestDistance}"
            );
#endif
            return closestPoint.CoordinatesInt;
        }

        private void HandleCursorMoved(Vector2Int gridPos)
        {
            if (mapGrid == null || UiSettings == null || _battleMapCamera == null)
            {
                return;
            }

            if (_shouldMove)
            {
                // Get the world position of the cursor
                Vector3 cursorWorldPos = mapGrid.GetTerrainAdjustedWorldPosition(gridPos);

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

#if UNITY_EDITOR
                    Debug.Log(
                        $"[CAMERA] Cursor at viewport {cursorViewportPos}, moving camera by {worldDisplacement}"
                    );
#endif
                }
            }
        }
    }
}
