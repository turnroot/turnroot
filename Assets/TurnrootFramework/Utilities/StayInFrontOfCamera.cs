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

        // Local-space offset of the emitter shape (from the ParticleSystem shape module).
        // This lets the script keep the actual emitter area centered in front of the camera,
        // even if the particle shape is positioned away from the GameObject origin.
        private Vector3 _shapeLocalOffset = Vector3.zero;

        private CameraTransformNotifier _notifier;

        void Awake()
        {
            if (TargetCamera == null)
            {
                TargetCamera = Camera.main;
            }

            _initialRotation = transform.rotation;
            if (AutoComputeExtents)
            {
                TryComputeExtentsFromEmitter();
            }

            RecalculateCorners();
        }

        void OnValidate()
        {
            if (AutoComputeExtents)
            {
                TryComputeExtentsFromEmitter();
            }
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
            // We treat Extents as half-size in the local X/Y plane (width/height).
            // This matches Unity's rectangle emitter axes and is more intuitive.
            _localCorners = new Vector3[4]
            {
                new Vector3(-Extents.x, -Extents.y, 0f),
                new Vector3(Extents.x, -Extents.y, 0f),
                new Vector3(-Extents.x, Extents.y, 0f),
                new Vector3(Extents.x, Extents.y, 0f),
            };
        }

        private void TryComputeExtentsFromEmitter()
        {
            // look for a particle system on this object or any child
            ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
            if (ps == null)
            {
                _shapeLocalOffset = Vector3.zero;
                return;
            }

            var shape = ps.shape;

            // Remember the particle-shape offset so emitter bounds are computed correctly even
            // when the shape is not centered on the root GameObject.
            _shapeLocalOffset = shape.position;

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

            // Place the transform such that the emitter's local-center (shape offset) is
            // at the desired location in front of the camera.
            Vector3 transformBase = basePos - rot * _shapeLocalOffset;

            Vector3 offset = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                Vector3 worldCorner = transformBase + rot * _localCorners[i] + offset;
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

            transform.position = transformBase + offset;
            transform.rotation = rot;
        }
    }
}
