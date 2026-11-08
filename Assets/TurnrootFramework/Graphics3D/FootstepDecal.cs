using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Turnroot.Graphics3D
{
    public class FootstepDecal : MonoBehaviour
    {
        public float Lifetime = 15f;
        [HideInInspector]
        public DecalProjector DecalProjector => GetComponentInChildren<DecalProjector>();
        // it needs to be on the child because of the bonkers way
        // decal projects are sideways and must be rotated 90 degrees
        // to be useful. This would create double transforms (yucky)
        // if there wasn't a parent to clear the transforms
        private float _lifetimeTimer;
        private bool _isLeftFoot;

        private void OnEnable()
        {
            _lifetimeTimer = Lifetime;
        }

        private void Update()
        {
            _lifetimeTimer -= Time.deltaTime;
            if (_lifetimeTimer <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        public void PlaceSelf(
            Transform navMeshAgentTransform,
            bool isLeftFoot,
            float interspacing,
            Vector3 strideDirection,
            float foreAftOffset
        )
        {
            _isLeftFoot = isLeftFoot;
            transform.position = navMeshAgentTransform.position;
            transform.rotation = navMeshAgentTransform.rotation;
            if (strideDirection.sqrMagnitude > 0.0001f)
            {
                transform.position += strideDirection * foreAftOffset;
            }
            // if left foot, move to the left half interspacing, if right foot, move to the right half interspacing
            if (_isLeftFoot)
            {
                transform.position -= 0.5f * interspacing * transform.right;
            }
            else
            {
                transform.position += 0.5f * interspacing * transform.right;
            }
            transform.Translate(Vector3.up * 0.1f, Space.World);

            // there's a left foot and right foot on the decal material- square texture use Tiling/Offset on URPDecalProjector component
            // but it has to mirrored because decal projectors 
            // are backwards by default  (╯°□°)╯︵ ┻━┻
            // or not????
            DecalProjector.uvBias = new Vector2(_isLeftFoot ? 0f : 0f, 0.5f);
            DecalProjector.uvScale = new Vector2(0.5f, 1f);
        }
    }
}
