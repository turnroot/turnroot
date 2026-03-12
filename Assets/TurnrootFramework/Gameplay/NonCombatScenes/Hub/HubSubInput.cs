using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public class HubSubInput : MonoBehaviour
    {
        // absolute degree limits from the base orientation; use positive values.
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
            // TODO: handle select/back if required
        }

        private float NormalizeAngle(float a)
        {
            // convert 0..360 to -180..180 for easier clamping math
            if (a > 180f)
            {
                a -= 360f;
            }

            return a;
        }

        public void SetLookEnabled(bool enabled)
        {
            if (enabled == _isLooking)
            {
                return;
            }

            _isLooking = enabled;
            if (_isLooking)
            {
                _hasBaseRotation = false;
                _pitchOffset = _yawOffset = 0f;
                if (_lookCoroutine != null)
                {
                    StopCoroutine(_lookCoroutine);
                }
            }
        }

        private System.Collections.IEnumerator SmoothLook(Camera cam, Vector3 targetRotation)
        {
            float elapsed = 0f;
            Vector3 start = cam.transform.localEulerAngles;
            start.x = NormalizeAngle(start.x);
            start.y = NormalizeAngle(start.y);
            start.z = 0f; // always zero roll

            while (elapsed < lookSmoothTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lookSmoothTime);
                // use DeltaAngle to find shortest rotation direction across the
                // 180 boundary (avoids wild spins when base+offset crosses +-180).
                float pitchDelta = Mathf.DeltaAngle(start.x, targetRotation.x);
                float yawDelta = Mathf.DeltaAngle(start.y, targetRotation.y);
                float pitch = start.x + pitchDelta * t;
                float yaw = start.y + yawDelta * t;
                cam.transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
                yield return null;
            }

            cam.transform.localEulerAngles = new Vector3(targetRotation.x, targetRotation.y, 0f);
            _lookCoroutine = null;
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
                _baseRotation.x = NormalizeAngle(_baseRotation.x);
                _baseRotation.y = NormalizeAngle(_baseRotation.y);
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

            _yawOffset = Mathf.Clamp(
                _yawOffset + h * lookStep * Time.deltaTime,
                -leftLimit,
                rightLimit
            );
            _pitchOffset = Mathf.Clamp(
                _pitchOffset - v * lookStep * Time.deltaTime,
                -upLimit,
                downLimit
            );

            Vector3 targetRotation = new Vector3(
                NormalizeAngle(_baseRotation.x + _pitchOffset),
                NormalizeAngle(_baseRotation.y + _yawOffset),
                0f
            );

            if (_lookCoroutine != null)
            {
                StopCoroutine(_lookCoroutine);
            }

            _lookCoroutine = StartCoroutine(SmoothLook(hubCamera, targetRotation));
            UpdateFov();
        }

        private void UpdateFov()
        {
            if (hubCamera == null)
            {
                return;
            }

            // use both a narrow ray (centre of screen) and the forgiving sphere cast.
            // the raycast wins whenever it hits something, which allows us to switch to a
            // new collider even if a nearer object partially blocks the sphere.
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
                            oldPoi.Hide();
                    }

                    Debug.Log($"Zoom hit {newTarget.name}");
                    targetCollider = newTarget;
                    _isZoomed = true;

                    var poi = newTarget.GetComponent<HubPoiUi>();
                    if (poi != null)
                        poi.Show();
                }
            }
            else if (_isZoomed)
            {
                Debug.Log("Zoom cleared");
                // hide previous POI UI
                if (targetCollider != null)
                {
                    var oldPoi = targetCollider.GetComponent<HubPoiUi>();
                    if (oldPoi != null)
                        oldPoi.Hide();
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
