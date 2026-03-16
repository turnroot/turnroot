using System.Collections;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    public partial class CameraBrain : BrainComponent
    {
        private Coroutine _cameraTransitionCoroutine;

        public OperationResult StartCameraTransition(
            Camera camera,
            Transform target,
            float transitionDuration
        )
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(camera, nameof(camera)),
                OperationResultGuards.RequireNotNull(target, nameof(target))
            );
            if (!validation.Success)
            {
                return validation;
            }

            // If no duration is supplied, just snap.
            if (transitionDuration <= 0f)
            {
                return MoveCameraInstant(camera, target);
            }

            if (_cameraTransitionCoroutine != null)
            {
                StopCoroutine(_cameraTransitionCoroutine);
                _cameraTransitionCoroutine = null;
            }

            _cameraTransitionCoroutine = StartCoroutine(
                CameraTransitionCoroutine(camera, target, transitionDuration)
            );

            return OperationResult.Successful();
        }

        private IEnumerator CameraTransitionCoroutine(
            Camera camera,
            Transform target,
            float duration
        )
        {
            Vector3 startPos = camera.transform.position;
            Quaternion startRot = camera.transform.rotation;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                camera.transform.position = Vector3.Lerp(startPos, target.position, t);
                camera.transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
                yield return null;
            }

            camera.transform.position = target.position;
            camera.transform.rotation = target.rotation;
            _cameraTransitionCoroutine = null;
        }

        public OperationResult MoveCameraInstant(Camera camera, Transform target)
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(camera, nameof(camera)),
                OperationResultGuards.RequireNotNull(target, nameof(target))
            );
            if (!validation.Success)
            {
                return validation;
            }

            camera.transform.position = target.position;
            camera.transform.rotation = target.rotation;
            return OperationResult.Successful();
        }

        public IEnumerator SmoothLook(Camera cam, Vector3 targetRotation, float lookSmoothTime)
        {
            float elapsed = 0f;
            Vector3 start = cam.transform.localEulerAngles;
            start.x = NormalizeAngle(start.x);
            start.y = NormalizeAngle(start.y);
            start.z = 0f; // always zero roll

            while (elapsed < lookSmoothTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lookSmoothTime);
                // use DeltaAngle to find shortest rotation direction across the
                // 180 boundary (avoids wild spins when base+offset crosses +-180).
                float pitchDelta = Mathf.DeltaAngle(start.x, targetRotation.x);
                float yawDelta = Mathf.DeltaAngle(start.y, targetRotation.y);
                float pitch = start.x + pitchDelta * t;
                float yaw = start.y + yawDelta * t;
                cam.transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
                yield return null;
            }

            cam.transform.localEulerAngles = new Vector3(targetRotation.x, targetRotation.y, 0f);
        }

        public float NormalizeAngle(float a)
        {
            // convert 0..360 to -180..180 for easier clamping math
            if (a > 180f)
            {
                a -= 360f;
            }

            return a;
        }
    }
}
