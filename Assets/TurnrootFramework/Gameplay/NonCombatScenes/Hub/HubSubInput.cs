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

        public UIFade FocusOverlayFade;

        [Tooltip(
            "Radius used when casting out of the camera. A larger value gives you a bigger forgiveness window around the centre of the view."
        )]
        public float zoomCastRadius = 0.25f;
        private bool _isPoiActive;

        // Cached per-session values — recomputed in SetLookEnabled and when default rotation is first set
        private float _cachedLeftLimit;
        private float _cachedRightLimit;
        private float _cachedUpLimit;
        private float _cachedDownLimit;
        private float _cachedMinWorldYaw;
        private float _cachedMaxWorldYaw;
        private float _cachedMinWorldPitch;
        private float _cachedMaxWorldPitch;

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

                if (!_hasDefaultRotation)
                {
                    _defaultRotation = _baseRotation;
                    _hasDefaultRotation = true;

                    // Cache world-space angle bounds — only depend on default rotation and tilt limits,
                    // both of which are fixed for the lifetime of this look session.
                    float defaultYaw = baseCam.NormalizeAngle(_defaultRotation.y);
                    float defaultPitch = baseCam.NormalizeAngle(_defaultRotation.x);
                    _cachedMinWorldYaw = baseCam.NormalizeAngle(defaultYaw - _cachedLeftLimit);
                    _cachedMaxWorldYaw = baseCam.NormalizeAngle(defaultYaw + _cachedRightLimit);
                    _cachedMinWorldPitch = baseCam.NormalizeAngle(defaultPitch - _cachedUpLimit);
                    _cachedMaxWorldPitch = baseCam.NormalizeAngle(defaultPitch + _cachedDownLimit);
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

            var cam = hubManager._brain.cameraBrain;

            _yawOffset += h * lookStep * Time.deltaTime;
            _pitchOffset -= v * lookStep * Time.deltaTime;

            if (_hasDefaultRotation)
            {
                float desiredWorldYaw = cam.NormalizeAngle(_baseRotation.y + _yawOffset);
                float desiredWorldPitch = cam.NormalizeAngle(_baseRotation.x + _pitchOffset);

                float clampedWorldYaw = ClampAngleToRange(
                    desiredWorldYaw,
                    _cachedMinWorldYaw,
                    _cachedMaxWorldYaw
                );
                float clampedWorldPitch = ClampAngleToRange(
                    desiredWorldPitch,
                    _cachedMinWorldPitch,
                    _cachedMaxWorldPitch
                );

                _yawOffset = cam.NormalizeAngle(clampedWorldYaw - _baseRotation.y);
                _pitchOffset = cam.NormalizeAngle(clampedWorldPitch - _baseRotation.x);
            }
            else
            {
                _yawOffset = Mathf.Clamp(_yawOffset, -_cachedLeftLimit, _cachedRightLimit);
                _pitchOffset = Mathf.Clamp(_pitchOffset, -_cachedUpLimit, _cachedDownLimit);
            }

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
