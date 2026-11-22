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
        private bool _isInside;

        private void Awake()
        {
            DoorCollider = GetComponent<BoxCollider>();
            DoorObstacle = GetComponent<NavMeshObstacle>();
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
                if (_doorState == DoorState.Opening || _doorState == DoorState.Open)
                {
                    StartDoorRoutine(CloseDoor(commitOutside: _doorState == DoorState.Open));
                }
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

            if (!_isInside)
            {
                _isInside = true;
                goingInside?.Invoke();
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
                    StartDoorRoutine(CloseDoor(commitOutside: true));
                }
            }
        }

        private IEnumerator CloseDoor(bool commitOutside)
        {
            _doorState = DoorState.Closing;

            if (commitOutside && _isInside)
            {
                _isInside = false;
                goingOutside?.Invoke();
            }

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
