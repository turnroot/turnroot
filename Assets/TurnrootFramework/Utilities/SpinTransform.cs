namespace Turnroot.Utilities
{
    public enum SpinAxis
    {
        X,
        Y,
        Z,
    }

    public class SpinTransform : UnityEngine.MonoBehaviour
    {
        private UnityEngine.Vector3 spinAxis;
        public SpinAxis axis = SpinAxis.Y;
        public float spinSpeed = 10f;

        private void Awake()
        {
            switch (axis)
            {
                case SpinAxis.X:
                    spinAxis = UnityEngine.Vector3.right;
                    break;
                case SpinAxis.Y:
                    spinAxis = UnityEngine.Vector3.up;
                    break;
                case SpinAxis.Z:
                    spinAxis = UnityEngine.Vector3.forward;
                    break;
            }
        }

        private void Update()
        {
            transform.Rotate(spinAxis, spinSpeed * UnityEngine.Time.deltaTime);
        }
    }
}
