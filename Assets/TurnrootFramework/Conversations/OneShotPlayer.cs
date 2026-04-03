using Turnroot.Utilities;
using UnityEngine;

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
    }
}
