using System.Linq;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Helper component for playing single-shot flavor dialogue (OneShot) without needing a full Conversation flow.
    /// Uses ConversationController.Instance to render the UI and play the line.
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
            _controller = ConversationController.Instance;
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

            var controller = _controller ?? ConversationController.Instance;
            if (controller == null)
            {
                controller = FindFirstObjectByType<ConversationController>();
                if (controller == null)
                {
                    controller = Resources
                        .FindObjectsOfTypeAll<ConversationController>()
                        .FirstOrDefault();
                }

                _controller = controller;
            }

            if (controller == null)
            {
                "OneShotPlayer: ConversationController instance not found. Ensure a ConversationController exists in the scene.".LogWarning();

                return;
            }

            $"OneShotPlayer: calling PlayOneShot (dialogue='{oneShot.Dialogue}', speaker='{oneShot.SpeakerName}').".LogInfo();

            controller.PlayOneShot(oneShot);
        }
    }
}
