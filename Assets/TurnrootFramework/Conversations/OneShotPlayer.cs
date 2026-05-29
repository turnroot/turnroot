using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Helper component for playing single-shot flavor dialogue (OneShot) without needing a full Conversation flow.
    /// Finds the scene's ConversationController to render the UI and play the line.
    /// Optionally plays audio using an AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class OneShotPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        private ConversationController _controller;
        private UnityAction _pendingCallback;

        public bool HasPendingCallback => _pendingCallback != null;

        private void Awake()
        {
            _audioSource ??= GetComponent<AudioSource>();
            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
            }
        }

        public void SetAudioSource(AudioSource audioSource)
        {
            _audioSource = audioSource;
            if (_audioSource != null)
            {
                _audioSource.playOnAwake = false;
            }
        }

        public void PlayOneShot(OneShot oneShot)
        {
            if (oneShot.Audio != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(oneShot.Audio);
            }

            if (!_controller)
            {
                _controller = FindFirstObjectByType<ConversationController>();
            }

            if (!_controller)
            {
                "OneShotPlayer: ConversationController instance not found. Ensure a ConversationController exists in the scene.".LogWarning();

                return;
            }

            _controller.PlayOneShot(oneShot);
        }

        /// <summary>
        /// Plays a one-shot and subscribes <paramref name="onFinished"/> to fire once when the
        /// conversation controller reports completion. If the one-shot has no dialogue the
        /// callback is invoked immediately.
        /// </summary>
        public void PlayOneShotThen(OneShot oneShot, UnityAction onFinished)
        {
            if (!string.IsNullOrWhiteSpace(oneShot.Dialogue))
            {
                if (!_controller)
                {
                    _controller = FindFirstObjectByType<ConversationController>();
                }
                if (_controller != null)
                {
                    _pendingCallback = onFinished;
                    _controller.OnAnyConversationFinished.AddListener(onFinished);
                    PlayOneShot(oneShot);
                    return;
                }

                PlayOneShot(oneShot);
                onFinished?.Invoke();
            }
            else
            {
                onFinished?.Invoke();
            }
        }

        /// <summary>Removes a specific finished callback and clears the pending reference if it matches.</summary>
        public void UnsubscribeOneShotFinished(UnityAction onFinished)
        {
            _controller?.OnAnyConversationFinished.RemoveListener(onFinished);
            if (_pendingCallback == onFinished)
            {
                _pendingCallback = null;
            }
        }

        /// <summary>Removes whatever pending callback is currently registered. Safe to call from OnDisable.</summary>
        public void ClearPendingCallback()
        {
            if (_pendingCallback != null)
            {
                _controller?.OnAnyConversationFinished.RemoveListener(_pendingCallback);
                _pendingCallback = null;
            }
        }
    }
}
