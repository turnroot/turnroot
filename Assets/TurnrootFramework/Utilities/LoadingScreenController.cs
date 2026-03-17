using TMPro;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Drives a reusable loading screen UI that can be shared across multiple scenes.
    ///
    /// This component listens for Brain scene transition and loading progress events
    /// and updates the configured UI elements accordingly.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadingScreenController : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Fade component that shows/hides the loading screen.")]
        public UIFade Fade;

        [Tooltip("Fill driver used for progress bar visuals.")]
        public UiFillDriver FillDriver;

        [Tooltip("Optional text element to display a loading percentage.")]
        public TextMeshProUGUI PercentageText;

        [Header("Behavior")]
        [Tooltip(
            "Whether the loading screen should automatically show when a scene transition starts."
        )]
        public bool showOnSceneTransitionStart = true;

        [Tooltip(
            "Whether the loading screen should automatically hide when the new scene is ready to display."
        )]
        public bool autoHideOnReadyToDisplay = true;

        [Tooltip("Minimum time (seconds) the loading screen should remain visible.")]
        public float minimumVisibleTime = 0.5f;

        [Tooltip("Optional extra delay after the scene is ready before hiding the loading screen.")]
        public float hideDelayAfterReady = 0.1f;

        private Brain _brain;
        private LoadingController _loadingController;
        private float _visibleSinceTime;

        private void Awake()
        {
            // Try to auto-wire common components
            if (Fade == null)
            {
                Fade = GetComponentInChildren<UIFade>(true);
            }

            if (FillDriver == null)
            {
                FillDriver = GetComponentInChildren<UiFillDriver>(true);
            }
        }

        private void Start()
        {
            _brain = FindFirstObjectByType<Brain>();
            if (_brain == null)
            {
                "LoadingScreenController: No Brain found in scene".LogWarning();
                return;
            }

            _loadingController = _brain.GetComponent<LoadingController>();
            SubscribeToBrainEvents();
            SubscribeToLoadingController();

            if (Fade != null && Fade.Visible)
            {
                Fade.Hide();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromBrainEvents();
            UnsubscribeFromLoadingController();
        }

        private void SubscribeToBrainEvents()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnSceneTransitionStarted += HandleSceneTransitionStarted;
            _brain.OnSceneLoadProgress += HandleSceneLoadProgress;
            _brain.OnSceneReadyToDisplay += HandleSceneReadyToDisplay;
        }

        private void UnsubscribeFromBrainEvents()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnSceneTransitionStarted -= HandleSceneTransitionStarted;
            _brain.OnSceneLoadProgress -= HandleSceneLoadProgress;
            _brain.OnSceneReadyToDisplay -= HandleSceneReadyToDisplay;
        }

        private void SubscribeToLoadingController()
        {
            if (_loadingController == null)
            {
                return;
            }

            _loadingController.OnProgressChanged += HandleLoadingControllerProgress;
        }

        private void UnsubscribeFromLoadingController()
        {
            if (_loadingController == null)
            {
                return;
            }

            _loadingController.OnProgressChanged -= HandleLoadingControllerProgress;
        }

        private void HandleSceneTransitionStarted(string sceneName, string displayName)
        {
            if (!showOnSceneTransitionStart)
            {
                return;
            }

            Show();
        }

        private void HandleSceneLoadProgress(float progress)
        {
            SetProgress(progress);
        }

        private void HandleLoadingControllerProgress(float progress)
        {
            SetProgress(progress);
        }

        private void HandleSceneReadyToDisplay(string sceneName, string displayName)
        {
            if (!autoHideOnReadyToDisplay)
            {
                return;
            }

            float elapsed = Time.time - _visibleSinceTime;
            float remaining = Mathf.Max(0f, minimumVisibleTime - elapsed);
            float delay = remaining + hideDelayAfterReady;

            if (Fade != null)
            {
                Fade.HideAfterTime(delay);
            }
            else
            {
                // If no fade is set, just log a warning when it should end.
                "LoadingScreenController: Hide requested but no UIFade assigned.".LogWarning();
            }
        }

        public void Show()
        {
            _visibleSinceTime = Time.time;
            SetProgress(0f);
            Fade?.Show();
        }

        public void Hide()
        {
            Fade?.Hide();
        }

        public void SetProgress(float progress)
        {
            float clamped = Mathf.Clamp01(progress);
            FillDriver?.SetAmount(clamped);
            if (PercentageText != null)
            {
                PercentageText.text = Mathf.RoundToInt(clamped * 100f) + "%";
            }
        }
    }
}
