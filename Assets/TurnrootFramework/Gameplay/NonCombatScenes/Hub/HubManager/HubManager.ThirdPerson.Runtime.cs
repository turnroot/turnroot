using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        private enum GroundSurface
        {
            Grass,
            Sand,
            Rock,
            Snow,
        }

        private Mesh _cachedGroundMesh;
        private int[] _cachedGroundTriangles;
        private Color[] _cachedGroundColors;
        private bool _hasGroundSample;
        private Color _lastGroundColor;

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
            UpdateGroundSurface();

            SetWalkingState(
                _moveInput.sqrMagnitude >= (WalkingInputThreshold * WalkingInputThreshold)
            );
        }

        private void UpdateGroundSurface()
        {
            isOnGrass = true;
            isOnSand = false;
            isOnRock = false;
            isOnSnow = false;
            _hasGroundSample = false;

            if (GroundMeshFilter == null || GroundMeshFilter.sharedMesh == null)
            {
                return;
            }

            var meshCollider = GroundMeshFilter.GetComponent<MeshCollider>();
            if (meshCollider == null || !meshCollider.enabled)
            {
                return;
            }

            Mesh mesh = GroundMeshFilter.sharedMesh;
            if (meshCollider.sharedMesh != mesh)
            {
                return;
            }

            if (_cachedGroundMesh != mesh)
            {
                _cachedGroundMesh = mesh;
                _cachedGroundTriangles = mesh.triangles;
                _cachedGroundColors = mesh.colors;
            }

            if (_cachedGroundColors == null || _cachedGroundColors.Length == 0)
            {
                return;
            }

            float agentHeight = NavMeshAgent != null ? NavMeshAgent.height : 0f;
            Vector3 rayOrigin = MovementRig.position + (Vector3.up * (agentHeight + 0.5f));
            float rayDistance = agentHeight + 1.5f;
            if (
                !meshCollider.Raycast(
                    new Ray(rayOrigin, Vector3.down),
                    out RaycastHit hit,
                    rayDistance
                )
            )
            {
                return;
            }

            int triangleStart = hit.triangleIndex * 3;
            if (
                hit.triangleIndex < 0
                || _cachedGroundTriangles == null
                || triangleStart + 2 >= _cachedGroundTriangles.Length
            )
            {
                return;
            }

            int vertexA = _cachedGroundTriangles[triangleStart];
            int vertexB = _cachedGroundTriangles[triangleStart + 1];
            int vertexC = _cachedGroundTriangles[triangleStart + 2];
            if (
                vertexA >= _cachedGroundColors.Length
                || vertexB >= _cachedGroundColors.Length
                || vertexC >= _cachedGroundColors.Length
            )
            {
                return;
            }

            Vector3 barycentric = hit.barycentricCoordinate;
            Color groundColor =
                (_cachedGroundColors[vertexA] * barycentric.x)
                + (_cachedGroundColors[vertexB] * barycentric.y)
                + (_cachedGroundColors[vertexC] * barycentric.z);

            _hasGroundSample = true;
            _lastGroundColor = groundColor;
            GroundSurface surface = FindClosestGroundSurface(groundColor);
            isOnGrass = surface == GroundSurface.Grass;
            isOnSand = surface == GroundSurface.Sand;
            isOnRock = surface == GroundSurface.Rock;
            isOnSnow = surface == GroundSurface.Snow;
        }

        private GroundSurface FindClosestGroundSurface(Color actual)
        {
            GroundSurface closestSurface = GroundSurface.Grass;
            float closestDistance = float.PositiveInfinity;

            ConsiderGroundColorMatch(actual, grassColor, GroundSurface.Grass, ref closestSurface, ref closestDistance);
            ConsiderGroundColorMatch(actual, sandColor, GroundSurface.Sand, ref closestSurface, ref closestDistance);
            ConsiderGroundColorMatch(actual, rockColor, GroundSurface.Rock, ref closestSurface, ref closestDistance);
            ConsiderGroundColorMatch(actual, snowColor, GroundSurface.Snow, ref closestSurface, ref closestDistance);

            return closestSurface;
        }

        private void ConsiderGroundColorMatch(
            Color actual,
            Color expected,
            GroundSurface surface,
            ref GroundSurface closestSurface,
            ref float closestDistance
        )
        {
            if (!ColorsMatch(actual, expected))
            {
                return;
            }

            float distance = ColorDistanceSquared(actual, expected);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSurface = surface;
            }
        }

        private static float ColorDistanceSquared(Color left, Color right)
        {
            float red = left.r - right.r;
            float green = left.g - right.g;
            float blue = left.b - right.b;
            return (red * red) + (green * green) + (blue * blue);
        }

        private bool ColorsMatch(Color actual, Color expected)
        {
            return Mathf.Abs(actual.r - expected.r) <= colorTolerance
                && Mathf.Abs(actual.g - expected.g) <= colorTolerance
                && Mathf.Abs(actual.b - expected.b) <= colorTolerance;
        }

        private void OnDrawGizmos()
        {
            if (NavMeshAgent == null)
            {
                return;
            }

            Transform agentTransform = MovementRig != null ? MovementRig : NavMeshAgent.transform;
            Gizmos.color = !_hasGroundSample
                ? Color.magenta
                : isOnGrass
                    ? Color.green
                    : isOnSand
                        ? Color.yellow
                        : isOnRock
                            ? Color.gray
                            : Color.white;

            Vector3 markerPosition =
                agentTransform.position
                + (Vector3.up * Mathf.Max(NavMeshAgent.height * 0.5f, 0.25f));
            float markerRadius = Mathf.Max(NavMeshAgent.radius * 0.25f, 0.1f);
            Gizmos.DrawSphere(markerPosition, markerRadius);

            if (_hasGroundSample)
            {
                Gizmos.color = _lastGroundColor;
                Gizmos.DrawCube(
                    markerPosition + (agentTransform.right * markerRadius * 2f),
                    Vector3.one * markerRadius
                );
            }
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

        private void PlayFootstepsIfDue(Vector3 moveDirection, bool isRunning)
        {
            _lastFootstepTime += Time.deltaTime;
            if (moveDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var settings = GameplayGeneralSettings.Instance;
            var movementSpeed = isRunning ? RunSpeed : MoveSpeed;
            var interval = settings.GetFootstepInterval(movementSpeed);
            if (_lastFootstepTime >= interval)
            {
                _lastFootstepTime = 0f;
                var footstepClip =
                    isOnGrass ? settings.HubGrassFootstep
                    : isOnSand ? settings.HubSandFootstep
                    : isOnSnow ? settings.HubSnowFootstep
                    : isOnRock ? settings.HubRockFootstep
                    : settings.HubGrassFootstep;
                _brain?.audioBrain?.PlayFootstepSound(FootstepAudioSource, footstepClip);
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
