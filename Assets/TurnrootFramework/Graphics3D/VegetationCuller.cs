using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Graphics3D
{
    public class VegetationCuller : MonoBehaviour
    {
        public UnityEngine.Camera Culler;

        public GrassRenderer GrassRenderer;
        private float ViewportPadding = 1f;
        private float UpdateInterval = 0.1f;

        private Transform[] children;

        private Animator[] grandchildrenAnimators;
        private float lastUpdateTime;
        private Brain _brain;

        private void Start()
        {
            _brain = FindFirstObjectByType<Brain>();

            if (_brain != null)
            {
                _brain.OnGraphicsQualityChanged += OnGraphicsQualityChanged;
                OnGraphicsQualityChanged();
            }
            else
            {
                "VegetationCuller: No Brain found in scene. Graphics quality changes will not be handled.".LogWarning();
            }

            List<Transform> grandchildrenList = new List<Transform>();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                for (int j = 0; j < child.childCount; j++)
                {
                    grandchildrenList.Add(child.GetChild(j));
                }
            }

            children = grandchildrenList.ToArray();
            children.Length.ToString().LogInfo();
        }

        private void OnGraphicsQualityChanged()
        {
            var g = GameplayPlayerSettings.Instance;
            if (g != null)
            {
                var s = g.QualityStep;
                switch (s)
                {
                    case 0:
                        ViewportPadding = .018f;
                        UpdateInterval = .7f;
                        GrassRenderer.grassEnabled = false;
                        DisableGrandchildrenAnimators();
                        break;
                    case 1:
                        ViewportPadding = .02f;
                        UpdateInterval = .4f;
                        GrassRenderer.grassEnabled = true;
                        DisableGrandchildrenAnimators();
                        break;
                    case 2:
                        ViewportPadding = .024f;
                        UpdateInterval = .2f;
                        GrassRenderer.grassEnabled = true;
                        EnableGrandchildrenAnimators();
                        break;
                    case 3:
                        ViewportPadding = .028f;
                        UpdateInterval = .05f;
                        GrassRenderer.grassEnabled = true;
                        EnableGrandchildrenAnimators();
                        break;
                    default:
                        break;
                }
            }
        }

        private void DisableGrandchildrenAnimators()
        {
            List<Animator> animatorsList = new List<Animator>();

            foreach (Transform child in children)
            {
                if (child == null)
                {
                    continue;
                }

                if (child.TryGetComponent<Animator>(out var animator))
                {
                    animatorsList.Add(animator);
                    animator.enabled = false;
                }
            }

            grandchildrenAnimators = animatorsList.ToArray();
        }

        private void EnableGrandchildrenAnimators()
        {
            if (grandchildrenAnimators == null)
            {
                return;
            }

            foreach (var animator in grandchildrenAnimators)
            {
                if (animator != null)
                {
                    animator.enabled = true;
                }
            }
        }

        private void Update()
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

        private void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnGraphicsQualityChanged -= OnGraphicsQualityChanged;
            }
        }
    }
}
