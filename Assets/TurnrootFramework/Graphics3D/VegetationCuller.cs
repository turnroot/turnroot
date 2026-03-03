using System.Collections.Generic;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Graphics3D
{
    public class VegetationCuller : MonoBehaviour
    {
        public UnityEngine.Camera Culler;
        private float ViewportPadding = 1f;
        private float UpdateInterval = 0.1f;

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

            var g = GameplayPlayerSettings.Instance;
            if (g != null)
            {
                var s = g.QualityStep;
                switch (s)
                {
                    case 0:
                        ViewportPadding = .018f;
                        UpdateInterval = .2f;
                        break;
                    case 1:
                        ViewportPadding = .02f;
                        UpdateInterval = .17f;
                        break;
                    case 2:
                        ViewportPadding = .023f;
                        UpdateInterval = .13f;
                        break;
                    case 3:
                        ViewportPadding = .026f;
                        UpdateInterval = .1f;
                        break;
                    default:
                        break;
                }
            }
        }

        void Update()
        {
            if (Culler == null)
            {
                return;
            }

            if (Time.time - lastUpdateTime < UpdateInterval)
            {
                return;
            }

            lastUpdateTime = Time.time;

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
