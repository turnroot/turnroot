using System.Collections;
using Turnroot.Utilities;
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
        [SerializeField]
        private UnityEvent onDoorApproached;

        [SerializeField]
        private UnityEvent onDoorEntered;
        private bool _open = false;

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
            $"HubDoor: OnTriggerEnter with {other.name}, open is {_open}, locked is {Locked}".LogInfo();
            if (Locked)
            {
                if (other == PlayerCollider)
                {
                    LockedWarningUi.Show();
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
            LockedWarningUi.Hide();
            $"HubDoor: OnTriggerExit with {other.name}, open is {_open}, locked is {Locked}".LogInfo();
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
            $"HubDoor: Approaching door".LogInfo();
            AudioSource.PlayOneShot(DoorOpenSfx);
            onDoorApproached.Invoke();
            // lerp door transform open
            float elapsed = 0f;
            while (elapsed < OpenSpeed)
            {
                DoorTransformOpen.localRotation = Quaternion.Lerp(
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
            AudioSource.PlayOneShot(DoorCloseSfx);
            // lerp door transform closed
            float elapsed = 0f;
            while (elapsed < OpenSpeed)
            {
                DoorTransformOpen.localRotation = Quaternion.Lerp(
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
        }
    }
}
