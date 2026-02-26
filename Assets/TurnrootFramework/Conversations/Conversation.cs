using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Defines a conversation with linear or branching dialogue paths.
    /// </summary>
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Turnroot/Conversation")]
    public class Conversation : ScriptableObject
    {
        [field:
            SerializeField,
            InfoBox("If checked, this conversation will branch based on player choices.")
        ]
        public bool BranchingConversation { get; private set; } = true;

        public UnityEvent OnConversationStart;
        public UnityEvent OnConversationEnd;

        [field: SerializeField, ReorderableList, HideIf("BranchingConversation")]
        public ConversationLayer[] Layers { get; set; }

        [field:
            SerializeField,
            ShowIf("BranchingConversation"),
            InfoBox("Branching is handled by a ConversationGraph")
        ]
        public Branching.Nodes.ConversationGraph ConversationGraph { get; private set; }

        // runtime cache built from the graph
        private Dictionary<int, NodeData> _graphNodes;

        public Dictionary<int, NodeData> GetGraphNodes()
        {
            // Always rebuild runtime node data to avoid holding references to destroyed editor nodes.
            _graphNodes = BranchedConversationHelpers.GetDataFromGraph(ConversationGraph);
            return _graphNodes;
        }

        // Returns names of entry nodes (nodes with no incoming ConversationFlow connections)
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
                if (
                    !ValidationHelper.ValidateNotNull(nd, nameof(nd))
                    || !ValidationHelper.ValidateNotNull(nd.node, nameof(nd.node))
                )
                {
                    continue;
                }

                if (nd.node is Branching.ConversationNode conv)
                {
                    if (nd.incomingCount == 0)
                    {
                        entries.Add(conv.name);
                    }
                }
            }
            return entries;
        }

        [SerializeField]
        private int _currentLayerIndex = 0;
        public int CurrentLayerIndex
        {
            get => _currentLayerIndex;
            set => _currentLayerIndex = Mathf.Clamp(value, 0, Layers.Length - 1);
        }

        public ConversationLayer CurrentLayer
        {
            get
            {
                return _currentLayerIndex < 0 || _currentLayerIndex >= Layers.Length
                    ? null
                    : Layers[_currentLayerIndex];
            }
        }

        public void StartConversation()
        {
            OnConversationStart?.Invoke();
            _currentLayerIndex = 0;
        }
    }
}
