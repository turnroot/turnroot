using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Graphics3D;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        #region Movement and Look

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

            PlayFootstepsIfDue(desiredDirection, isRunning);

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

        private float _lastFootstepTime;
        private float _footstepDecalTime;

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

        #endregion
    }
}