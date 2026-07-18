using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Utilities
{
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class FootIKController : MonoBehaviour
    {
        #region Inspector - General

        [BoxGroup("General")]
        [InfoBox(
            "Requires a Humanoid Animator with 'IK Pass' enabled on the base layer "
                + "(Animator Controller > Layer gear icon > IK Pass). Bones are found "
                + "automatically via the Humanoid avatar - no manual wiring needed."
        )]
        [SerializeField]
        private bool enableFootIK = true;

        [BoxGroup("General")]
        [SerializeField, MinValue(0.01f)]
        [Tooltip("How quickly the whole system blends in/out when Enable Foot IK is toggled.")]
        private float masterWeightSmoothSpeed = 8f;

        [BoxGroup("General")]
        [SerializeField]
        [Tooltip("Colliders on these layers are treated as ground/steppable geometry.")]
        private LayerMask groundLayers = ~0;

        [BoxGroup("General")]
        [SerializeField]
        [Tooltip(
            "Optional. The transform whose Y position represents 'flat ground level' "
                + "for this character (usually the feet/root pivot). Defaults to this "
                + "GameObject's transform if left empty - correct for almost every setup "
                + "using a CharacterController or root motion."
        )]
        private Transform groundReferenceTransform;

        [BoxGroup("General")]
        [SerializeField]
        [Tooltip("Manual vertical correction if your root pivot isn't exactly at ground level.")]
        private float groundReferenceYOffset = 0f;

        #endregion

        #region Inspector - Raycast

        [BoxGroup("Raycast")]
        [SerializeField, MinValue(0f)]
        [Tooltip("How far above the animated foot position the ground probe starts.")]
        private float raycastHeightOffset = 0.5f;

        [BoxGroup("Raycast")]
        [SerializeField, MinValue(0.01f)]
        [Tooltip("How far below the start point the probe searches for ground.")]
        private float raycastDistance = 1.0f;

        [BoxGroup("Raycast")]
        [SerializeField]
        private bool useSphereCast = true;

        [BoxGroup("Raycast"), ShowIf(nameof(useSphereCast))]
        [SerializeField, MinValue(0.001f)]
        private float footCastRadius = 0.08f;

        #endregion

        #region Inspector - Feet

        [BoxGroup("Feet")]
        [SerializeField, Range(0f, 1f)]
        private float footPositionWeight = 1f;

        [BoxGroup("Feet")]
        [SerializeField, Range(0f, 1f)]
        private float footRotationWeight = 1f;

        [BoxGroup("Feet")]
        [SerializeField]
        [Tooltip(
            "Small vertical offset for sole thickness (raises the foot slightly off the raycast hit point)."
        )]
        private float footGroundOffset = 0.02f;

        [BoxGroup("Feet")]
        [SerializeField, MinValue(0.01f)]
        private float footPositionSpeed = 12f;

        [BoxGroup("Feet")]
        [SerializeField, MinValue(0.01f)]
        private float footRotationSpeed = 12f;

        [BoxGroup("Feet")]
        [SerializeField, Range(0f, 90f)]
        [Tooltip("Clamps how far a foot can tilt to match a steep ground normal.")]
        private float maxFootTiltAngle = 45f;

        [BoxGroup("Feet")]
        [SerializeField]
        [Tooltip(
            "If no ground is found under a foot (e.g. falling/jumping off a ledge), "
                + "fade that foot's IK weight to 0 instead of holding a stale position."
        )]
        private bool disableIfNoGroundHit = true;

        #endregion

        #region Inspector - Knees

        [BoxGroup("Knees")]
        [SerializeField]
        private bool solveKneeHints = true;

        [BoxGroup("Knees"), ShowIf(nameof(solveKneeHints))]
        [SerializeField, Range(0f, 1f)]
        private float kneeHintWeight = 1f;

        [BoxGroup("Knees"), ShowIf(nameof(solveKneeHints))]
        [SerializeField]
        [Tooltip(
            "Assumes a standard forward-bending human knee. Offset along the character's forward direction."
        )]
        private float kneeForwardOffset = 0.4f;

        [BoxGroup("Knees"), ShowIf(nameof(solveKneeHints))]
        [SerializeField]
        private float kneeUpOffset = 0.1f;

        #endregion

        #region Inspector - Hips

        [BoxGroup("Hips")]
        [SerializeField]
        [Tooltip(
            "Raises/lowers the pelvis so legs bend naturally instead of over-extending on stairs and slopes."
        )]
        private bool adjustHips = true;

        [BoxGroup("Hips"), ShowIf(nameof(adjustHips))]
        [SerializeField, MinValue(0.01f)]
        private float hipAdjustSpeed = 6f;

        [BoxGroup("Hips"), ShowIf(nameof(adjustHips))]
        [SerializeField, MinValue(0f)]
        private float maxHipLower = 0.35f;

        [BoxGroup("Hips"), ShowIf(nameof(adjustHips))]
        [SerializeField, MinValue(0f)]
        private float maxHipRaise = 0.12f;

        #endregion

        #region Inspector - Debug

        [BoxGroup("Debug")]
        [SerializeField]
        private bool showGizmos = false;

        #endregion

        private Animator animator;
        private FootIKData leftFoot;
        private FootIKData rightFoot;

        private float masterWeight;
        private float currentHipOffset;

        public bool EnableFootIK
        {
            get => enableFootIK;
            set => enableFootIK = value;
        }

        public bool IsLeftFootGrounded => leftFoot != null && leftFoot.hasHit;
        public bool IsRightFootGrounded => rightFoot != null && rightFoot.hasHit;

        private class FootIKData
        {
            public HumanBodyBones kneeBone;
            public AvatarIKGoal ikGoal;
            public AvatarIKHint ikHint;

            public bool initialized;
            public float currentWeight;
            public Vector3 currentPosition;
            public Quaternion currentRotation;
            public float groundOffsetY;
            public Vector3 rayOrigin;
            public Vector3 rayHitPoint;
            public Vector3 rayHitNormal;
            public bool hasHit;
            public Vector3 kneeHintPos;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();

            leftFoot = new FootIKData
            {
                kneeBone = HumanBodyBones.LeftLowerLeg,
                ikGoal = AvatarIKGoal.LeftFoot,
                ikHint = AvatarIKHint.LeftKnee,
            };

            rightFoot = new FootIKData
            {
                kneeBone = HumanBodyBones.RightLowerLeg,
                ikGoal = AvatarIKGoal.RightFoot,
                ikHint = AvatarIKHint.RightKnee,
            };
        }

        private void Start()
        {
            if (animator.avatar == null || !animator.avatar.isHuman)
            {
                $"[FootIKController] '{name}': Animator has no Humanoid avatar assigned. Foot IK requires a Humanoid rig (Auto Rig Pro's Unity export supports this). Disabling.".LogWarning();
                enabled = false;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null)
            {
                return;
            }

            float targetMasterWeight = enableFootIK ? 1f : 0f;
            masterWeight = Mathf.MoveTowards(
                masterWeight,
                targetMasterWeight,
                Time.deltaTime * masterWeightSmoothSpeed
            );

            if (masterWeight <= 0f)
            {
                ZeroOutIK(leftFoot);
                ZeroOutIK(rightFoot);
                return;
            }

            SolveFoot(leftFoot);
            SolveFoot(rightFoot);

            if (adjustHips)
            {
                SolveHips();
            }

            ApplyFoot(leftFoot);
            ApplyFoot(rightFoot);

            if (solveKneeHints)
            {
                SolveKnee(leftFoot);
                SolveKnee(rightFoot);
            }
            else
            {
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
                animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
            }
        }

        private float GetGroundReferenceY()
        {
            Transform reference =
                groundReferenceTransform != null ? groundReferenceTransform : transform;
            return reference.position.y + groundReferenceYOffset;
        }

        private void SolveFoot(FootIKData foot)
        {
            Vector3 animatedPos = animator.GetIKPosition(foot.ikGoal);
            Quaternion animatedRot = animator.GetIKRotation(foot.ikGoal);

            if (!foot.initialized)
            {
                foot.currentPosition = animatedPos;
                foot.currentRotation = animatedRot;
                foot.initialized = true;
            }

            float animatedHeightAboveRoot = animatedPos.y - GetGroundReferenceY();

            Vector3 origin = animatedPos + (Vector3.up * raycastHeightOffset);
            float castDistance = raycastHeightOffset + raycastDistance;

            bool didHit;
            RaycastHit hit;

            if (useSphereCast)
            {
                didHit = Physics.SphereCast(
                    origin,
                    footCastRadius,
                    Vector3.down,
                    out hit,
                    castDistance,
                    groundLayers,
                    QueryTriggerInteraction.Ignore
                );
            }
            else
            {
                didHit = Physics.Raycast(
                    origin,
                    Vector3.down,
                    out hit,
                    castDistance,
                    groundLayers,
                    QueryTriggerInteraction.Ignore
                );
            }

            foot.rayOrigin = origin;
            foot.hasHit = didHit;

            Vector3 targetPos = animatedPos;
            Quaternion targetRot = animatedRot;
            float targetWeight = disableIfNoGroundHit ? 0f : 1f;

            if (didHit)
            {
                foot.rayHitPoint = hit.point;
                foot.rayHitNormal = hit.normal;

                float targetY = hit.point.y + animatedHeightAboveRoot + footGroundOffset;
                targetPos = new Vector3(animatedPos.x, targetY, animatedPos.z);

                Vector3 clampedNormal = Vector3.RotateTowards(
                    Vector3.up,
                    hit.normal,
                    maxFootTiltAngle * Mathf.Deg2Rad,
                    0f
                );

                Vector3 animatedForward = animatedRot * Vector3.forward;
                Vector3 projectedForward = Vector3.ProjectOnPlane(animatedForward, clampedNormal);
                if (projectedForward.sqrMagnitude < 0.0001f)
                {
                    projectedForward = animatedForward;
                }

                targetRot = Quaternion.LookRotation(projectedForward.normalized, clampedNormal);

                foot.groundOffsetY = targetY - animatedPos.y;
                targetWeight = 1f;
            }
            else
            {
                foot.groundOffsetY = 0f;
            }

            foot.currentWeight = Mathf.MoveTowards(
                foot.currentWeight,
                targetWeight,
                Time.deltaTime * footPositionSpeed
            );
            foot.currentPosition = Vector3.Lerp(
                foot.currentPosition,
                targetPos,
                Time.deltaTime * footPositionSpeed
            );
            foot.currentRotation = Quaternion.Slerp(
                foot.currentRotation,
                targetRot,
                Time.deltaTime * footRotationSpeed
            );
        }

        private void SolveHips()
        {
            float rawOffset = Mathf.Min(leftFoot.groundOffsetY, rightFoot.groundOffsetY);
            float clampedOffset = Mathf.Clamp(rawOffset, -maxHipLower, maxHipRaise);

            currentHipOffset = Mathf.Lerp(
                currentHipOffset,
                clampedOffset,
                Time.deltaTime * hipAdjustSpeed
            );

            Vector3 bodyPos = animator.bodyPosition;
            bodyPos.y += currentHipOffset * masterWeight;
            animator.bodyPosition = bodyPos;
        }

        private void ApplyFoot(FootIKData foot)
        {
            float weight = masterWeight * foot.currentWeight;

            animator.SetIKPositionWeight(foot.ikGoal, footPositionWeight * weight);
            animator.SetIKRotationWeight(foot.ikGoal, footRotationWeight * weight);
            animator.SetIKPosition(foot.ikGoal, foot.currentPosition);
            animator.SetIKRotation(foot.ikGoal, foot.currentRotation);
        }

        private void SolveKnee(FootIKData foot)
        {
            Transform kneeTransform = animator.GetBoneTransform(foot.kneeBone);
            if (kneeTransform == null)
            {
                return;
            }

            Vector3 hintPos =
                kneeTransform.position
                + (transform.forward * kneeForwardOffset)
                + (transform.up * kneeUpOffset);

            foot.kneeHintPos = hintPos;

            float weight = kneeHintWeight * masterWeight * foot.currentWeight;
            animator.SetIKHintPositionWeight(foot.ikHint, weight);
            animator.SetIKHintPosition(foot.ikHint, hintPos);
        }

        private void ZeroOutIK(FootIKData foot)
        {
            animator.SetIKPositionWeight(foot.ikGoal, 0f);
            animator.SetIKRotationWeight(foot.ikGoal, 0f);
            animator.SetIKHintPositionWeight(foot.ikHint, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || leftFoot == null || rightFoot == null)
            {
                return;
            }

            DrawFootGizmo(leftFoot, Color.cyan);
            DrawFootGizmo(rightFoot, Color.magenta);
        }

        private void DrawFootGizmo(FootIKData foot, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(
                foot.rayOrigin,
                foot.rayOrigin + (Vector3.down * (raycastHeightOffset + raycastDistance))
            );

            if (foot.hasHit)
            {
                Gizmos.DrawSphere(foot.rayHitPoint, 0.04f);
                Gizmos.DrawLine(foot.rayHitPoint, foot.rayHitPoint + (foot.rayHitNormal * 0.3f));
            }

            if (solveKneeHints)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(foot.kneeHintPos, 0.03f);
            }
        }
    }
}
