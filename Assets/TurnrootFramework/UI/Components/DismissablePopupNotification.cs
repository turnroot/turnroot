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
        /// Shows based on an Brain event, is triggered by Conversation action nodes automatically,
        /// can be used outside of conversations.
        /// Matches by ID- one DismissablePopupNotification per needed popup
        /// Temporarily subscribes to UiInputProvider.OnInput to detect player input, and unsubscribes when done
        /// </summary>
        [InfoBox(
            "The ID needs to match whatever is triggering; if it's a conversation event, it should be the same as the conversation Action ID"
        )]
        public string popupId;
        public bool RequiresPlayerInputToDismiss = true;

        [ShowIf(nameof(RequiresPlayerInputToDismiss), false)]
        public float DismissDelay = 5f;
        private UIFade _fade;

        public bool PlaysSoundOnShow = false;

        [ShowIf(nameof(PlaysSoundOnShow), true)]
        public AudioClip SoundOnShow;

        [ShowIf(nameof(PlaysSoundOnShow), true)]
        public AudioSource AudioSource;

        public bool PlayTimelineOnShow = false;

        [ShowIf(nameof(PlayTimelineOnShow), true)]
        public PlayableDirector TimelineOnShow;

        private Brain _brain;
        private UiInputProvider _inputProvider;
        private bool _isShowing;
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
            _brain.OnWaitForPlayerAcknowledgment += HandleWaitForPlayerAcknowledgment;
        }

        private void OnDisable()
        {
            _brain.OnWaitForPlayerAcknowledgment -= HandleWaitForPlayerAcknowledgment;
            if (_inputProvider != null)
            {
                _inputProvider.OnInput -= HandleInput;
            }
            StopAutoDismissCoroutine();
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

        private void Done()
        {
            if (!_isShowing)
            {
                return;
            }

            _isShowing = false;
            StopAutoDismissCoroutine();
            _fade.Hide();
            _brain.PublishPlayerAcknowledgedConversationEvent();
            if (_inputProvider != null)
            {
                _inputProvider.OnInput -= HandleInput;
            }
        }

        /// <summary>
        ///  Shows the popup along with whatever is enabled
        /// </summary>
        /// <param name="id"></param>
        private void HandleWaitForPlayerAcknowledgment(string id)
        {
            if (id != popupId || _isShowing)
            {
                return;
            }

            _isShowing = true;

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
