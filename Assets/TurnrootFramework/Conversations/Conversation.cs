using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Defines a branching conversation as an XNode graph.
    /// </summary>
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Turnroot/Conversation")]
    public class Conversation : ScriptableObject
    {
        [field: SerializeField]
        public Branching.Nodes.ConversationGraph ConversationGraph { get; private set; }

        private Dictionary<int, NodeData> _graphNodes;

        public Dictionary<int, NodeData> GetGraphNodes()
        {
            _graphNodes = BranchedConversationHelpers.GetDataFromGraph(ConversationGraph);
            return _graphNodes;
        }

        public List<string> GetGraphEntryNodeNames()
        {
            var entries = new List<string>();
            if (!ValidationHelper.ValidateNotNull(ConversationGraph, nameof(ConversationGraph)))
            {
                return entries;
            }

            var gnodes = GetGraphNodes();
            if (!ValidationHelper.ValidateNotNull(gnodes, nameof(gnodes)))
            {
                return entries;
            }

            foreach (var kv in gnodes)
            {
                var nd = kv.Value;
                if (nd?.node == null)
                {
                    continue;
                }

                if (nd.node is Branching.ConversationNode conv && nd.incomingCount == 0)
                {
                    entries.Add(conv.name);
                }
            }
            return entries;
        }
    }
}
