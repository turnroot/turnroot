using System.Collections.Generic;
using Turnroot.Conversations.Mermaid;
using UnityEngine;

namespace Turnroot.Conversations
{
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Turnroot/Conversation")]
    public class Conversation : ScriptableObject
    {
        private string _uniqueId;
        public string UniqueId
        {
            get
            {
                _uniqueId = System.Guid.NewGuid().ToString();
                return _uniqueId;
            }
        }
        public TextAsset MermaidSource { get; set; }
        public bool CanRepeat { get; set; } = false;
        public List<ConversationPerson> People { get; set; } = new();

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
