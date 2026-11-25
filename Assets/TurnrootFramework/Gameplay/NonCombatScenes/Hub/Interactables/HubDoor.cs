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
        public bool IsBetweenInsideAndOutside = true;
        private NavMeshObstacle DoorObstacle;

        public Collider PlayerCollider;
        private BoxCollider DoorCollider;

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
        private static bool _playerIsInside;
        private int _playerEntrySide;
        private bool _insideEventInvokedForPass;
        private Vector3 _doorwayCenter;
        private Vector3 _doorwayNormal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlayerLocation()
        {
            _playerIsInside = false;
        }

        private void Awake()
        {
            DoorCollider = GetComponent<BoxCollider>();
            DoorObstacle = GetComponent<NavMeshObstacle>();
            _doorwayCenter = DoorCollider.bounds.center;
            _doorwayNormal = transform.forward;
        }

        private void OnDisable()
        {
            StopDoorRoutine();
            _doorState = DoorState.Closed;
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
                _playerEntrySide = GetPlayerDoorSide();
                _insideEventInvokedForPass = false;
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
                if (IsBetweenInsideAndOutside)
                {
                    HandlePlayerCrossing();
                }

                if (_doorState == DoorState.Opening || _doorState == DoorState.Open)
                {
                    StartDoorRoutine(CloseDoor());
                }
            }
        }

        private int GetPlayerDoorSide()
        {
            return Vector3.Dot(
                PlayerCollider.bounds.center - _doorwayCenter,
                _doorwayNormal
            ) >= 0f ? 1 : -1;
        }

        private void HandlePlayerCrossing()
        {
            bool crossedDoor = _playerEntrySide != GetPlayerDoorSide();
            if (crossedDoor)
            {
                if (_playerIsInside)
                {
                    _playerIsInside = false;
                    goingOutside?.Invoke();
                }
                else
                {
                    if (!_insideEventInvokedForPass)
                    {
                        goingInside?.Invoke();
                    }
                    _playerIsInside = true;
                }

                _insideEventInvokedForPass = false;
            }
            else if (_insideEventInvokedForPass && !_playerIsInside)
            {
                goingOutside?.Invoke();
                _insideEventInvokedForPass = false;
            }
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
            while (elapsed < OpenSpeed)
            {
                gameObject.transform.localRotation = Quaternion.Lerp(
                    DoorTransformClosed.localRotation,
                    DoorTransformOpen.localRotation,
                    elapsed / OpenSpeed
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            // after lerp, disable NavMeshObstacle
            DoorObstacle.enabled = false;
            _doorState = DoorState.Open;

            if (IsBetweenInsideAndOutside && !_playerIsInside)
            {
                goingInside?.Invoke();
                _insideEventInvokedForPass = true;
            }

            // once player collider is no longer colliding with door collider lerp closed if AutoCloseBehindPlayer is true
            if (AutoCloseBehindPlayer)
            {
                while (DoorCollider.bounds.Intersects(PlayerCollider.bounds))
                {
                    yield return null;
                }

                if (_doorState == DoorState.Open)
                {
                    StartDoorRoutine(CloseDoor());
                }
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
            while (elapsed < OpenSpeed)
            {
                gameObject.transform.localRotation = Quaternion.Lerp(
                    DoorTransformOpen.localRotation,
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
