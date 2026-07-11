using Cinemachine;
using NaughtyAttributes;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [System.Serializable]
    public struct ExploreModeFade
    {
        public UIFade fade;
        public bool showInExploreMode;
    }

    public partial class HubManager : MonoBehaviour
    {
        [Foldout("Explore/Movement")]
        public float MaxTiltUp;

        [Foldout("Explore/Movement")]
        public float MaxTiltDown;

        [Tooltip("Time it takes to reach the target rotation (seconds)")]
        [Foldout("Explore/Movement")]
        public float lookSmoothTime = 0.15f;
        private Coroutine _lookCoroutine;

        // degrees per second movement when axis input is present.
        [Foldout("Explore/Movement")]
        public float lookStep = 10f;

        private Vector3 _baseRotation;
        private bool _hasBaseRotation;

        private float _pitchOffset;
        private float _yawOffset;

        [Foldout("Cameras")]
        [Tooltip("Single traversal vcam used for both exploring and zooming")]
        public CinemachineVirtualCamera TraversalVcam;

        [Foldout("Explore/UI")]
        public float maxPoiDistance = 10f;
        private bool _isLooking;
        private IHubSelectable _currentPoiVisual;

        [UnityEngine.Serialization.FormerlySerializedAs("zoomLayerMask")]
        [Foldout("Explore/UI")]
        public LayerMask poiLayerMask;

        [UnityEngine.Serialization.FormerlySerializedAs("normalFov")]
        [Foldout("Cameras")]
        public float ExploreFOV = 60f;

        [UnityEngine.Serialization.FormerlySerializedAs("zoomedFov")]
        [Foldout("Cameras")]
        public float ZoomFOV = 30f;

        [Tooltip("Seconds used to blend between ExploreFOV and ZoomFOV")]
        [Foldout("Cameras")]
        public float zoomTime = 0.2f;

        [Foldout("Explore/UI")]
        public UIFade FocusOverlayFade;

        [Foldout("Explore/UI")]
        public ExploreModeFade[] exploreModeFades;
        private bool useThirdPersonWalkWhenUnzoomed = true;
        private bool lockCursorWhileLooking = true;

        [HideInInspector]
        public float InputEaseDuration => GameplayPlayerSettings.Instance.InputEasing;

        [Tooltip(
            "Radius used when casting out of the camera. A larger value gives you a bigger forgiveness window around the centre of the view."
        )]
        [Foldout("Explore/UI")]
        public float zoomCastRadius = 0.25f;
        private bool _isPoiActive;
        private bool _isZoomed;
        private bool _isRunning;

        // Tilt-limit magnitudes cached from inspector values on each SetLookEnabled(true).
        private float _cachedUpLimit;
        private float _cachedDownLimit;
        private bool _loggedMissingGeneralCamera;
        private Coroutine _zoomFovCoroutine;
        private CursorLockMode _savedCursorLockMode;
        private bool _savedCursorVisible;
        private bool _hasSavedCursorState;
        private Vector2 _smoothedMoveInput;
        private Vector2 _smoothedLookInput;
        private Vector2 _moveInputVelocity;
        private Vector2 _lookInputVelocity;

        public void HandleSubLocationInput(string action)
        {
            if (TryHandleFastTravelInput(action))
            {
                return;
            }

            var currentWorldPosition = _avatarRoot.transform.position;
            if (
                action
                is InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Submit
                    or InputActionConstants.Confirm
            )
            {
                // select whatever is currently highlighted
                if (_currentPoiVisual != null && _currentPoiVisual.CanSelect)
                {
                    _currentPoiVisual.Select();
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
                ApplyExploreModeFades(isExploreMode: true);

                _hasBaseRotation = false;
                _pitchOffset = _yawOffset = 0f;
                _cachedUpLimit = Mathf.Abs(MaxTiltUp);
                _cachedDownLimit = Mathf.Abs(MaxTiltDown);
                ResetSmoothedTraversalInput();
                ApplyLookCursorState();
            }
            else
            {
                _isZoomed = false;
                _wasZoomPressed = false;
                SetTraversalFovImmediate(ExploreFOV);
                ApplyExploreModeFades(isExploreMode: false);
                SetWalkMode(false);
                ClearCurrentPoiTarget();
                FocusOverlayFade?.Hide();
                ResetSmoothedTraversalInput();
                RestoreLookCursorState();
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
    }
}
