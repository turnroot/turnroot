using System.Collections;
using Cinemachine;
using NaughtyAttributes;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        [BoxGroup("Look/Traversal Settings")]
        public float MaxTiltUp;

        [BoxGroup("Look/Traversal Settings")]
        public float MaxTiltDown;

        [Tooltip("Time it takes to reach the target rotation (seconds)")]
        [BoxGroup("Look/Traversal Settings")]
        public float lookSmoothTime = 0.15f;
        private Coroutine _lookCoroutine;

        // degrees per second movement when axis input is present.
        [BoxGroup("Look/Traversal Settings")]
        public float lookStep = 10f;

        private Vector3 _baseRotation;
        private bool _hasBaseRotation;

        private float _pitchOffset;
        private float _yawOffset;

        [BoxGroup("Look/Traversal Settings")]
        [Tooltip("Single traversal vcam used for both exploring and zooming")]
        public CinemachineVirtualCamera TraversalVcam;
        private Collider targetCollider;

        [BoxGroup("Look/Traversal Settings")]
        public float maxPoiDistance = 10f;
        private bool _isLooking;
        private HubFadableVisualBase _currentPoiVisual;

        [UnityEngine.Serialization.FormerlySerializedAs("zoomLayerMask")]
        [BoxGroup("Look/Traversal Settings")]
        public LayerMask poiLayerMask;

        [UnityEngine.Serialization.FormerlySerializedAs("normalFov")]
        [BoxGroup("Look/Traversal Settings")]
        public float ExploreFOV = 60f;

        [UnityEngine.Serialization.FormerlySerializedAs("zoomedFov")]
        [BoxGroup("Look/Traversal Settings")]
        public float ZoomFOV = 30f;

        [Tooltip("Seconds used to blend between ExploreFOV and ZoomFOV")]
        [BoxGroup("Look/Traversal Settings")]
        public float zoomTime = 0.2f;

        [BoxGroup("Look/Traversal Settings")]
        public UIFade FocusOverlayFade;

        [BoxGroup("Look/Traversal Settings")]
        [Tooltip(
            "When enabled, non-zoom input is routed into ThirdPersonAdapter instead of hub camera look."
        )]
        public bool useThirdPersonWalkWhenUnzoomed = true;

        [Tooltip(
            "Radius used when casting out of the camera. A larger value gives you a bigger forgiveness window around the centre of the view."
        )]
        [BoxGroup("Look/Traversal Settings")]
        public float zoomCastRadius = 0.25f;
        private bool _isPoiActive;
        private bool _isZoomed;
        private bool _isRunning;

        // Tilt-limit magnitudes cached from inspector values on each SetLookEnabled(true).
        private float _cachedUpLimit;
        private float _cachedDownLimit;
        private bool _loggedMissingGeneralCamera;
        private Coroutine _zoomFovCoroutine;

        public void HandleSubLocationInput(string action)
        {
            var currentWorldPosition = _avatarRoot.transform.position;
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
                    _ = TrySelectTarget(targetCollider);
                }
            }

            if (action is InputActionConstants.Back or InputActionConstants.Cancel)
            {
                // if we're exploring/walking/traversing, back should take us to the main hub menu with a random locatipn
                // if we're in a POI menu, back should take us out of the menu and back to exploring/walking/traversing at the same location
                // we can use DoTransitionBackToHub to go back to the main hub menu
                // to go back to exploring/walking/traversing, we can just set the input mode back to Location, which will also hide any active POI
                if (CurrentInputMode == HubInputMode.Location)
                {
                    TransitionBackToHub(fadeToBlack: HubFadeToBlack);
                }
                else
                {
                    SetInputMode(HubInputMode.Location);
                    TransitionBackToHub(fadeToBlack: HubFadeToBlack, currentWorldPosition);
                }
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

            if (_zoomFovCoroutine != null)
            {
                StopCoroutine(_zoomFovCoroutine);
                _zoomFovCoroutine = null;
            }

            if (_isLooking)
            {
                SetTraversalFovImmediate(ExploreFOV);

                _hasBaseRotation = false;
                _pitchOffset = _yawOffset = 0f;
                _cachedUpLimit = Mathf.Abs(MaxTiltUp);
                _cachedDownLimit = Mathf.Abs(MaxTiltDown);
            }
            else
            {
                _isZoomed = false;
                _wasZoomPressed = false;
                SetTraversalFovImmediate(ExploreFOV);
                SetWalkMode(false);
                ClearCurrentPoiTarget();
                FocusOverlayFade?.Hide();
            }

            // Cinemachine should only drive the camera while look/traversal mode is active.
            if (
                GeneralCamera != null
                && GeneralCamera.TryGetComponent<CinemachineBrain>(out var brain)
            )
            {
                brain.enabled = enabled;
            }
        }

        private bool _wasZoomPressed;

        private bool _wasRunPressed;

        private void Awake()
        {
            if (NavMeshAgent != null)
            {
                NavMeshAgent.updateRotation = false;
                NavMeshAgent.updatePosition = true;
                NavMeshAgent.speed = MoveSpeed;
            }
        }

        private void Update()
        {
            if (!_isLooking)
            {
                return;
            }

            HandleWalk();

            UpdateZoomToggle();

            var runAction = UIInputActionDefaults.LeftStickClick;

            bool runPressed = runAction.IsPressed();
            if (runPressed && !_wasRunPressed)
            {
                _isRunning = !_isRunning;
            }
            _wasRunPressed = runPressed;

            Vector2 moveInput = GetNavigateMoveInput();
            Vector2 lookInput = GetRightStickLookInput();

            if (TryHandleThirdPersonMode(moveInput, lookInput))
            {
                UpdateRunning(_isRunning);
                UpdatePoiDetection();
                return;
            }
        }

        private void UpdateRunning(bool isRunning) => SetRunning(isRunning);

        private bool TryHandleThirdPersonMode(Vector2 moveInput, Vector2 lookInput)
        {
            bool shouldUseThirdPersonWalk = useThirdPersonWalkWhenUnzoomed;

            if (!shouldUseThirdPersonWalk)
            {
                SetWalkMode(false);
                SetInput(Vector2.zero, Vector2.zero);

                if (_isZoomed)
                {
                    ApplyLookOnly(lookInput);
                }

                return false;
            }

            SetWalkMode(true);
            SetInput(moveInput, lookInput);
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
            float startFov = startLens.FieldOfView;
            float duration = Mathf.Max(0.0001f, zoomTime);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetTraversalFovImmediate(Mathf.Lerp(startFov, targetFov, t));
                yield return null;
            }

            SetTraversalFovImmediate(targetFov);
            _zoomFovCoroutine = null;
        }

        private void ClearCurrentPoiTarget()
        {
            HideCurrentPoiTarget();

            targetCollider = null;
            _currentPoiVisual = null;
            _isPoiActive = false;
        }

        private void HideCurrentPoiTarget()
        {
            if (_currentPoiVisual != null)
            {
                _currentPoiVisual.Hide();
                return;
            }

            var target = GetPoiVisualTarget(targetCollider);
            if (target != null)
            {
                target.Hide();
            }
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

            Collider newTarget = null;
            if (rayHit)
            {
                newTarget = rayInfo.collider;
            }
            else if (sphereHit)
            {
                newTarget = sphereInfo.collider;
            }

            HubFadableVisualBase newPoiVisual = GetPoiVisualTarget(newTarget);

            if (newPoiVisual != null)
            {
                if (!_isPoiActive || newPoiVisual != _currentPoiVisual)
                {
                    HideCurrentPoiTarget();

                    targetCollider = newTarget;
                    _currentPoiVisual = newPoiVisual;
                    _isPoiActive = true;

                    newPoiVisual.Show();
                    FocusOverlayFade?.Show();
                }
            }
            else if (_isPoiActive)
            {
                ClearCurrentPoiTarget();
                FocusOverlayFade?.Hide();
            }
        }

        private static HubFadableVisualBase GetPoiVisualTarget(Collider candidate)
        {
            if (candidate == null)
            {
                return null;
            }

            if (candidate.TryGetComponent<HubPoiUi>(out var poiUi))
            {
                return poiUi;
            }

            if (candidate.TryGetComponent<HubInformation>(out var information))
            {
                return information;
            }

            poiUi = candidate.GetComponentInParent<HubPoiUi>();
            if (poiUi != null)
            {
                return poiUi;
            }

            information = candidate.GetComponentInParent<HubInformation>();
            if (information != null)
            {
                return information;
            }

            return null;
        }

        private static bool TrySelectTarget(Collider candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate.TryGetComponent<HubPoiUi>(out var poiUi))
            {
                if (!poiUi.CanSelect)
                {
                    return false;
                }

                poiUi.Select();
                return true;
            }

            poiUi = candidate.GetComponentInParent<HubPoiUi>();
            if (poiUi != null)
            {
                if (!poiUi.CanSelect)
                {
                    return false;
                }

                poiUi.Select();
                return true;
            }

            if (candidate.TryGetComponent<HubInformation>(out var information))
            {
                information.Select();
                return true;
            }

            information = candidate.GetComponentInParent<HubInformation>();
            if (information != null)
            {
                information.Select();
                return true;
            }

            return false;
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
