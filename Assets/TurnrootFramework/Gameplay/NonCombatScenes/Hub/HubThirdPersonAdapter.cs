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

        [Header("Input Drive")]
        public bool ConsumeHubInput = true;
        public CharacterController CharacterController;
        public NavMeshAgent NavMeshAgent;
        public float MoveSpeed = 3f;
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
        private bool _loggedMissingMovementDriver;

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

            CharacterController ??= avatarModel.GetComponent<CharacterController>();
            NavMeshAgent ??= avatarModel.GetComponent<NavMeshAgent>();

            // Snap the yaw root to the avatar's position/yaw so Cinemachine starts framed behind
            // the avatar rather than wherever it was left from a previous traversal session.
            if (CameraYawRoot != null)
            {
                CameraYawRoot.position = _avatarRoot.position;
                CameraYawRoot.rotation = Quaternion.Euler(0f, _avatarRoot.eulerAngles.y, 0f);
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
            }

            if (NavMeshAgent != null && CharacterController != null && CharacterController.enabled)
            {
                NavMeshAgent.updatePosition = !enabled;
                NavMeshAgent.updateRotation = !enabled;
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

            TryResolveAnimator();

            // Keep the yaw root glued to the avatar's position so Cinemachine's framing
            // follows movement. Rotation is left alone here — that's player/Cinemachine territory.
            CameraYawRoot.position = _avatarRoot.position;

            // Apply look first so movement direction this frame is based on the
            // up-to-date yaw root orientation.
            ApplyLook(_lookInput);
            ApplyMovement(_moveInput);

            SetWalkingState(
                _moveInput.sqrMagnitude >= (WalkingInputThreshold * WalkingInputThreshold)
            );
        }

        private void Awake() => _brain = FindFirstObjectByType<BrainType>();

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

            if (CharacterController != null && CharacterController.enabled)
            {
                CharacterController.SimpleMove(desiredDirection * MoveSpeed);
            }
            else if (NavMeshAgent != null && NavMeshAgent.enabled)
            {
                NavMeshAgent.Move(desiredDirection * (MoveSpeed * Time.deltaTime));
            }
            else if (!_loggedMissingMovementDriver)
            {
                "HubThirdPersonAdapter: No enabled CharacterController/NavMeshAgent available for movement.".LogError();
                _loggedMissingMovementDriver = true;
            }

            if (_avatarRoot != null && desiredDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDirection, Vector3.up);
                _avatarRoot.rotation = Quaternion.Slerp(
                    _avatarRoot.rotation,
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

            bool hasMovementDriver =
                (CharacterController != null && CharacterController.enabled)
                || (NavMeshAgent != null && NavMeshAgent.enabled);
            if (!hasMovementDriver)
            {
                return OperationResult.Failure(
                    "Neither CharacterController nor NavMeshAgent is assigned and enabled."
                );
            }

            return OperationResult.Successful();
        }

        private void SetWalkingState(bool walking)
        {
            if (_isWalking == walking)
            {
                return;
            }

            _isWalking = walking;

            TryResolveAnimator();

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
    }
}