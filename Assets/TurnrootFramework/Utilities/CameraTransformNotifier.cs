using System;
using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Monitors a camera's transform and fires an event when its position or rotation
    /// changes.  Useful for other components that only need to update when the
    /// camera moves instead of every frame.
    ///
    /// Add this script to the camera you want to track.  It automatically checks for
    /// changes in <see cref="LateUpdate"/> and invokes <see cref="TransformChanged"/>
    /// if either property differs from the previous frame.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class CameraTransformNotifier : MonoBehaviour
    {
        /// <summary>
        /// Raised when the camera's transform has been modified.  The argument is the
        /// camera's transform.
        /// </summary>
        public event Action<Transform> TransformChanged;

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        void Awake()
        {
            // initialize the last values to avoid firing on the first frame
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        void LateUpdate()
        {
            if (transform.position != _lastPosition || transform.rotation != _lastRotation)
            {
                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                TransformChanged?.Invoke(transform);
            }
        }
    }
}
