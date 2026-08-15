using System.Collections;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        private void UpdateInspectLook(Vector2 lookInput)
        {
            if (lookInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (!_hasBaseRotation)
            {
                var baseCam = _brain.cameraBrain;
                _baseRotation = GeneralCamera.transform.localEulerAngles;
                _baseRotation.x = baseCam.NormalizeAngle(_baseRotation.x);
                _baseRotation.y = baseCam.NormalizeAngle(_baseRotation.y);
                _hasBaseRotation = true;
            }

            float h = lookInput.x;
            float v = lookInput.y;

            var cam = _brain.cameraBrain;

            _yawOffset += h * lookStep * Time.deltaTime;
            _pitchOffset -= v * lookStep * Time.deltaTime;

            _pitchOffset = Mathf.Clamp(_pitchOffset, -_cachedUpLimit, _cachedDownLimit);

            Vector3 targetRotation = new Vector3(
                cam.NormalizeAngle(_baseRotation.x + _pitchOffset),
                cam.NormalizeAngle(_baseRotation.y + _yawOffset),
                0f
            );

            if (_lookCoroutine != null)
            {
                StopCoroutine(_lookCoroutine);
            }

            _lookCoroutine = StartCoroutine(
                _brain.cameraBrain.SmoothLook(GeneralCamera, targetRotation, lookSmoothTime)
            );
            UpdatePoiDetection();
        }

        private void UpdateZoomToggle()
        {
            var zoomAction = UiChoice.RightStickClickAction;
            if (zoomAction == null || !zoomAction.enabled)
            {
                return;
            }

            bool zoomPressed = zoomAction.IsPressed();
            if (zoomPressed && !_wasZoomPressed)
            {
                _isZoomed = !_isZoomed;

                float targetFov = _isZoomed ? ZoomFOV : ExploreFOV;
                StartTraversalFovTween(targetFov);
                ApplyExploreModeFades(isExploreMode: !_isZoomed);

                if (!_isZoomed)
                {
                    ClearCurrentPoiTarget();
                    FocusOverlayFade?.Hide();
                }
            }

            _wasZoomPressed = zoomPressed;
        }

        private void SetTraversalFovImmediate(float fov)
        {
            if (TraversalVcam != null)
            {
                var lens = TraversalVcam.m_Lens;
                lens.FieldOfView = fov;
                TraversalVcam.m_Lens = lens;
            }

            if (GeneralCamera != null)
            {
                GeneralCamera.fieldOfView = fov;
            }
        }

        private void SetOverlayCameraFovImmediate(float fov)
        {
            if (OverlayCamera != null)
            {
                OverlayCamera.fieldOfView = fov;
            }
        }

        private void StartTraversalFovTween(float targetFov)
        {
            if (_zoomFovCoroutine != null)
            {
                StopCoroutine(_zoomFovCoroutine);
            }

            _zoomFovCoroutine = StartCoroutine(TweenTraversalFov(targetFov));
        }

        private IEnumerator TweenTraversalFov(float targetFov)
        {
            if (TraversalVcam == null)
            {
                SetTraversalFovImmediate(targetFov);
                _zoomFovCoroutine = null;
                yield break;
            }

            var startLens = TraversalVcam.m_Lens;
            var startOverlayLens = OverlayCamera?.fieldOfView ?? 0f;
            float startFov = startLens.FieldOfView;
            float duration = Mathf.Max(0.0001f, zoomTime);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetTraversalFovImmediate(Mathf.Lerp(startFov, targetFov, t));
                SetOverlayCameraFovImmediate(Mathf.Lerp(startOverlayLens, targetFov, t));
                yield return null;
            }

            SetTraversalFovImmediate(targetFov);
            SetOverlayCameraFovImmediate(targetFov);
            _zoomFovCoroutine = null;
        }

        private void ClearCurrentPoiTarget()
        {
            _currentPoiVisual?.Hide();
            _currentPoiVisual = null;
            _isPoiActive = false;
        }

        private void UpdatePoiDetection()
        {
            if (GeneralCamera == null)
            {
                return;
            }

            // skip raycast if Location or Chosen
            if (CurrentInputMode is HubInputMode.Location or HubInputMode.Chosen)
            {
                if (_isPoiActive)
                {
                    ClearCurrentPoiTarget();
                    FocusOverlayFade?.Hide();
                }
                return;
            }

            Vector3 origin = GeneralCamera.transform.position;
            Vector3 forward = GeneralCamera.transform.forward;

            bool rayHit = Physics.Raycast(
                origin,
                forward,
                out RaycastHit rayInfo,
                maxPoiDistance,
                poiLayerMask
            );

            bool sphereHit = Physics.SphereCast(
                origin,
                zoomCastRadius,
                forward,
                out RaycastHit sphereInfo,
                maxPoiDistance,
                poiLayerMask
            );

            Collider colliderHit =
                rayHit ? rayInfo.collider
                : sphereHit ? sphereInfo.collider
                : null;
            float hitDistance =
                rayHit ? rayInfo.distance
                : sphereHit ? sphereInfo.distance
                : 0f;

            IHubSelectable newTarget = null;
            if (colliderHit != null && TryGetSelectableTarget(colliderHit, out var fromCollider))
            {
                // ILookTargetable objects cap their own selectable range independently of maxPoiDistance.
                bool inRange =
                    fromCollider is not ILookTargetable lt || hitDistance <= lt.LookDistance;
                if (inRange)
                {
                    newTarget = fromCollider;
                }
            }

            if (newTarget != null)
            {
                if (!_isPoiActive || newTarget != _currentPoiVisual)
                {
                    ClearCurrentPoiTarget();
                    _currentPoiVisual = newTarget;
                    _isPoiActive = true;
                    newTarget.Show();
                    FocusOverlayFade?.Show();
                }
            }
            else if (_isPoiActive)
            {
                ClearCurrentPoiTarget();
                FocusOverlayFade?.Hide();
            }
        }

        private static bool TryGetSelectableTarget(
            Collider candidate,
            out IHubSelectable selectable
        )
        {
            selectable = null;
            if (candidate == null)
            {
                return false;
            }

            if (candidate.TryGetComponent(out selectable))
            {
                return true;
            }

            selectable = candidate.GetComponentInParent<IHubSelectable>();
            return selectable != null;
        }

        private Vector2 GetNavigateMoveInput()
        {
            Vector2 target = Vector2.zero;
            if (UIInputActionDefaults.Navigate != null && UIInputActionDefaults.Navigate.enabled)
            {
                Vector2 value = UIInputActionDefaults.Navigate.ReadValue<Vector2>();
                if (value.sqrMagnitude > 0.0001f)
                {
                    target = Vector2.ClampMagnitude(value, 1f);
                }
            }

            _smoothedMoveInput = SmoothTraversalInput(
                _smoothedMoveInput,
                target,
                ref _moveInputVelocity
            );
            return _smoothedMoveInput;
        }

        private Vector2 GetRightStickLookInput()
        {
            Vector2 target = Vector2.zero;
            if (
                UIInputActionDefaults.RightStickMove != null
                && UIInputActionDefaults.RightStickMove.enabled
            )
            {
                Vector2 analog = UIInputActionDefaults.RightStickMove.ReadValue<Vector2>();
                if (analog.sqrMagnitude > 0.0001f)
                {
                    target = ApplyGameSpeedScale(Vector2.ClampMagnitude(analog, 1f));
                }
            }

            _smoothedLookInput = SmoothTraversalInput(
                _smoothedLookInput,
                target,
                ref _lookInputVelocity
            );
            return _smoothedLookInput;
        }

        private Vector2 SmoothTraversalInput(Vector2 current, Vector2 target, ref Vector2 velocity)
        {
            float duration = Mathf.Max(0f, InputEaseDuration);
            if (duration <= 0.0001f)
            {
                velocity = Vector2.zero;
                return target;
            }

            Vector2 smoothed = Vector2.SmoothDamp(
                current,
                target,
                ref velocity,
                duration,
                Mathf.Infinity,
                Time.deltaTime
            );

            // Snap tiny trailing values so the avatar/camera can settle to a true zero input.
            if (target.sqrMagnitude <= 0.000001f && smoothed.sqrMagnitude <= 0.000001f)
            {
                velocity = Vector2.zero;
                return Vector2.zero;
            }

            return smoothed;
        }

        private void ResetSmoothedTraversalInput()
        {
            _smoothedMoveInput = Vector2.zero;
            _smoothedLookInput = Vector2.zero;
            _moveInputVelocity = Vector2.zero;
            _lookInputVelocity = Vector2.zero;
        }

        private void ApplyExploreModeFades(bool isExploreMode)
        {
            if (exploreModeFades == null || exploreModeFades.Length == 0)
            {
                return;
            }

            for (int i = 0; i < exploreModeFades.Length; i++)
            {
                var fade = exploreModeFades[i];
                if (fade.fade == null)
                {
                    continue;
                }

                bool shouldShow = isExploreMode ? fade.showInExploreMode : !fade.showInExploreMode;
                if (shouldShow)
                {
                    fade.fade.Show();
                }
                else
                {
                    fade.fade.Hide();
                }
            }
        }

        private Vector2 ApplyGameSpeedScale(Vector2 input)
        {
            var gameSpeed = GameplayPlayerSettings.Instance.SpeedSetting;

            switch (gameSpeed)
            {
                case GameplayPlayerSettings.GameSpeed.Normal:
                    break;
                case GameplayPlayerSettings.GameSpeed.Fast:
                    input *= 1.25f;
                    break;
                case GameplayPlayerSettings.GameSpeed.VeryFast:
                    input *= 1.5f;
                    break;
            }

            return input;
        }

        private void ApplyLookCursorState()
        {
            if (!_hasSavedCursorState)
            {
                _savedCursorLockMode = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                _hasSavedCursorState = true;
            }

            if (lockCursorWhileLooking)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void RestoreLookCursorState()
        {
            if (!_hasSavedCursorState)
            {
                return;
            }

            Cursor.lockState = _savedCursorLockMode;
            Cursor.visible = _savedCursorVisible;
            _hasSavedCursorState = false;
        }

        private float ApplyGameSpeedScaleFloat(float value)
        {
            var gameSpeed = GameplayPlayerSettings.Instance.SpeedSetting;

            switch (gameSpeed)
            {
                case GameplayPlayerSettings.GameSpeed.Normal:
                    break;
                case GameplayPlayerSettings.GameSpeed.Fast:
                    value *= 1.25f;
                    break;
                case GameplayPlayerSettings.GameSpeed.VeryFast:
                    value *= 1.5f;
                    break;
            }

            return value;
        }
    }
}
