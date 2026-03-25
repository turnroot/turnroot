using Turnroot.Utilities;
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

        private Vector3 _defaultRotation;
        private bool _hasDefaultRotation;

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
        public float zoomedFov = 52f;
        public float fovSmoothTime = 0.2f;

        [Tooltip(
            "Radius used when casting out of the camera.  A larger value gives you a bigger "
                + "forgiveness window around the centre of the view."
        )]
        public float zoomCastRadius = 0.25f;

        private float _fovVelocity;
        private bool _isZoomed;

        public void HandleSubLocationInput(string action)
        {
            if (hubManager == null)
            {
                hubManager = GetComponent<HubManager>();
            }

            if (
                action == InputActionConstants.Select
                || action == InputActionConstants.Start
                || action == InputActionConstants.Submit
                || action == InputActionConstants.Confirm
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

            if (action == "Back" || action == InputActionConstants.Cancel)
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
                _baseRotation = hubCamera.transform.localEulerAngles;
                _baseRotation.x = hubManager._brain.cameraBrain.NormalizeAngle(_baseRotation.x);
                _baseRotation.y = hubManager._brain.cameraBrain.NormalizeAngle(_baseRotation.y);

                if (!_hasDefaultRotation)
                {
                    _defaultRotation = _baseRotation;
                    _hasDefaultRotation = true;
                }

                _hasBaseRotation = true;
            }

            Vector2 inVec = GetLookInput();
            float h = inVec.x;
            float v = inVec.y;
            if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
            {
                return;
            }

            float leftLimit = Mathf.Abs(MaxTiltLeft);
            float rightLimit = Mathf.Abs(MaxTiltRight);
            float upLimit = Mathf.Abs(MaxTiltUp);
            float downLimit = Mathf.Abs(MaxTiltDown);

            _yawOffset += h * lookStep * Time.deltaTime;
            _pitchOffset -= v * lookStep * Time.deltaTime;

            if (_hasDefaultRotation)
            {
                float desiredWorldYaw = hubManager._brain.cameraBrain.NormalizeAngle(
                    _baseRotation.y + _yawOffset
                );
                float desiredWorldPitch = hubManager._brain.cameraBrain.NormalizeAngle(
                    _baseRotation.x + _pitchOffset
                );

                float defaultYaw = hubManager._brain.cameraBrain.NormalizeAngle(_defaultRotation.y);
                float defaultPitch = hubManager._brain.cameraBrain.NormalizeAngle(
                    _defaultRotation.x
                );

                float minWorldYaw = hubManager._brain.cameraBrain.NormalizeAngle(
                    defaultYaw - leftLimit
                );
                float maxWorldYaw = hubManager._brain.cameraBrain.NormalizeAngle(
                    defaultYaw + rightLimit
                );
                float minWorldPitch = hubManager._brain.cameraBrain.NormalizeAngle(
                    defaultPitch - upLimit
                );
                float maxWorldPitch = hubManager._brain.cameraBrain.NormalizeAngle(
                    defaultPitch + downLimit
                );

                float clampedWorldYaw = ClampAngleToRange(
                    desiredWorldYaw,
                    minWorldYaw,
                    maxWorldYaw
                );
                float clampedWorldPitch = ClampAngleToRange(
                    desiredWorldPitch,
                    minWorldPitch,
                    maxWorldPitch
                );

                _yawOffset = hubManager._brain.cameraBrain.NormalizeAngle(
                    clampedWorldYaw - _baseRotation.y
                );
                _pitchOffset = hubManager._brain.cameraBrain.NormalizeAngle(
                    clampedWorldPitch - _baseRotation.x
                );
            }
            else
            {
                _yawOffset = Mathf.Clamp(_yawOffset, -leftLimit, rightLimit);
                _pitchOffset = Mathf.Clamp(_pitchOffset, -upLimit, downLimit);
            }

            Vector3 targetRotation = new Vector3(
                hubManager._brain.cameraBrain.NormalizeAngle(_baseRotation.x + _pitchOffset),
                hubManager._brain.cameraBrain.NormalizeAngle(_baseRotation.y + _yawOffset),
                0f
            );

            if (_lookCoroutine != null)
            {
                StopCoroutine(_lookCoroutine);
            }

            _lookCoroutine = StartCoroutine(
                hubManager._brain.cameraBrain.SmoothLook(hubCamera, targetRotation, lookSmoothTime)
            );
            UpdateFov();
        }

        private void UpdateFov()
        {
            if (hubCamera == null)
            {
                return;
            }

            // skip raycast if Location or Chosen
            if (hubManager != null)
            {
                if (
                    hubManager.CurrentInputMode == HubInputMode.Location
                    || hubManager.CurrentInputMode == HubInputMode.Chosen
                )
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
                if (!_isZoomed || newTarget != targetCollider)
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
                    _isZoomed = true;

                    var poi = newTarget.GetComponent<HubPoiUi>();
                    if (poi != null)
                    {
                        poi.Show();
                    }
                }
            }
            else if (_isZoomed)
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
                _isZoomed = false;
            }

            float desired = _isZoomed ? zoomedFov : normalFov;
            hubCamera.fieldOfView = Mathf.SmoothDamp(
                hubCamera.fieldOfView,
                desired,
                ref _fovVelocity,
                fovSmoothTime
            );
        }

        private float ClampAngleToRange(float angle, float min, float max)
        {
            angle = hubManager._brain.cameraBrain.NormalizeAngle(angle);
            min = hubManager._brain.cameraBrain.NormalizeAngle(min);
            max = hubManager._brain.cameraBrain.NormalizeAngle(max);

            bool isInRange;
            if (min <= max)
            {
                isInRange = angle >= min && angle <= max;
            }
            else
            {
                // wrapped interval across -180/180 boundary
                isInRange = angle >= min || angle <= max;
            }

            if (isInRange)
            {
                return angle;
            }

            float deltaToMin = Mathf.Abs(Mathf.DeltaAngle(angle, min));
            float deltaToMax = Mathf.Abs(Mathf.DeltaAngle(angle, max));
            return deltaToMin < deltaToMax ? min : max;
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
