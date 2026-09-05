using System.Collections.Generic;
using Turnroot.Conversations.Mermaid;
using UnityEngine;

namespace Turnroot.Conversations
{
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Turnroot/Conversation")]
    public class Conversation : ScriptableObject
    {
        [SerializeField]
        private TextAsset mermaidSource;

        [SerializeField]
        private List<ConversationPerson> people = new();

        [SerializeField]
        private bool canRepeat;

        private string _uniqueId;
        public string UniqueId
        {
            get
            {
                _uniqueId = System.Guid.NewGuid().ToString();
                return _uniqueId;
            }
        }

        public TextAsset MermaidSource
        {
            get => mermaidSource;
            set => mermaidSource = value;
        }

        public bool CanRepeat
        {
            get => canRepeat;
            set => canRepeat = value;
        }

        public List<ConversationPerson> People
        {
            get => people ??= new List<ConversationPerson>();
            set => people = value ?? new List<ConversationPerson>();
        }

        [System.NonSerialized]
        private MermaidConversationGraph _cachedGraph;

        public MermaidConversationGraph GetGraph()
        {
            if (_cachedGraph != null)
            {
                return _cachedGraph;
            }

            if (MermaidSource == null)
            {
                return null;
            }

            _cachedGraph = MermaidConversationParser.Parse(MermaidSource.text, name);
            return _cachedGraph;
        }

        public void InvalidateCache() => _cachedGraph = null;
    }
}
