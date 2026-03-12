using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Keeps the GameObject a fixed distance in front of a camera while optionally
    /// preserving its original rotation.  If the object is larger than the camera
    /// frustum at that distance this script will nudge it along the camera's
    /// right/up axes so the rectangle defined by <see cref="Extents"/>
    /// stays inside the viewport.
    ///
    /// Attach to a particle-system root (or any emitter) and set the extents to
    /// half of the width/height of the emitter area in world units.  You can also
    /// leave <see cref="TargetCamera"/> null to use <c>Camera.main</c>.
    /// </summary>
    [ExecuteAlways]
    public class StayInFrontOfCamera : MonoBehaviour
    {
        [Tooltip("Camera that the object should stay in front of.  If null, Camera.main is used.")]
        public Camera TargetCamera;

        [Tooltip(
            "Distance from the camera along its forward vector where the object will be kept."
        )]
        public float Distance = 2f;

        [Tooltip(
            "Half-size of the emitter rectangle in the object's local X (width) and Z (height) axes. "
                + "Will be overwritten if AutoComputeExtents is enabled and a rectangle emitter is found."
        )]
        public Vector2 Extents = new Vector2(5f, 5f);

        [Tooltip(
            "When true the script will look for a ParticleSystem with a rectangle shape and calculate \"Extents\" automatically."
        )]
        public bool AutoComputeExtents = true;

        [Tooltip(
            "When true the transform's rotation is frozen to whatever it was when the script awoke.  "
                + "When false the object will copy the camera's rotation."
        )]
        public bool PreserveRotation = true;

        private Quaternion _initialRotation;
        private Vector3[] _localCorners;

        private CameraTransformNotifier _notifier;

        void Awake()
        {
            if (TargetCamera == null)
            {
                TargetCamera = Camera.main;
            }

            _initialRotation = transform.rotation;
            if (AutoComputeExtents)
                TryComputeExtentsFromEmitter();
            RecalculateCorners();
        }

        void OnValidate()
        {
            if (AutoComputeExtents)
                TryComputeExtentsFromEmitter();
            // if inspector changes the extents, update the corner array immediately for editor preview
            RecalculateCorners();
        }

        void OnEnable()
        {
            if (TargetCamera == null)
            {
                TargetCamera = Camera.main;
            }

            if (TargetCamera != null)
            {
                _notifier = TargetCamera.GetComponent<CameraTransformNotifier>();
                if (_notifier == null)
                {
                    _notifier = TargetCamera.gameObject.AddComponent<CameraTransformNotifier>();
                }

                _notifier.TransformChanged += OnCameraTransformChanged;
                UpdatePosition();
            }
        }

        void OnDisable()
        {
            if (_notifier != null)
            {
                _notifier.TransformChanged -= OnCameraTransformChanged;
            }
        }

        private void RecalculateCorners()
        {
            _localCorners = new Vector3[4]
            {
                new Vector3(-Extents.x, 0f, -Extents.y),
                new Vector3(Extents.x, 0f, -Extents.y),
                new Vector3(-Extents.x, 0f, Extents.y),
                new Vector3(Extents.x, 0f, Extents.y),
            };
        }

        private void TryComputeExtentsFromEmitter()
        {
            // look for a particle system on this object or any child
            ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
            if (ps == null)
                return;

            var shape = ps.shape;
            if (shape.shapeType == ParticleSystemShapeType.Rectangle)
            {
                // shape.scale gives the full size in the module's local space
                Vector3 s = shape.scale;
                // convert to world units by applying the transform's lossy scale
                Vector3 worldScale = ps.transform.lossyScale;
                float width = s.x * worldScale.x;
                float height = s.y * worldScale.y;
                Extents = new Vector2(width * 0.5f, height * 0.5f);
            }
        }

        private void OnCameraTransformChanged(Transform cam)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (TargetCamera == null)
            {
                return;
            }

            Vector3 basePos =
                TargetCamera.transform.position + TargetCamera.transform.forward * Distance;
            Quaternion rot = PreserveRotation ? _initialRotation : TargetCamera.transform.rotation;

            Vector3 offset = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                Vector3 worldCorner = basePos + rot * _localCorners[i] + offset;
                Vector3 vp = TargetCamera.WorldToViewportPoint(worldCorner);

                if (vp.z < 0f)
                {
                    continue;
                }

                if (vp.x < 0f)
                {
                    offset += TargetCamera.transform.right * (vp.x * Distance);
                }

                if (vp.x > 1f)
                {
                    offset -= TargetCamera.transform.right * ((vp.x - 1f) * Distance);
                }

                if (vp.y < 0f)
                {
                    offset += TargetCamera.transform.up * (vp.y * Distance);
                }

                if (vp.y > 1f)
                {
                    offset -= TargetCamera.transform.up * ((vp.y - 1f) * Distance);
                }
            }

            transform.position = basePos + offset;
            transform.rotation = rot;
        }
    }
}
