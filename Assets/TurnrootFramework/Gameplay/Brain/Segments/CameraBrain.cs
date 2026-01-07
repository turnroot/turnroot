using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class CameraBrain : BrainComponent
    {
        #region Fields and Properties
        private Camera _battleMapCamera;
        private Vector2Int _currentCameraCenterGridPosition = Vector2Int.zero; // the center-reference point for panning
        private Coroutine _currentPanCoroutine;

        // Helper properties to reduce repetition
        private MapGrid MapGrid => Brain?.battleBrain?.BattleObject?.Context?.mapGrid;
        private GamewideUiSettings UiSettings => Brain?.uiBrain?.uiSettings;
        private BattleGameObject BattleObject => Brain?.battleBrain?.BattleObject;

        #endregion
        #region Brain Events

        protected override void SubscribeToBrainEvents() =>
            Brain.OnBattleCursorMoved += HandleCursorMoved;

        protected override void UnsubscribeFromBrainEvents() =>
            Brain.OnBattleCursorMoved -= HandleCursorMoved;

        private void HandleCursorMoved(Vector2Int gridPos)
        {
#if UNITY_EDITOR
            Debug.Log($"[CAMERA] Cursor moved to grid position: {gridPos}");
#endif
            if (MapGrid == null || !ShouldPanCamera(gridPos))
            {
                return;
            }

            Vector3 worldStartPos = GetWorldPosition(_currentCameraCenterGridPosition);
            Vector3 worldEndPos = CalculateNewCameraPosition(gridPos);

            // Calculate the target grid position for center tracking
            Vector2Int targetCenterPos;
            if (UiSettings.CameraPanTriesToCenterItself)
            {
                targetCenterPos = gridPos; // Center on cursor
#if UNITY_EDITOR
                Debug.Log($"[CAMERA] Using center mode (CameraPanTriesToCenterItself=true)");
#endif
            }
            else
            {
                // Calculate the new center position for partial panning
                Vector2Int panAmount = UiSettings.DistanceFromCenterCameraPan / 2;
                Vector2Int direction = gridPos - _currentCameraCenterGridPosition;
                direction.x = direction.x == 0 ? 0 : (direction.x > 0 ? 1 : -1);
                direction.y = direction.y == 0 ? 0 : (direction.y > 0 ? 1 : -1);
                targetCenterPos =
                    _currentCameraCenterGridPosition
                    + new Vector2Int(direction.x * panAmount.x, direction.y * panAmount.y);
#if UNITY_EDITOR
                Debug.Log(
                    $"[CAMERA] Using partial mode (CameraPanTriesToCenterItself=false), panAmount: {panAmount}, direction: {direction}"
                );
#endif
            }

#if UNITY_EDITOR
            Debug.Log(
                $"[CAMERA] Pan from {worldStartPos} to {worldEndPos}, target center: {targetCenterPos}"
            );
#endif
            StartCameraPan(worldStartPos, worldEndPos, targetCenterPos);
        }

        #endregion
        #region Initialization Methods

        public void SetBattleMapCamera(Camera cam) => _battleMapCamera = cam;

        public void InitializeBattleMapCamera(BattleGameObject battleObject)
        {
            var battleObjectCameras = battleObject.GetComponentsInChildren<Camera>();
            // Get tag "BattleMapCamera"
            foreach (var cam in battleObjectCameras)
            {
                if (cam.CompareTag("BattleMapCamera"))
                {
                    SetBattleMapCamera(cam);
                    break;
                }
            }
        }

        public Vector2Int SetCameraNeutralCenter()
        {
            Debug.Log($"[CAMERA] SetCameraNeutralCenter() called");

            if (!ValidateRequiredComponents().Success)
            {
                return Vector2Int.zero;
            }

            Vector3 targetPoint = GetCameraTargetPoint();
            Debug.Log(
                $"[CAMERA] Current camera is at {_battleMapCamera.transform.position}, target point: {targetPoint}"
            );

            // Find the closest grid point to the target position
            var allGridPoints = MapGrid.GetAllGridPoints();
            if (allGridPoints == null || allGridPoints.Count == 0)
            {
                return Vector2Int.zero;
            }

            MapGridPoint closestPoint = null;
            float closestDistance = float.MaxValue;

            foreach (var gridPoint in allGridPoints)
            {
                Vector3 gridWorldPos = GetWorldPosition(gridPoint.CoordinatesInt);
                float distance = Vector3.Distance(targetPoint, gridWorldPos);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = gridPoint;
                }
            }

            if (closestPoint == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[CAMERA] SetCameraNeutralCenter: No closest point found");
#endif
                return Vector2Int.zero;
            }
#if UNITY_EDITOR
            Debug.Log(
                $"[CAMERA] SetCameraNeutralCenter: Found closest grid point {closestPoint.CoordinatesInt} at distance {closestDistance}"
            );
#endif
            _currentCameraCenterGridPosition = closestPoint.CoordinatesInt;
            return closestPoint.CoordinatesInt;
        }
        #endregion

        private void StartCameraPan(
            Vector3 worldStartPosition,
            Vector3 worldEndPosition,
            Vector2Int targetGridPos
        )
        {
            if (!ValidateRequiredComponents().Success)
            {
                return;
            }

            // Stop any existing pan operation
            if (_currentPanCoroutine != null)
            {
#if UNITY_EDITOR
                Debug.Log("[CAMERA] Stopping existing pan coroutine");
#endif
                StopCoroutine(_currentPanCoroutine);
            }

#if UNITY_EDITOR
            Debug.Log(
                $"[CAMERA] Starting new pan coroutine from {worldStartPosition} to {worldEndPosition}"
            );
#endif
            _currentPanCoroutine = StartCoroutine(
                PanCameraCoroutine(worldStartPosition, worldEndPosition, targetGridPos)
            );
        }

        private System.Collections.IEnumerator PanCameraCoroutine(
            Vector3 worldStartPosition,
            Vector3 worldEndPosition,
            Vector2Int targetGridPos
        )
        {
#if UNITY_EDITOR
            Debug.Log(
                $"[CAMERA] PanCameraCoroutine started: from {worldStartPosition} to {worldEndPosition}"
            );
#endif

            if (UiSettings == null)
            {
                Debug.LogWarning("[CAMERA] PanCameraCoroutine: UI settings not found");
                yield break;
            }

            // Check if we actually need to move
            float distance = Vector3.Distance(worldStartPosition, worldEndPosition);
            if (distance < 0.01f) // Very small distance, no need to pan
            {
#if UNITY_EDITOR
                Debug.Log("[CAMERA] Distance too small, no pan needed");
#endif
                _currentPanCoroutine = null;
                yield break;
            }

#if UNITY_EDITOR
            Debug.Log($"[CAMERA] Pan distance: {distance}, duration: {UiSettings.CameraPanSpeed}s");
#endif

            // Use duration from settings
            float duration = UiSettings.CameraPanSpeed;
            AnimationCurve easingCurve = UiSettings.CameraPanEasingCurve;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                if (_battleMapCamera == null)
                {
                    Debug.LogError("[CAMERA] Battle map camera became null during pan!");
                    _currentPanCoroutine = null;
                    yield break;
                }

                float normalizedTime = elapsedTime / duration;
                float easedTime = easingCurve?.Evaluate(normalizedTime) ?? normalizedTime;

                Vector3 currentPos = Vector3.Lerp(worldStartPosition, worldEndPosition, easedTime);
                _battleMapCamera.transform.position = currentPos;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure we end exactly at the target position
            if (_battleMapCamera != null)
            {
                _battleMapCamera.transform.position = worldEndPosition;
#if UNITY_EDITOR
                Debug.Log($"[CAMERA] Final position set to: {worldEndPosition}");
#endif
            }
            else
            {
                Debug.LogError(
                    "[CAMERA] Battle map camera is null when trying to set final position!"
                );
            }

            // Update camera center tracking after successful pan
            if (UiSettings.CameraPanTriesToCenterItself)
            {
#if UNITY_EDITOR
                Debug.Log(
                    $"[CAMERA] Updating camera center from {_currentCameraCenterGridPosition} to {targetGridPos} (center mode)"
                );
#endif
                _currentCameraCenterGridPosition = targetGridPos;
            }
            else
            {
                // Calculate the actual new center position for partial pans
                Vector2Int panAmount = UiSettings.DistanceFromCenterCameraPan / 2;
                Vector2Int direction = targetGridPos - _currentCameraCenterGridPosition;

                // Normalize direction
                direction.x = direction.x == 0 ? 0 : (direction.x > 0 ? 1 : -1);
                direction.y = direction.y == 0 ? 0 : (direction.y > 0 ? 1 : -1);

                Vector2Int oldCenter = _currentCameraCenterGridPosition;
                _currentCameraCenterGridPosition += new Vector2Int(
                    direction.x * panAmount.x,
                    direction.y * panAmount.y
                );
#if UNITY_EDITOR
                Debug.Log(
                    $"[CAMERA] Updating camera center from {oldCenter} to {_currentCameraCenterGridPosition} (partial pan mode)"
                );
#endif
            }

            _currentPanCoroutine = null;

#if UNITY_EDITOR
            Debug.Log(
                $"[CAMERA] Camera pan completed to position {worldEndPosition} in {duration}s"
            );
#endif
        }

        private bool ShouldPanCamera(Vector2Int currentCursorPositionInMapGridCoordinates)
        {
            if (UiSettings == null)
            {
                return false;
            }

            // Always allow pans if no pan is currently active
            if (_currentPanCoroutine == null)
            {
                Vector2Int panDistanceFromCenter = UiSettings.DistanceFromCenterCameraPan;
                Vector2Int currentDistance =
                    currentCursorPositionInMapGridCoordinates - _currentCameraCenterGridPosition;

                bool shouldPan =
                    Mathf.Abs(currentDistance.x) > panDistanceFromCenter.x
                    || Mathf.Abs(currentDistance.y) > panDistanceFromCenter.y;

                if (shouldPan)
                {
                    Debug.Log(
                        $"[CAMERA] ShouldPanCamera: Cursor beyond threshold. Distance: {currentDistance}, Threshold: {panDistanceFromCenter}"
                    );
                }

                return shouldPan;
            }

            // If a pan is in progress, only allow emergency interruptions for very large distances
            Vector2Int emergencyThreshold = UiSettings.DistanceFromCenterCameraPan * 3; // Increased threshold
            Vector2Int emergencyDistance =
                currentCursorPositionInMapGridCoordinates - _currentCameraCenterGridPosition;

            bool isEmergencyPan =
                Mathf.Abs(emergencyDistance.x) > emergencyThreshold.x
                || Mathf.Abs(emergencyDistance.y) > emergencyThreshold.y;

            if (isEmergencyPan)
            {
#if UNITY_EDITOR
                Debug.Log(
                    $"[CAMERA] Emergency pan triggered while panning. Distance: {emergencyDistance}, Emergency threshold: {emergencyThreshold}"
                );
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log(
                    $"[CAMERA] Pan in progress, ignoring cursor movement. Distance: {emergencyDistance}, Emergency threshold: {emergencyThreshold}"
                );
#endif
            }

            return isEmergencyPan;
        }
    }
}
