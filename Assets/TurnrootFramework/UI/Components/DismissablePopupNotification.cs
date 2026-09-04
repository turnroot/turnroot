using System;
using NaughtyAttributes;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.Playables;

namespace Turnroot.UI.Components
{
    [RequireComponent(typeof(UIFade))]
    public class DismissablePopupNotification : MonoBehaviour
    {
        /// <summary>
        /// UI popup that will require player input to dismiss, or auto-dismiss after a delay.
        /// Triggered by <see cref="PopupManager"/> (or any other system) via <see cref="Show"/>.
        /// temporarily subscribes to UiInputProvider.OnInput to detect player input
        /// </summary>
        public bool RequiresPlayerInputToDismiss = true;

        [HideIf(nameof(RequiresPlayerInputToDismiss))]
        public float DismissDelay = 5f;
        private UIFade _fade;

        public bool PlaysSoundOnShow = false;

        [ShowIf(nameof(PlaysSoundOnShow))]
        public AudioClip SoundOnShow;

        [ShowIf(nameof(PlaysSoundOnShow))]
        public AudioSource AudioSource;

        public bool PlayTimelineOnShow = false;

        [ShowIf(nameof(PlayTimelineOnShow))]
        public PlayableDirector TimelineOnShow;
        public event Action OnDismissed;

        private Brain _brain;
        private UiInputProvider _inputProvider;
        private bool _isShowing;
        private bool _dismissalReported;
        private Coroutine _autoDismissCoroutine;

        private void Awake()
        {
            _fade = GetComponent<UIFade>();
            _brain = GetAndCacheBrain.GetBrain();
            if (_inputProvider == null)
            {
                _inputProvider = GetAndCacheBrain.GetInputProvider();
                if (_inputProvider == null)
                {
                    "DismissablePopupNotification: InputProvider not found! Cannot handle input. Disabling".LogError(
                        "DismissablePopupNotification"
                    );
                    enabled = false;
                }
            }
        }

        private void OnEnable()
        {
            if (_fade != null)
            {
                _fade.OnHidden.AddListener(HandleFadeHidden);
            }
        }

        private void OnDisable()
        {
            StopAutoDismissCoroutine();
            if (_inputProvider != null)
            {
                _inputProvider.OnInput -= HandleInput;
            }

            if (_fade != null)
            {
                _fade.OnHidden.RemoveListener(HandleFadeHidden);
            }

            if (_isShowing)
            {
                _isShowing = false;
                ReportDismissed();
            }
        }

        private System.Collections.IEnumerator AutoDismissAfterDelay()
        {
            yield return new WaitForSeconds(DismissDelay);
            _autoDismissCoroutine = null;
            Done();
        }

        private void StopAutoDismissCoroutine()
        {
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
        }

        public void Show()
        {
            if (_isShowing)
            {
                return;
            }

            _isShowing = true;
            _dismissalReported = false;

            if (PlaysSoundOnShow && SoundOnShow != null && AudioSource != null)
            {
                AudioSource?.PlayOneShot(SoundOnShow);
            }

            if (PlayTimelineOnShow && TimelineOnShow != null)
            {
                TimelineOnShow?.Play();
            }
            _fade.Show();

            if (RequiresPlayerInputToDismiss)
            {
                if (_inputProvider == null)
                {
                    return;
                }

                _inputProvider.OnInput -= HandleInput; // prevent duplicate subscriptions
                _inputProvider.OnInput += HandleInput; // dismiss on input
            }
            else
            {
                StopAutoDismissCoroutine();
                _autoDismissCoroutine = StartCoroutine(AutoDismissAfterDelay()); // auto dismiss
            }
        }

        private void Done()
        {
            if (!_isShowing)
            {
                return;
            }

            _isShowing = false;
            StopAutoDismissCoroutine();
            _brain.PublishPlayerAcknowledgedConversationEvent();
            if (_inputProvider != null)
            {
                _inputProvider.OnInput -= HandleInput;
            }

            _fade.Hide();
        }

        private void HandleFadeHidden()
        {
            ReportDismissed();
            Destroy(gameObject);
        }

        private void ReportDismissed()
        {
            if (_dismissalReported)
            {
                return;
            }

            _dismissalReported = true;
            OnDismissed?.Invoke();
        }

        private void HandleInput(string action)
        {
            if (
                action
                is InputActionConstants.Select
                    or InputActionConstants.Start
                    or InputActionConstants.Submit
                    or InputActionConstants.Confirm
            )
            {
                Done();
            }
        }
    }
}
