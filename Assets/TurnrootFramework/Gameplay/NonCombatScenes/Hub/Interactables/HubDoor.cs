using System.Collections;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Weather;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class HubDoor : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onDoorApproached;

        [SerializeField]
        private UnityEvent onDoorEntered;

        [SerializeField]
        private UnityEvent goingInside;

        [SerializeField]
        private UnityEvent goingOutside;
        private bool _open = false;

        private bool _goingInside = false;

        [SerializeField]
        public bool Locked;

        [SerializeField]
        public bool AutoCloseBehindPlayer;
        private NavMeshObstacle DoorObstacle;

        [SerializeField]
        private Collider PlayerCollider;
        private BoxCollider DoorCollider;

        [SerializeField]
        private Transform DoorTransformOpen;

        [SerializeField]
        private Transform DoorTransformClosed;
        private IEnumerator _doorCoroutine;

        [SerializeField]
        private float OpenSpeed = 1f;

        [SerializeField]
        private AudioClip DoorOpenSfx;

        [SerializeField]
        private AudioClip DoorCloseSfx;

        [SerializeField]
        private AudioSource AudioSource;

        [SerializeField]
        private UIFade LockedWarningUi;

        private void Awake()
        {
            DoorCollider = GetComponent<BoxCollider>();
            DoorObstacle = GetComponent<NavMeshObstacle>();
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
                if (!_open)
                {
                    _doorCoroutine = ApproachDoor();
                    StartCoroutine(_doorCoroutine);
                }
                else
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
                if (_doorCoroutine != null)
                {
                    StopCoroutine(_doorCoroutine);
                }
                if (_open)
                {
                    _doorCoroutine = CloseDoor();
                    StartCoroutine(_doorCoroutine);
                }
            }
        }

        private IEnumerator ApproachDoor()
        {
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
            _open = true;
            // once player collider is no longer colliding with door collider lerp closed if AutoCloseBehindPlayer is true
            if (AutoCloseBehindPlayer)
            {
                _goingInside = !_goingInside;
                if (_goingInside)
                {
                    goingInside?.Invoke();
                }

                while (DoorCollider.bounds.Intersects(PlayerCollider.bounds))
                {
                    yield return null;
                }
                _doorCoroutine = CloseDoor();
                StartCoroutine(_doorCoroutine);
            }
        }

        private IEnumerator CloseDoor()
        {
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
            _open = false;
            if (!_goingInside)
            {
                goingOutside?.Invoke();
            }
        }
    }
}
