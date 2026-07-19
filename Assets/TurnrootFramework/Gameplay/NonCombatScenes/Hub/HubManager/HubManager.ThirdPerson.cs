using NaughtyAttributes;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.AI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        [Foldout("Explore/Movement")]
        public GameObject[] ObjectsToEnableInWalkMode;

        [Foldout("Explore/Movement")]
        [Tooltip(
            "Persistent scene transform that drives traversal movement. The spawned avatar visual "
                + "model is re-parented onto this transform when bound. NavMeshAgent must be on this "
                + "same transform."
        )]
        public Transform MovementRig;

        [Tooltip(
            "NavMeshAgent on MovementRig — the sole movement driver. Handles Y via navmesh projection."
        )]
        [Foldout("Explore/Movement")]
        public NavMeshAgent NavMeshAgent;

        [Foldout("Explore/Movement")]
        private bool ConsumeHubInput = true;

        [Foldout("Explore/Movement")]
        public float MoveSpeed = 3f;

        [Foldout("Explore/Movement")]
        public float RunSpeed = 5f;

        [Foldout("Explore/Movement")]
        private bool isRunning = false;

        [Foldout("Explore/Movement")]
        public float RotationLerp = 12f;

        [Foldout("Explore/Movement")]
        public float WalkingInputThreshold = 0.05f;

        [Foldout("Explore/Movement")]
        [InfoBox(
            "Movement direction is derived from this transform's forward/right, "
                + "and right-stick/mouse input rotates its yaw. Cinemachine's vcam should track this "
                + "transform (e.g. as its Follow target) so the camera turns when this rotates."
        )]
        public Transform CameraYawRoot;

        [InfoBox("If true, right-stick/mouse X input rotates CameraYawRoot's yaw.")]
        [Foldout("Explore/Movement")]
        public bool ApplyLookYaw = true;

        [Foldout("Explore/Movement")]
        public float LookYawSpeed = 120f;
        private bool ApplyLookPitch = true;

        [Foldout("Explore/Movement")]
        public float LookPitchSpeed = 120f;

        private bool _walkMode;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _isWalking;
        private Transform _avatarRoot;
        private Animator _avatarAnimator;
        private bool _loggedOffNavMesh;
        private bool _cachedWalkReadiness;
        private bool _hasCachedWalkReadiness = false;

        public void BindAvatar(GameObject avatarModel)
        {
            if (avatarModel == null)
            {
                _avatarRoot = null;
                _avatarAnimator = null;
                return;
            }

            _avatarRoot = avatarModel.transform;
            _avatarAnimator = avatarModel.GetComponentInChildren<Animator>();

            float yaw = _avatarRoot.eulerAngles.y;

            if (MovementRig != null)
            {
                // Force the rig to be perfectly upright (pure yaw) regardless of how the avatar
                // spawn point / model is authored. This guarantees the rig's local Y axis always
                // matches world Y, so horizontal input maps to X/Z only — Y is left entirely to
                // the NavMeshAgent's surface projection.
                Quaternion modelWorldRotation = _avatarRoot.rotation;

                MovementRig.rotation = Quaternion.Euler(0f, yaw, 0f);

                if (NavMeshAgent != null)
                {
                    // Warp keeps the agent correctly placed on the navmesh rather than
                    // teleporting it to a position that may be off-mesh.
                    NavMeshAgent.Warp(_avatarRoot.position);
                }
                else
                {
                    MovementRig.position = _avatarRoot.position;
                }

                _avatarRoot.SetParent(MovementRig, worldPositionStays: false);
                _avatarRoot.localPosition = Vector3.zero;

                // Preserve the model's original visual orientation (including any authored
                // pitch/roll) — the difference between that and the now-upright rig becomes
                // the model's local rotation.
                _avatarRoot.rotation = modelWorldRotation;
            }

            if (CameraYawRoot != null)
            {
                Transform reference = MovementRig != null ? MovementRig : _avatarRoot;
                CameraYawRoot.position = reference.position;
                CameraYawRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        public void ClearAvatarBindingIfMatches(GameObject avatarModel)
        {
            if (avatarModel == null)
            {
                return;
            }

            if (_avatarRoot == avatarModel.transform)
            {
                _avatarRoot = null;
                _avatarAnimator = null;
            }
        }

        public void SetWalkMode(bool enabled)
        {
            if (_walkMode == enabled)
            {
                return;
            }

            _walkMode = enabled;

            if (enabled)
            {
                var readiness = ValidateWalkReadiness();
                if (!readiness.Success)
                {
                    $"HubThirdPersonAdapter: Cannot enter walk mode. {readiness.ErrorMessage}".LogError();
                    _walkMode = false;
                    return;
                }
            }

            if (ObjectsToEnableInWalkMode != null)
            {
                foreach (var obj in ObjectsToEnableInWalkMode)
                {
                    if (obj != null)
                    {
                        obj.SetActive(enabled);
                    }
                }
            }

            if (!enabled)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                SetWalkingState(false);

                if (NavMeshAgent != null)
                {
                    NavMeshAgent.velocity = Vector3.zero;
                }
            }
        }

        public void SetInput(Vector2 moveInput, Vector2 lookInput)
        {
            _moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            _lookInput = Vector2.ClampMagnitude(lookInput, 1f);
        }

        public void SetRunning(bool running) => isRunning = running;

        public void ApplyLookOnly(Vector2 lookInput)
        {
            if (CameraYawRoot == null)
            {
                return;
            }

            ApplyLook(Vector2.ClampMagnitude(lookInput, 1f));
        }
    }
}
