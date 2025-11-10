using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using UnityEngine;
using UnityEngine.Events;

namespace TurnrootFramework.Conversations
{
    [System.Serializable]
    public class SimpleConversationLayer
    {
        [SerializeField]
        private string _dialogue;
        public string Dialogue
        {
            get => _dialogue;
            set => _dialogue = value;
        }

        [SerializeField, SerializeReference]
        private CharacterData _speaker;
        public CharacterData Speaker
        {
            get => _speaker;
            set
            {
                _speaker = value;
                SpeakerPortrait?.SetOwner(_speaker);
            }
        }

        [SerializeField]
        private string _speakerDisplayName;
        public string SpeakerDisplayName
        {
            get => _speakerDisplayName;
            set => _speakerDisplayName = value;
        }

        [SerializeField, SerializeReference]
        private Portrait _speakerPortrait;
        public Portrait SpeakerPortrait
        {
            get => _speakerPortrait;
            set => _speakerPortrait = value;
        }

        public UnityEvent OnLayerStart;
        public UnityEvent OnLayerEnd;
        public UnityEvent OnLayerComplete; // Signals when this layer is finished
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
    }
}
