using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using UnityEngine;
using UnityEngine.Events;

namespace TurnrootFramework.Conversations
{
    [System.Serializable]
    public class ConversationLayer : BaseConversation
    {
        [SerializeField, SerializeReference]
        private CharacterData _speaker;
        public CharacterData Speaker
        {
            get => _speaker;
            set
            {
                _speaker = value;
                // Clear portrait key if speaker changes and key is invalid
                if (
                    _speaker == null
                    || (
                        _speakerPortraitKey != null
                        && !_speaker.Portraits.ContainsKey(_speakerPortraitKey)
                    )
                )
                {
                    _speakerPortraitKey = null;
                }
            }
        }

        [SerializeField]
        private string _speakerDisplayName;
        public string SpeakerDisplayName
        {
            get => _speakerDisplayName;
            set => _speakerDisplayName = value;
        }

        [SerializeField, Dropdown("GetAvailablePortraitKeys")]
        private string _speakerPortraitKey;

        public string SpeakerPortraitKey
        {
            get => _speakerPortraitKey;
            set
            {
                _speakerPortraitKey = value;
                // Clear cached sprite when portrait changes
                _PortraitSprite = null;
            }
        }

        public Portrait SpeakerPortrait
        {
            get
            {
                if (
                    _speaker != null
                    && _speakerPortraitKey != null
                    && _speaker.Portraits.ContainsKey(_speakerPortraitKey)
                )
                {
                    return _speaker.Portraits[_speakerPortraitKey];
                }
                return null;
            }
        }

        public UnityEvent OnLayerStart;
        public UnityEvent OnLayerEnd;
        public UnityEvent OnLayerComplete;
        private Sprite _PortraitSprite;

        public Sprite PortraitSprite
        {
            get
            {
                if (_PortraitSprite == null && SpeakerPortrait != null)
                {
                    _PortraitSprite = SpeakerPortrait.SavedSprite;
                }
                return _PortraitSprite;
            }
        }

        public void OnAwake()
        {
            if (SpeakerPortrait != null)
            {
                _PortraitSprite = SpeakerPortrait.SavedSprite;
            }
        }

        public void StartLayer()
        {
            OnLayerStart?.Invoke();
        }

        public void EndLayer()
        {
            OnLayerEnd?.Invoke();
        }

        public void CompleteLayer()
        {
            OnLayerComplete?.Invoke();
        }

        private bool HasSpeaker()
        {
            return _speaker != null;
        }

        private string[] GetAvailablePortraitKeys()
        {
            if (_speaker == null || _speaker.Portraits == null)
            {
                // Clear the key if speaker is null
                if (!string.IsNullOrEmpty(_speakerPortraitKey))
                {
                    _speakerPortraitKey = null;
                }
                return new string[] { "No speaker selected" };
            }

            var keys = _speaker.Portraits.Keys.ToArray();
            // Ensure current value is valid
            if (!string.IsNullOrEmpty(_speakerPortraitKey) && !keys.Contains(_speakerPortraitKey))
            {
                _speakerPortraitKey = null;
            }
            return keys.Length > 0 ? keys : new string[] { "No portraits available" };
        }
    }
}
