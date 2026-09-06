using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        private void HandleWalk()
        {
            if (!_walkMode || !ConsumeHubInput)
            {
                return;
            }

            var readiness = _cachedWalkReadiness && _hasCachedWalkReadiness;
            if (!readiness)
            {
                _cachedWalkReadiness = ValidateWalkReadiness().Success;
                _hasCachedWalkReadiness = true;
            }
            if (!_cachedWalkReadiness)
            {
                $"HubThirdPersonAdapter: Disabling walk mode.".LogError();
                SetWalkMode(false);
                return;
            }

            if (_avatarAnimator == null)
            {
                TryResolveAnimator();
            }

            CameraYawRoot.position = MovementRig.position;

            ApplyLook(_lookInput);
            ApplyMovement(_moveInput);

            SetWalkingState(
                _moveInput.sqrMagnitude >= (WalkingInputThreshold * WalkingInputThreshold)
            );
        }

        private void ApplyMovement(Vector2 moveInput)
        {
            if (_isTraversalMovementLocked)
            {
                SetWalkingState(false);
                return;
            }

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

            Vector3 desiredDirection = (forward * moveInput.y) + (right * moveInput.x);
            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            if (isRunning)
            {
                NavMeshAgent.Move(
                    ApplyGameSpeedScaleFloat(RunSpeed)
                        * GameplayPlayerSettings.Instance.ExploreMovementSpeed
                        * Time.deltaTime
                        * desiredDirection
                );
            }
            else
            {
                NavMeshAgent.Move(
                    ApplyGameSpeedScaleFloat(MoveSpeed)
                        * GameplayPlayerSettings.Instance.ExploreMovementSpeed
                        * Time.deltaTime
                        * desiredDirection
                );
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

        private void ApplyLook(Vector2 lookInput)
        {
            var threshold =
                0.01f + (0.1f - (GameplayPlayerSettings.Instance.ExploreMouseSensitivity * 0.1f));
            float sensitivity = GameplayPlayerSettings.Instance.ExploreMouseSensitivity;

            bool hasYawInput = ApplyLookYaw && Mathf.Abs(lookInput.x) >= threshold;
            bool hasPitchInput = ApplyLookPitch && Mathf.Abs(lookInput.y) >= threshold;
            if (!hasYawInput && !hasPitchInput)
            {
                return;
            }

            if (hasYawInput)
            {
                float yawRotateAmount = lookInput.x * LookYawSpeed * Time.deltaTime;
                yawRotateAmount *= sensitivity;
                if (GameplayPlayerSettings.Instance.InvertExploreMouse)
                {
                    yawRotateAmount *= -1f;
                }

                CameraYawRoot.Rotate(0f, yawRotateAmount, 0f, Space.World);
            }

            if (hasPitchInput)
            {
                _pitchOffset -= lookInput.y * LookPitchSpeed * Time.deltaTime * sensitivity;
                _pitchOffset = Mathf.Clamp(
                    _pitchOffset,
                    -Mathf.Abs(MaxTiltUp),
                    Mathf.Abs(MaxTiltDown)
                );

                Vector3 euler = CameraYawRoot.localEulerAngles;
                CameraYawRoot.localRotation = Quaternion.Euler(_pitchOffset, euler.y, 0f);
            }
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

            var appearanceBrain = _brain?.unitAppearanceBrain;
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
