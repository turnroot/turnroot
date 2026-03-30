using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using static Turnroot.Gameplay.NonCombatScenes.Hub.HubManager;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public class HubSubInput : MonoBehaviour
    {
        // absolute degree limits from the default hub sublocation base orientation; use positive values.
        // World-space tilt is clamped to [default - left,right] and [default - up,down], even when returning from POI.
        public float MaxTiltLeft;
        public float MaxTiltRight;
        public float MaxTiltUp;
        public float MaxTiltDown;

        [Tooltip("Time it takes to reach the target rotation (seconds)")]
        public float lookSmoothTime = 0.15f;
        private Coroutine _lookCoroutine;

        // degrees per second movement when axis input is present.
        public float lookStep = 10f;

        private Vector3 _baseRotation;
        private bool _hasBaseRotation;

        private float _pitchOffset;
        private float _yawOffset;

        private HubManager hubManager;
        private Camera hubCamera;

        private Collider targetCollider;

        private bool _isLooking;

        public LayerMask zoomLayerMask;
        public float normalFov = 60f;

        public UIFade FocusOverlayFade;

        [Tooltip(
            "Radius used when casting out of the camera. A larger value gives you a bigger forgiveness window around the centre of the view."
        )]
        public float zoomCastRadius = 0.25f;
        private bool _isPoiActive;

        // Tilt-limit magnitudes cached from inspector values on each SetLookEnabled(true).
        private float _cachedLeftLimit;
        private float _cachedRightLimit;
        private float _cachedUpLimit;
        private float _cachedDownLimit;

        public void HandleSubLocationInput(string action)
        {
            if (hubManager == null)
            {
                hubManager = GetComponent<HubManager>();
            }

            if (
                action
                is InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Submit
                    or InputActionConstants.Confirm
            )
            {
                // check if there is a highlighted POI and can be selected
                if (targetCollider != null)
                {
                    var poi = targetCollider.GetComponent<HubPoiUi>();
                    if (poi != null && poi.CanSelect)
                    {
                        poi.Select();
                    }
                }
            }

            if (action is "Back" or InputActionConstants.Cancel)
            {
                hubManager.TransitionBackToHub(hubManager.HubFadeToBlack);
            }
        }

        public void SetLookEnabled(bool enabled)
        {
            if (enabled == _isLooking)
            {
                return;
            }

            _isLooking = enabled;

            if (_lookCoroutine != null)
            {
                StopCoroutine(_lookCoroutine);
                _lookCoroutine = null;
            }

            if (_isLooking)
            {
                _hasBaseRotation = false;
                _pitchOffset = _yawOffset = 0f;
                _cachedLeftLimit = Mathf.Abs(MaxTiltLeft);
                _cachedRightLimit = Mathf.Abs(MaxTiltRight);
                _cachedUpLimit = Mathf.Abs(MaxTiltUp);
                _cachedDownLimit = Mathf.Abs(MaxTiltDown);
            }
        }

        private void Update()
        {
            if (!_isLooking)
            {
                return;
            }

            if (hubManager == null)
            {
                hubManager = GetComponent<HubManager>();
            }

            if (hubCamera == null)
            {
                hubCamera = hubManager.GeneralCamera;
                hubCamera.fieldOfView = normalFov;
            }

            if (!_hasBaseRotation)
            {
                var baseCam = hubManager._brain.cameraBrain;
                _baseRotation = hubCamera.transform.localEulerAngles;
                _baseRotation.x = baseCam.NormalizeAngle(_baseRotation.x);
                _baseRotation.y = baseCam.NormalizeAngle(_baseRotation.y);
                _hasBaseRotation = true;
            }

            Vector2 inVec = GetLookInput();
            float h = inVec.x;
            float v = inVec.y;
            if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
            {
                return;
            }

            var cam = hubManager._brain.cameraBrain;

            _yawOffset += h * lookStep * Time.deltaTime;
            _pitchOffset -= v * lookStep * Time.deltaTime;

            // Clamp the scalar offsets directly. This is always correct: _yawOffset and
            // _pitchOffset are degree-of-deviation scalars (never exceeding ±180), so
            // Mathf.Clamp with scalar limits needs no wrapping logic. The previous world-space
            // approach (ClampAngleToRange) introduced a wrapped-interval bug: going to max-left
            // then max-right caused the wrapped "angle >= min || angle <= max" check to pass for
            // all angles near the ±180 boundary, disabling clamping for the rest of the session.
            _yawOffset = Mathf.Clamp(_yawOffset, -_cachedLeftLimit, _cachedRightLimit);
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
                hubManager._brain.cameraBrain.SmoothLook(hubCamera, targetRotation, lookSmoothTime)
            );
            UpdatePoiDetection();
        }

        private void UpdatePoiDetection()
        {
            if (hubCamera == null)
            {
                return;
            }

            // skip raycast if Location or Chosen
            if (hubManager != null)
            {
                if (hubManager.CurrentInputMode is HubInputMode.Location or HubInputMode.Chosen)
                {
                    return;
                }
            }

            Vector3 origin = hubCamera.transform.position;
            Vector3 forward = hubCamera.transform.forward;

            bool rayHit = Physics.Raycast(
                origin,
                forward,
                out RaycastHit rayInfo,
                Mathf.Infinity,
                zoomLayerMask
            );

            bool sphereHit = Physics.SphereCast(
                origin,
                zoomCastRadius,
                forward,
                out RaycastHit sphereInfo,
                Mathf.Infinity,
                zoomLayerMask
            );

            Collider newTarget = null;
            if (rayHit)
            {
                newTarget = rayInfo.collider;
            }
            else if (sphereHit)
            {
                newTarget = sphereInfo.collider;
            }

            if (newTarget != null)
            {
                if (!_isPoiActive || newTarget != targetCollider)
                {
                    // hide previous POI UI if present
                    if (targetCollider != null)
                    {
                        var oldPoi = targetCollider.GetComponent<HubPoiUi>();
                        if (oldPoi != null)
                        {
                            oldPoi.Hide();
                        }
                    }

                    targetCollider = newTarget;
                    _isPoiActive = true;

                    var poi = newTarget.GetComponent<HubPoiUi>();
                    if (poi != null)
                    {
                        poi.Show();
                        FocusOverlayFade?.Show();
                    }
                }
            }
            else if (_isPoiActive)
            {
                if (targetCollider != null)
                {
                    var oldPoi = targetCollider.GetComponent<HubPoiUi>();
                    if (oldPoi != null)
                    {
                        oldPoi.Hide();
                    }
                }

                targetCollider = null;
                _isPoiActive = false;
                FocusOverlayFade?.Hide();
            }
        }

        private Vector2 GetLookInput()
        {
            Vector2 result = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
                {
                    result.x -= 1;
                }

                if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
                {
                    result.x += 1;
                }

                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
                {
                    result.y += 1;
                }

                if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
                {
                    result.y -= 1;
                }
            }

            if (Gamepad.current != null)
            {
                result += Gamepad.current.leftStick.ReadValue();
                result += Gamepad.current.rightStick.ReadValue();
                result += Gamepad.current.dpad.ReadValue();
            }

            return result;
        }
    }
}
