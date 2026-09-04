using System.Collections.Generic;
using Turnroot.Conversations.Mermaid;
using UnityEngine;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Defines a conversation as a Mermaid flowchart. The source text is parsed at runtime into
    /// a directed graph of dialogue, choices, actions, and conditions.
    /// </summary>
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Turnroot/Conversation")]
    public class Conversation : ScriptableObject
    {
        [field: SerializeField]
        public TextAsset MermaidSource { get; set; }

        [field: SerializeField]
        public List<ConversationPerson> People { get; set; } = new();

        [System.NonSerialized]
        private MermaidConversationGraph _cachedGraph;

        /// <summary>
        /// Parses and returns the Mermaid graph. Results are cached for the lifetime of this instance.
        /// </summary>
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

        /// <summary>
        /// Clears the cached parsed graph. Call after changing the Mermaid source at runtime.
        /// </summary>
        public void InvalidateCache() => _cachedGraph = null;
    }
}
