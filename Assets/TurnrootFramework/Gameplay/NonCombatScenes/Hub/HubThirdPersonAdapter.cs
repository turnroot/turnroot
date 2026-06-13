using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.AI;
using BrainType = Turnroot.Gameplay.Brain.Brain;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubThirdPersonAdapter : MonoBehaviour
    {
        [Header("Mode Toggle")]
        public Behaviour[] ComponentsToEnableInWalkMode;
        public GameObject[] ObjectsToEnableInWalkMode;

        [Header("Movement Rig")]
        [Tooltip(
            "Persistent scene transform that drives traversal movement. The spawned avatar visual "
                + "model is re-parented onto this transform when bound. NavMeshAgent must be on this "
                + "same transform."
        )]
        public Transform MovementRig;

        [Tooltip(
            "NavMeshAgent on MovementRig — the sole movement driver. Handles Y via navmesh projection."
        )]
        public NavMeshAgent NavMeshAgent;

        [Header("Input Drive")]
        public bool ConsumeHubInput = true;
        public float MoveSpeed = 3f;
        public float RunSpeed = 5f;
        private bool isRunning = false;
        public float RotationLerp = 12f;

        [Header("Animation (Reuse UnitAppearanceBrain)")]
        public float WalkingInputThreshold = 0.05f;

        [Header("Camera Yaw (Cinemachine target)")]
        [Tooltip(
            "Required for walk mode. Movement direction is derived from this transform's forward/right, "
                + "and right-stick/mouse input rotates its yaw. Cinemachine's vcam should track this "
                + "transform (e.g. as its Follow target) so the camera turns when this rotates."
        )]
        public Transform CameraYawRoot;

        [Tooltip("If true, right-stick/mouse X input rotates CameraYawRoot's yaw.")]
        public bool ApplyLookYaw = true;

        public float LookYawSpeed = 120f;

        private bool _walkMode;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _isWalking;
        private BrainType _brain;
        private Transform _avatarRoot;
        private Animator _avatarAnimator;
        private bool _loggedOffNavMesh;

        private void Awake()
        {
            _brain = FindFirstObjectByType<BrainType>();

            if (NavMeshAgent != null)
            {
                // We drive rotation ourselves (facing movement direction) and position via Move(),
                // so let the agent purely follow our calls without its own steering/rotation logic.
                NavMeshAgent.updateRotation = false;
                NavMeshAgent.updatePosition = true;
                NavMeshAgent.speed = MoveSpeed;
            }
        }

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

            if (ComponentsToEnableInWalkMode != null)
            {
                foreach (var component in ComponentsToEnableInWalkMode)
                {
                    if (component != null)
                    {
                        component.enabled = enabled;
                    }
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

        private void Update()
        {
            if (!_walkMode || !ConsumeHubInput)
            {
                return;
            }

            var readiness = ValidateWalkReadiness();
            if (!readiness.Success)
            {
                $"HubThirdPersonAdapter: Disabling walk mode. {readiness.ErrorMessage}".LogError();
                SetWalkMode(false);
                return;
            }

            if (_avatarAnimator == null)
            {
                TryResolveAnimator();
            }

            // Keep the yaw root glued to the rig's position so Cinemachine's framing
            // follows movement. Rotation is left alone here — that's player/Cinemachine territory.
            CameraYawRoot.position = MovementRig.position;

            // Apply look first so movement direction this frame is based on the
            // up-to-date yaw root orientation.
            ApplyLook(_lookInput);
            ApplyMovement(_moveInput);

            SetWalkingState(
                _moveInput.sqrMagnitude >= (WalkingInputThreshold * WalkingInputThreshold)
            );
        }

        private void ApplyMovement(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 forward = CameraYawRoot.forward;
            Vector3 right = CameraYawRoot.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredDirection = (forward * moveInput.y + right * moveInput.x);
            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            // NavMeshAgent.Move projects the requested motion onto the navmesh surface,
            // handling Y for us (slopes, steps within the agent's step height, etc.).
            if (isRunning)
            {
                NavMeshAgent.Move(desiredDirection * RunSpeed * Time.deltaTime);
            }
            else
            {
                NavMeshAgent.Move(desiredDirection * MoveSpeed * Time.deltaTime);
            }

            if (desiredDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDirection, Vector3.up);
                MovementRig.rotation = Quaternion.Slerp(
                    MovementRig.rotation,
                    targetRot,
                    RotationLerp * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// Rotates <see cref="CameraYawRoot"/>'s yaw based on right-stick/mouse X input.
        /// This is the only rotation this script applies in response to look input — the
        /// actual Camera transform is left untouched for Cinemachine to drive.
        /// </summary>
        private void ApplyLook(Vector2 lookInput)
        {
            if (!ApplyLookYaw || Mathf.Abs(lookInput.x) < 0.01f)
            {
                return;
            }

            CameraYawRoot.Rotate(0f, lookInput.x * LookYawSpeed * Time.deltaTime, 0f, Space.World);
        }

        private OperationResult ValidateWalkReadiness()
        {
            var rigValidation = OperationResultGuards.RequireNotNull(
                MovementRig,
                nameof(MovementRig)
            );
            if (!rigValidation.Success)
            {
                return rigValidation;
            }

            var avatarValidation = OperationResultGuards.RequireNotNull(
                _avatarRoot,
                "AvatarRootBinding"
            );
            if (!avatarValidation.Success)
            {
                return avatarValidation;
            }

            var yawRootValidation = OperationResultGuards.RequireNotNull(
                CameraYawRoot,
                nameof(CameraYawRoot)
            );
            if (!yawRootValidation.Success)
            {
                return yawRootValidation;
            }

            if (NavMeshAgent == null || !NavMeshAgent.enabled)
            {
                return OperationResult.Failure(
                    "NavMeshAgent is not assigned and enabled on MovementRig."
                );
            }

            if (!NavMeshAgent.isOnNavMesh)
            {
                if (!_loggedOffNavMesh)
                {
                    "HubThirdPersonAdapter: NavMeshAgent is not on a baked NavMesh at its current position.".LogWarning();
                    _loggedOffNavMesh = true;
                }

                return OperationResult.Failure("NavMeshAgent is not on a NavMesh.");
            }

            _loggedOffNavMesh = false;
            return OperationResult.Successful();
        }

        private void SetWalkingState(bool walking)
        {
            if (_isWalking == walking)
            {
                return;
            }

            _isWalking = walking;

            if (_avatarAnimator == null)
            {
                return;
            }

            var appearanceBrain = _brain != null ? _brain.unitAppearanceBrain : null;
            if (appearanceBrain == null)
            {
                return;
            }

            if (walking)
            {
                appearanceBrain.BlendToWalkAnimation(_avatarAnimator);
            }
            else
            {
                appearanceBrain.BlendToIdleAnimation(_avatarAnimator);
            }
        }

        private void TryResolveAnimator()
        {
            if (_avatarAnimator != null)
            {
                return;
            }

            if (_avatarRoot == null)
            {
                return;
            }

            _avatarAnimator = _avatarRoot.GetComponentInChildren<Animator>();
        }

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
