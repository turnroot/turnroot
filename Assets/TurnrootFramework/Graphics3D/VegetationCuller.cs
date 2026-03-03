using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Graphics3D
{
    public class VegetationCuller : MonoBehaviour
    {
        public UnityEngine.Camera Culler;

        [Tooltip("Padding around the viewport to prevent popping (default: 1)")]
        public float ViewportPadding = 1f;

        [Tooltip("How often to check visibility (in seconds)")]
        public float UpdateInterval = 0.1f;

        private Transform[] children;
        private float lastUpdateTime;

        void Start()
        {
            // Cache all grandchildren transforms
            List<Transform> grandchildrenList = new List<Transform>();

            // Iterate through all children
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                // Add all grandchildren from each child
                for (int j = 0; j < child.childCount; j++)
                {
                    grandchildrenList.Add(child.GetChild(j));
                }
            }

            children = grandchildrenList.ToArray();
            children.Length.ToString().LogInfo();
        }

        void Update()
        {
            if (Culler == null)
            {
                return;
            }

            // Only update at specified intervals for performance
            if (Time.time - lastUpdateTime < UpdateInterval)
            {
                return;
            }

            lastUpdateTime = Time.time;

            // Check each child's visibility
            foreach (Transform child in children)
            {
                if (child == null)
                {
                    continue;
                }

                bool isVisible = IsVisibleFromCamera(child.position);
                child.gameObject.SetActive(isVisible);
            }
        }

        private bool IsVisibleFromCamera(Vector3 worldPosition)
        {
            // Convert world position to viewport coordinates
            Vector3 viewportPoint = Culler.WorldToViewportPoint(worldPosition);

            // Calculate padded bounds
            float minBound = -ViewportPadding;
            float maxBound = 1f + ViewportPadding;

            // Check if the point is within the viewport (with padding) and in front of the camera
            bool inViewport =
                viewportPoint.x >= minBound
                && viewportPoint.x <= maxBound
                && viewportPoint.y >= minBound
                && viewportPoint.y <= maxBound
                && viewportPoint.z > 0;

            return inViewport;
        }
    }
}
