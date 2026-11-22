using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Graphics3D;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        #region Footsteps and Movement State

        private void PlayFootstepsIfDue(Vector3 moveDirection, bool isRunning)
        {
            if (moveDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }
            _lastFootstepTime += Time.deltaTime;
            _footstepDecalTime += Time.deltaTime;
            var settings = GameplayGeneralSettings.Instance;
            var movementSpeed = isRunning ? RunSpeed : MoveSpeed;
            var soundInterval = settings.GetFootstepInterval(movementSpeed);
            var decalInterval = soundInterval * 0.5f;
            if (_footstepDecalTime >= decalInterval)
            {
                _footstepDecalTime -= decalInterval;
                HandleFootstep(moveDirection);
            }

            if (_lastFootstepTime >= soundInterval)
            {
                var footstepClip =
                    isOnGrass ? settings.HubGrassFootstep
                    : isOnSand ? settings.HubSandFootstep
                    : isOnSnow ? settings.HubSnowFootstep
                    : isOnRock ? settings.HubRockFootstep
                    : settings.HubGrassFootstep;
                _brain?.audioBrain?.PlayFootstepSound(FootstepAudioSource, footstepClip);
                _lastFootstepTime -= soundInterval;
            }
        }

        private void HandleFootstep(Vector3 moveDirection)
        {
            if ((isOnSand || isOnSnow) && UseFootstepDecals)
            {
                LeaveFootprintDecals(NavMeshAgent.transform, moveDirection);
            }
            else
            {
                if (_footstepDecalTimer > 60f || _footstepDecalIndex >= FootstepDecalPool.Length)
                {
                    ResetFootprintDecals();
                }
                else
                {
                    _footstepDecalTimer += Time.deltaTime;
                    // the timer is more of a failsafe since
                    // footsteps get rid of themselves on their own timers
                    // this just ensures that the pool is always available
                    // and there's no hanging
                }
            }
        }

        private OperationResult LeaveFootprintDecals(Transform agentTransform, Vector3 moveDirection)
        {
            if (FootstepDecalPool == null || FootstepDecalPool.Length == 0)
            {
                // no decals available
                $"No decals in pool available. Disable footstep decals or set things up correctly.".LogWarning();
                return OperationResult.Successful();
            }
            if (_avatarAnimator == null || !_avatarAnimator.isHuman)
            {
                if (!_loggedMissingFootstepBones)
                {
                    $"Footstep decals require a Humanoid avatar with foot bones; skipping decals.".LogWarning();
                    _loggedMissingFootstepBones = true;
                }
                return OperationResult.Successful();
            }

            var leftFoot = _avatarAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = _avatarAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (leftFoot == null || rightFoot == null)
            {
                if (!_loggedMissingFootstepBones)
                {
                    $"Footstep decals require both left and right foot bones; skipping decals.".LogWarning();
                    _loggedMissingFootstepBones = true;
                }
                return OperationResult.Successful();
            }

            if (_footstepDecalIndex >= FootstepDecalPool.Length)
            {
                ResetFootprintDecals();
            }
            var decal = FootstepDecalPool[_footstepDecalIndex];
            if (decal != null && !decal.activeSelf)
            {
                decal.SetActive(true);
                if (decal.TryGetComponent<FootstepDecal>(out var footstepDecal))
                {
                    var forwardProjection = Vector3.Dot(
                        leftFoot.position - rightFoot.position,
                        moveDirection.normalized
                    );
                    bool isLeftFoot =
                        Mathf.Abs(forwardProjection) < 0.001f
                            ? _footstepDecalLeft
                            : forwardProjection > 0f;
                    var steppingFoot = isLeftFoot ? leftFoot : rightFoot;

                    footstepDecal.PlaceSelf(
                        steppingFoot.position,
                        agentTransform.rotation,
                        isLeftFoot
                    );
                    _footstepDecalLeft = !isLeftFoot;
                    _footstepDecalIndex++;
                }
                else
                {
                    return OperationResult.Failure(
                        $"FootstepDecal component not found on decal GameObject: {decal.name}"
                    );
                }
            }
            return OperationResult.Successful();
        }

        private void ResetFootprintDecals()
        {
            _footstepDecalTimer = 0f;
            _footstepDecalIndex = 0;
            _footstepDecalLeft = FootStepDecalLeftFirst;

            if (FootstepDecalPool == null)
            {
                return;
            }

            foreach (var decal in FootstepDecalPool)
            {
                if (decal != null)
                {
                    decal.SetActive(false);
                }
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
        #endregion
    }
}