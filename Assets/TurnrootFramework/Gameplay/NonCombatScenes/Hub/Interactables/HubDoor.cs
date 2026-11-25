using System.Collections;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class HubDoor : MonoBehaviour
    {
        public UnityEvent onDoorApproached;

        public UnityEvent onDoorEntered;

        public UnityEvent goingInside;

        public UnityEvent goingOutside;
        public bool Locked;

        public bool AutoCloseBehindPlayer;
        [Tooltip("Enable only on doors that separate the interior from outdoors.")]
        public bool IsBetweenInsideAndOutside = true;
        [Tooltip("The interior is on the positive side of the trigger's thinner horizontal axis.")]
        public bool InsideIsPositiveSide = true;
        private NavMeshObstacle DoorObstacle;

        public Collider PlayerCollider;
        private BoxCollider DoorCollider;

        [Tooltip("Assign the door mesh here so the trigger and NavMesh obstacle stay stationary.")]
        public Transform DoorMeshTransform;

        public Transform DoorTransformOpen;

        public Transform DoorTransformClosed;

        public float OpenSpeed = 1f;

        public AudioClip DoorOpenSfx;

        public AudioClip DoorCloseSfx;

        public AudioSource AudioSource;

        public UIFade LockedWarningUi;

        private enum DoorState
        {
            Closed,
            Opening,
            Open,
            Closing,
        }

        private DoorState _doorState = DoorState.Closed;
        private Coroutine _doorCoroutine;
        private int _playerEntrySide;
        private bool _playerCanTransition;
        private bool _insideEventInvokedForPass;
        private bool _brainSubscribed;
        private bool _closeWhenOpen;
        private Transform _doorVisualTransform;
        private Vector3 _doorwayLocalAxis;

        private void Awake()
        {
            DoorCollider = GetComponent<BoxCollider>();
            DoorObstacle = GetComponent<NavMeshObstacle>();
            _doorVisualTransform = DoorMeshTransform != null ? DoorMeshTransform : transform;
            if (_doorVisualTransform == transform)
            {
                Debug.LogWarning(
                    $"{nameof(HubDoor)} should animate a child DoorMeshTransform so its trigger stays stationary.",
                    this
                );
            }

            Vector3 scaledSize = Vector3.Scale(DoorCollider.size, transform.lossyScale);
            _doorwayLocalAxis = Mathf.Abs(scaledSize.x) < Mathf.Abs(scaledSize.z)
                ? Vector3.right
                : Vector3.forward;
        }

        private void OnEnable()
        {
            var brain = Brain.Brain.Instance;
            if (brain != null && !_brainSubscribed)
            {
                brain.OnSceneChanged += HandleSceneChanged;
                _brainSubscribed = true;
            }
        }

        private void OnDisable()
        {
            StopDoorRoutine();
            _doorState = DoorState.Closed;
            _playerCanTransition = false;
            _insideEventInvokedForPass = false;
            _closeWhenOpen = false;

            var brain = Brain.Brain.Instance;
            if (brain != null && _brainSubscribed)
            {
                brain.OnSceneChanged -= HandleSceneChanged;
                _brainSubscribed = false;
            }
        }

        private void HandleSceneChanged(string sceneName, string displayName)
        {
            StopDoorRoutine();
            _doorState = DoorState.Closed;
            _playerCanTransition = false;
            _insideEventInvokedForPass = false;
            _closeWhenOpen = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Locked)
            {
                if (other == PlayerCollider)
                {
                    LockedWarningUi?.Show();
                }
                return;
            }
            if (other == PlayerCollider)
            {
                _playerCanTransition = true;
                _insideEventInvokedForPass = false;
                _closeWhenOpen = false;
                _playerEntrySide = GetPlayerDoorSide();
                if (IsBetweenInsideAndOutside && IsExteriorSide(_playerEntrySide))
                {
                    goingInside?.Invoke();
                    _insideEventInvokedForPass = true;
                }

                if (_doorState == DoorState.Closed || _doorState == DoorState.Closing)
                {
                    StartDoorRoutine(ApproachDoor());
                }
                else if (_doorState == DoorState.Open)
                {
                    onDoorEntered.Invoke();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            LockedWarningUi?.Hide();
            if (other == PlayerCollider)
            {
                if (_playerCanTransition && IsBetweenInsideAndOutside)
                {
                    HandlePlayerCrossing();
                }
                _playerCanTransition = false;
                _insideEventInvokedForPass = false;

                if (_doorState == DoorState.Opening)
                {
                    _closeWhenOpen = true;
                }
                else if (_doorState == DoorState.Open)
                {
                    StartDoorRoutine(CloseDoor());
                }
            }
        }

        private void HandlePlayerCrossing()
        {
            int exitSide = GetPlayerDoorSide();
            bool crossedDoor = _playerEntrySide != exitSide;
            if (crossedDoor)
            {
                if (IsExteriorSide(exitSide))
                {
                    goingOutside?.Invoke();
                }
                else if (!_insideEventInvokedForPass)
                {
                    goingInside?.Invoke();
                }
            }
            else if (_insideEventInvokedForPass)
            {
                goingOutside?.Invoke();
            }
        }

        private bool IsExteriorSide(int side)
        {
            return (side > 0) != InsideIsPositiveSide;
        }

        private int GetPlayerDoorSide()
        {
            Vector3 doorwayNormal = transform.TransformDirection(_doorwayLocalAxis).normalized;
            Vector3 doorwayCenter = DoorCollider.transform.TransformPoint(DoorCollider.center);
            return Vector3.Dot(PlayerCollider.bounds.center - doorwayCenter, doorwayNormal) >= 0f
                ? 1
                : -1;
        }

        private void StartDoorRoutine(IEnumerator routine)
        {
            StopDoorRoutine();
            _doorCoroutine = StartCoroutine(routine);
        }

        private void StopDoorRoutine()
        {
            if (_doorCoroutine != null)
            {
                StopCoroutine(_doorCoroutine);
                _doorCoroutine = null;
            }
        }

        private IEnumerator ApproachDoor()
        {
            _doorState = DoorState.Opening;
            AudioSource.PlayOneShot(DoorOpenSfx);
            onDoorApproached.Invoke();
            // lerp door transform open
            float elapsed = 0f;
            Quaternion startRotation = _doorVisualTransform.localRotation;
            while (elapsed < OpenSpeed)
            {
                _doorVisualTransform.localRotation = Quaternion.Lerp(
                    startRotation,
                    DoorTransformOpen.localRotation,
                    elapsed / OpenSpeed
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            // after lerp, disable NavMeshObstacle
            DoorObstacle.enabled = false;
            _doorState = DoorState.Open;

            if (AutoCloseBehindPlayer)
            {
                while (DoorCollider.bounds.Intersects(PlayerCollider.bounds))
                {
                    yield return null;
                }
            }

            if (_closeWhenOpen)
            {
                _closeWhenOpen = false;
                StartDoorRoutine(CloseDoor());
            }
        }

        private IEnumerator CloseDoor()
        {
            _doorState = DoorState.Closing;

            // lerp door transform closed
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                // add a short delay to prevent door camera clipping
                elapsed += Time.deltaTime;
                yield return null;
            }
            elapsed = 0f;
            AudioSource.PlayOneShot(DoorCloseSfx);
            Quaternion startRotation = _doorVisualTransform.localRotation;
            while (elapsed < OpenSpeed)
            {
                _doorVisualTransform.localRotation = Quaternion.Lerp(
                    startRotation,
                    DoorTransformClosed.localRotation,
                    elapsed / OpenSpeed
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            // after lerp enable NavMeshObstacle
            DoorObstacle.enabled = true;
            _doorState = DoorState.Closed;
            _doorCoroutine = null;
        }
    }
}
