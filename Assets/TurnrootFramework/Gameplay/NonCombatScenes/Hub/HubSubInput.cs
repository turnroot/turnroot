using Turnroot.UI;
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
        // World-space tilt is clamped to [default - up,down], even when returning from POI.
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

        [UnityEngine.Serialization.FormerlySerializedAs("zoomLayerMask")]
        public LayerMask poiLayerMask;
        public float normalFov = 60f;
        public float zoomedFov = 30f;

        public UIFade FocusOverlayFade;

        [Tooltip(
            "Radius used when casting out of the camera. A larger value gives you a bigger forgiveness window around the centre of the view."
        )]
        public float zoomCastRadius = 0.25f;
        private bool _isPoiActive;
        private bool _isZoomed;

        // Tilt-limit magnitudes cached from inspector values on each SetLookEnabled(true).
        private float _cachedUpLimit;
        private float _cachedDownLimit;

        public void HandleSubLocationInput(string action)
        {
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
                if (hubCamera == null)
                {
                    hubCamera = hubManager.GeneralCamera;
                    hubCamera.fieldOfView = normalFov;
                }
                _hasBaseRotation = false;
                _pitchOffset = _yawOffset = 0f;
                _cachedUpLimit = Mathf.Abs(MaxTiltUp);
                _cachedDownLimit = Mathf.Abs(MaxTiltDown);
            }
            else
            {
                _isZoomed = false;
                _wasZoomPressed = false;
                if (hubCamera != null)
                {
                    hubCamera.fieldOfView = normalFov;
                }
                FocusOverlayFade?.Hide();
            }
        }

        private bool _wasZoomPressed;

        private void Awake()
        {
            hubManager = GetComponent<HubManager>();
        }

        private void Update()
        {
            if (!_isLooking)
            {
                return;
            }

            if (hubCamera == null)
            {
                hubCamera = hubManager.GeneralCamera;
            }

            var zoomAction = UIInputActionDefaults.RightStickClick;
            if (zoomAction == null)
            {
                "Zoom action (RightStickClick) is null — not initialized yet".LogWarning();
            }
            else if (!zoomAction.enabled)
            {
                $"Zoom action '{zoomAction.name}' exists but is DISABLED".LogWarning();
            }
            else
            {
                bool zoomPressed = zoomAction.IsPressed();
                if (zoomPressed && !_wasZoomPressed)
                {
                    _isZoomed = !_isZoomed;
                    $"Zoom toggled: _isZoomed={_isZoomed}, fov={(_isZoomed ? zoomedFov : normalFov)}, camera={hubCamera?.name}".LogInfo();
                    hubCamera.fieldOfView = _isZoomed ? zoomedFov : normalFov;
                    if (_isZoomed)
                    {
                        FocusOverlayFade?.Show();
                    }
                    else
                    {
                        FocusOverlayFade?.Hide();
                    }
                }
                _wasZoomPressed = zoomPressed;
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
            if (hubManager.CurrentInputMode is HubInputMode.Location or HubInputMode.Chosen)
            {
                return;
            }

            Vector3 origin = hubCamera.transform.position;
            Vector3 forward = hubCamera.transform.forward;

            bool rayHit = Physics.Raycast(
                origin,
                forward,
                out RaycastHit rayInfo,
                Mathf.Infinity,
                poiLayerMask
            );

            bool sphereHit = Physics.SphereCast(
                origin,
                zoomCastRadius,
                forward,
                out RaycastHit sphereInfo,
                Mathf.Infinity,
                poiLayerMask
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

            if (UiChoice.NavigateLeftAction?.IsPressed() == true)
            {
                result.x -= 1;
            }

            if (UiChoice.NavigateRightAction?.IsPressed() == true)
            {
                result.x += 1;
            }

            if (UiChoice.NavigateUpAction?.IsPressed() == true)
            {
                result.y += 1;
            }

            if (UiChoice.NavigateDownAction?.IsPressed() == true)
            {
                result.y -= 1;
            }

            return result;
        }
    }
}
