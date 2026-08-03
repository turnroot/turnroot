using UnityEngine;

namespace Turnroot.Utilities
{
    [System.Serializable]
    public enum SpinAxis
    {
        X,
        Y,
        Z,
    }

    public class SpinTransform : MonoBehaviour
    {
        private Vector3 spinAxis;
        public SpinAxis axis = SpinAxis.Y;
        public float spinSpeed = 10f;

        private void Awake()
        {
            switch (axis)
            {
                case SpinAxis.X:
                    spinAxis = Vector3.right;
                    break;
                case SpinAxis.Y:
                    spinAxis = Vector3.up;
                    break;
                case SpinAxis.Z:
                    spinAxis = Vector3.forward;
                    break;
            }
        }

        private void Update() => transform.Rotate(spinAxis, spinSpeed * UnityEngine.Time.deltaTime);
    }
}
