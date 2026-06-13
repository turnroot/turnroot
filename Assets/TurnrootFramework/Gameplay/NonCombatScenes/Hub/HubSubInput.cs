using System.Collections;
using Cinemachine;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using static Turnroot.Gameplay.NonCombatScenes.Hub.HubManager;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(HubManager))]
    public class HubSubInput : MonoBehaviour
    {
        // Absolute degree limits from the default hub traversal camera base orientation; use positive values.
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

        [Header("Cameras")]
        [Tooltip("Activated (higher priority) while zoomed")]
        public CinemachineVirtualCamera ZoomVcam;

        [Header("Cameras")]
        [Tooltip("Third-person traversal vcam — active while not zoomed")]
        public CinemachineVirtualCamera TraversalVcam;
        private Camera hubCamera;

        private Collider targetCollider;

        private bool _isLooking;

        [UnityEngine.Serialization.FormerlySerializedAs("zoomLayerMask")]
        public LayerMask poiLayerMask;
        public float normalFov = 60f;
        public float zoomedFov = 30f;
        public UIFade FocusOverlayFade;

        [Header("Third Person Walk (Phase 1)")]
        [Tooltip(
            "When enabled, non-zoom input is routed into ThirdPersonAdapter instead of hub camera look."
        )]
        public bool useThirdPersonWalkWhenUnzoomed = true;

        [Tooltip("Optional adapter that receives move/look from shared hub input actions.")]
        public HubThirdPersonAdapter ThirdPersonAdapter;

        [Tooltip(
            "Radius used when casting out of the camera. A larger value gives you a bigger forgiveness window around the centre of the view."
        )]
        public float zoomCastRadius = 0.25f;
        private bool _isPoiActive;
        private bool _isZoomed;

        // Tilt-limit magnitudes cached from inspector values on each SetLookEnabled(true).
        private float _cachedUpLimit;
        private float _cachedDownLimit;
        private bool _loggedMissingHubCamera;

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

            if (action is InputActionConstants.Back or InputActionConstants.Cancel)
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
                ThirdPersonAdapter?.SetWalkMode(false);
                ClearCurrentPoiTarget();
                FocusOverlayFade?.Hide();
            }

            // Cinemachine should only drive the camera while look/traversal mode is active.
            if (hubCamera != null && hubCamera.TryGetComponent<CinemachineBrain>(out var brain))
            {
                brain.enabled = enabled;
            }
        }

        private bool _wasZoomPressed;

        private void Awake() => hubManager = GetComponent<HubManager>();

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

            var cameraValidation = OperationResultGuards.RequireNotNull(
                hubCamera,
                nameof(hubCamera)
            );
            if (!cameraValidation.Success)
            {
                if (!_loggedMissingHubCamera)
                {
                    $"HubSubInput: Traversal input aborted. {cameraValidation.ErrorMessage}".LogError();
                    _loggedMissingHubCamera = true;
                }

                hubManager.TransitionBackToHub(hubManager.HubFadeToBlack);
                return;
            }

            UpdateZoomToggle();

            Vector2 moveInput = GetNavigateMoveInput();
            Vector2 lookInput = GetRightStickLookInput();

            if (TryHandleThirdPersonMode(moveInput, lookInput))
            {
                UpdatePoiDetection();
                return;
            }

            UpdateInspectLook(lookInput);
        }

        private bool TryHandleThirdPersonMode(Vector2 moveInput, Vector2 lookInput)
        {
            bool shouldUseThirdPersonWalk = useThirdPersonWalkWhenUnzoomed && !_isZoomed;

            if (!shouldUseThirdPersonWalk)
            {
                if (ThirdPersonAdapter != null)
                {
                    ThirdPersonAdapter.SetWalkMode(false);
                    ThirdPersonAdapter.SetInput(Vector2.zero, Vector2.zero);
                }

                return false;
            }

            var adapterValidation = OperationResultGuards.RequireNotNull(
                ThirdPersonAdapter,
                nameof(ThirdPersonAdapter)
            );
            if (!adapterValidation.Success)
            {
                $"HubSubInput: Traversal cannot continue. {adapterValidation.ErrorMessage}".LogError();
                hubManager.TransitionBackToHub(hubManager.HubFadeToBlack);
                return true;
            }

            ThirdPersonAdapter.SetWalkMode(true);
            ThirdPersonAdapter.SetInput(moveInput, lookInput);

            ClearCurrentPoiTarget();
            FocusOverlayFade?.Hide();
            return true;
        }

        private void UpdateInspectLook(Vector2 lookInput)
        {
            if (lookInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (!_hasBaseRotation)
            {
                var baseCam = hubManager._brain.cameraBrain;
                _baseRotation = hubCamera.transform.localEulerAngles;
                _baseRotation.x = baseCam.NormalizeAngle(_baseRotation.x);
                _baseRotation.y = baseCam.NormalizeAngle(_baseRotation.y);
                _hasBaseRotation = true;
            }

            float h = lookInput.x;
            float v = lookInput.y;

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

                if (ZoomVcam != null)
                {
                    ZoomVcam.Priority = _isZoomed ? 20 : 0;
                }
                if (TraversalVcam != null)
                {
                    TraversalVcam.Priority = _isZoomed ? 0 : 10;
                }

                if (!_isZoomed)
                {
                    ClearCurrentPoiTarget();
                    FocusOverlayFade?.Hide();
                }
            }

            _wasZoomPressed = zoomPressed;
        }

        private void ClearCurrentPoiTarget()
        {
            HideCurrentPoiTarget();

            targetCollider = null;
            _isPoiActive = false;
        }

        private void HideCurrentPoiTarget()
        {
            if (targetCollider == null)
            {
                return;
            }

            var poi = targetCollider.GetComponent<HubPoiUi>();
            if (poi != null)
            {
                poi.Hide();
            }
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
                    HideCurrentPoiTarget();

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
                ClearCurrentPoiTarget();
                FocusOverlayFade?.Hide();
            }
        }

        private Vector2 GetNavigateMoveInput()
        {
            if (UIInputActionDefaults.Navigate != null && UIInputActionDefaults.Navigate.enabled)
            {
                Vector2 value = UIInputActionDefaults.Navigate.ReadValue<Vector2>();
                if (value.sqrMagnitude > 0.0001f)
                {
                    return Vector2.ClampMagnitude(value, 1f);
                }
            }

            return Vector2.zero;
        }

        private Vector2 GetRightStickLookInput()
        {
            if (
                UIInputActionDefaults.RightStickMove != null
                && UIInputActionDefaults.RightStickMove.enabled
            )
            {
                Vector2 analog = UIInputActionDefaults.RightStickMove.ReadValue<Vector2>();
                if (analog.sqrMagnitude > 0.0001f)
                {
                    return ApplyGameSpeedScale(Vector2.ClampMagnitude(analog, 1f));
                }
            }

            return Vector2.zero;
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
    }
}
