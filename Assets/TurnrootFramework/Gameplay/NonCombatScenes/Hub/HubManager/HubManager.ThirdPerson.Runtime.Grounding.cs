using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Graphics3D;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        #region Grounding and Surface Detection
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

            ConsiderGroundColorMatch(
                actual,
                grassColor,
                GroundSurface.Grass,
                ref closestSurface,
                ref closestDistance
            );
            ConsiderGroundColorMatch(
                actual,
                sandColor,
                GroundSurface.Sand,
                ref closestSurface,
                ref closestDistance
            );
            ConsiderGroundColorMatch(
                actual,
                rockColor,
                GroundSurface.Rock,
                ref closestSurface,
                ref closestDistance
            );
            ConsiderGroundColorMatch(
                actual,
                snowColor,
                GroundSurface.Snow,
                ref closestSurface,
                ref closestDistance
            );

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
            Gizmos.color =
                !_hasGroundSample ? Color.magenta
                : isOnGrass ? Color.green
                : isOnSand ? Color.yellow
                : isOnRock ? Color.gray
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

        #endregion
    }
}