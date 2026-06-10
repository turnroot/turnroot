using UnityEngine;
using UnityEngine.AI;
using BrainType = Turnroot.Gameplay.Brain.Brain;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    /// <summary>
    /// Lightweight bridge for hub third-person walk mode that stays independent from any external controller package.
    /// </summary>
    public class HubThirdPersonAdapter : MonoBehaviour
    {
        [Header("Mode Toggle")]
        public Behaviour[] ComponentsToEnableInWalkMode;
        public GameObject[] ObjectsToEnableInWalkMode;

        [Header("Input Drive")]
        public bool ConsumeHubInput = true;
        public Transform AvatarRoot;
        public CharacterController CharacterController;
        public NavMeshAgent NavMeshAgent;
        public Transform CameraReference;
        public float MoveSpeed = 3f;
        public float RotationLerp = 12f;

        [Header("Animation (Reuse UnitAppearanceBrain)")]
        public Animator AvatarAnimator;
        public float WalkingInputThreshold = 0.05f;

        [Header("Optional Camera Yaw")]
        public bool ApplyLookYaw;
        public Transform CameraYawRoot;
        public float LookYawSpeed = 120f;

        private bool _walkMode;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _isWalking;
        private BrainType _brain;

        public void SetWalkMode(bool enabled)
        {
            if (_walkMode == enabled)
            {
                return;
            }

            _walkMode = enabled;

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

            TryResolveAnimator();

            ApplyMovement(_moveInput);
            ApplyLook(_lookInput);
            SetWalkingState(
                _moveInput.sqrMagnitude >= (WalkingInputThreshold * WalkingInputThreshold)
            );
        }

        private void Awake()
        {
            _brain = FindFirstObjectByType<BrainType>();
        }

        private void ApplyMovement(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Transform cameraRef =
                CameraReference != null ? CameraReference : Camera.main?.transform;
            Vector3 forward = cameraRef != null ? cameraRef.forward : Vector3.forward;
            Vector3 right = cameraRef != null ? cameraRef.right : Vector3.right;
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
            else if (AvatarRoot != null)
            {
                AvatarRoot.position += desiredDirection * (MoveSpeed * Time.deltaTime);
            }

            if (AvatarRoot != null && desiredDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDirection, Vector3.up);
                AvatarRoot.rotation = Quaternion.Slerp(
                    AvatarRoot.rotation,
                    targetRot,
                    RotationLerp * Time.deltaTime
                );
            }
        }

        private void ApplyLook(Vector2 lookInput)
        {
            if (!ApplyLookYaw || CameraYawRoot == null)
            {
                return;
            }

            if (Mathf.Abs(lookInput.x) < 0.01f)
            {
                return;
            }

            CameraYawRoot.Rotate(0f, lookInput.x * LookYawSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void SetWalkingState(bool walking)
        {
            if (_isWalking == walking)
            {
                return;
            }

            _isWalking = walking;

            TryResolveAnimator();

            if (AvatarAnimator == null)
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
                appearanceBrain.BlendToWalkAnimation(AvatarAnimator);
            }
            else
            {
                appearanceBrain.BlendToIdleAnimation(AvatarAnimator);
            }
        }

        private void TryResolveAnimator()
        {
            if (AvatarAnimator != null)
            {
                return;
            }

            if (AvatarRoot == null)
            {
                return;
            }

            AvatarAnimator = AvatarRoot.GetComponentInChildren<Animator>();
        }
    }
}
