using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public class CameraBrain : BrainComponent
    {
        private Camera _battleMapCamera;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleCursorMoved += HandleCursorMoved;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnBattleCursorMoved -= HandleCursorMoved;
        }

        public void SetBattleMapCamera(Camera cam)
        {
            _battleMapCamera = cam;
        }

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
            Debug.Log("[CAMERA] SetCameraNeutralCenter() called");

            var mapGrid = Brain?.battleBrain?.BattleObject?.Context?.mapGrid;
            if (mapGrid == null)
            {
                Debug.LogWarning("[CAMERA] SetCameraNeutralCenter: mapGrid is null");
                return Vector2Int.zero;
            }

            if (_battleMapCamera == null)
            {
                InitializeBattleMapCamera(Brain.battleBrain.BattleObject);
                return SetCameraNeutralCenter(); // Retry after initialization
            }

            // Create a ray from camera center using camera's forward direction
            Vector3 cameraCenter = _battleMapCamera.transform.position;
            Ray ray = new Ray(cameraCenter, _battleMapCamera.transform.forward);

            // Try to hit the map terrain first using the BattleGameObject's ground layer mask
            Vector3 targetPoint = cameraCenter;
            var battleObject = Brain.battleBrain.BattleObject;

            if (Physics.Raycast(ray, out RaycastHit hit, 200f, battleObject.GroundLayerMask))
            {
                targetPoint = hit.point;
            }

            // Find the closest grid point to the target position
            var allGridPoints = mapGrid.GetAllGridPoints();
            if (allGridPoints == null || allGridPoints.Count == 0)
            {
                return Vector2Int.zero;
            }

            MapGridPoint closestPoint = null;
            float closestDistance = float.MaxValue;

            foreach (var gridPoint in allGridPoints)
            {
                Vector3 gridWorldPos = mapGrid.GetTerrainAdjustedWorldPosition(
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

            return closestPoint.CoordinatesInt;
        }

        private void HandleCursorMoved(Vector2Int gridPos)
        {
            var mapGrid = Brain?.battleBrain?.BattleObject?.Context?.mapGrid;
            if (mapGrid == null)
            {
                return;
            }

            if (ShouldPanCamera(gridPos))
            {
                Debug.Log($"[CAMERA] recognizes cursor at grid position {gridPos}");
                // TODO: Actually move camera
            }
        }

        private bool ShouldPanCamera(Vector2Int currentCursorPositionInMapGridCoordinates)
        {
            // TODO: Implement logic to determine if camera should pan
            return true;
        }
    }
}
