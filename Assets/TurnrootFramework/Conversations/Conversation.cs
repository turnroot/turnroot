using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Conversations
{
    [CreateAssetMenu(fileName = "New Conversation", menuName = "Turnroot/Conversation")]
    public class Conversation : ScriptableObject
    {
        [
            SerializeField,
            InfoBox("If checked, this conversation will branch based on player choices.")
        ]
        private bool _branchingConversation = true;
        public bool BranchingConversation => _branchingConversation;

        public UnityEvent OnConversationStart;
        public UnityEvent OnConversationEnd;

        [SerializeField, ReorderableList, HideIf("BranchingConversation")]
        private ConversationLayer[] _layers;
        public ConversationLayer[] Layers
        {
            get => _layers;
            set => _layers = value;
        }

        [
            SerializeField,
            ShowIf("BranchingConversation"),
            InfoBox("Branching is handled by a ConversationGraph")
        ]
        private Branching.Nodes.ConversationGraph _conversationGraph;
        public Branching.Nodes.ConversationGraph ConversationGraph => _conversationGraph;

        // runtime cache built from the graph
        private Dictionary<int, NodeData> _graphNodes;
        public Dictionary<int, NodeData> GetGraphNodes()
        {
            // Always rebuild runtime node data to avoid holding references to destroyed editor nodes.
            _graphNodes = BranchedConversationHelpers.GetDataFromGraph(_conversationGraph);
            return _graphNodes;
        }

        // Returns names of entry nodes (nodes with no incoming ConversationFlow connections)
        public List<string> GetGraphEntryNodeNames()
        {
            var entries = new List<string>();
            if (_conversationGraph == null)
                return entries;
            var gnodes = GetGraphNodes();
            if (gnodes == null)
                return entries;
            foreach (var kv in gnodes)
            {
                var nd = kv.Value;
                if (nd == null || nd.node == null)
                    continue;
                if (nd.node is Branching.ConversationNode conv)
                {
                    if (nd.incomingCount == 0)
                        entries.Add(conv.name);
                }
            }
            return entries;
        }

        [SerializeField]
        private int _currentLayerIndex = 0;
        public int CurrentLayerIndex
        {
            get => _currentLayerIndex;
            set => _currentLayerIndex = Mathf.Clamp(value, 0, _layers.Length - 1);
        }

        public ConversationLayer CurrentLayer
        {
            get
            {
                if (_currentLayerIndex < 0 || _currentLayerIndex >= _layers.Length)
                    return null;
                return _layers[_currentLayerIndex];
            }
        }

        public void StartConversation()
        {
            OnConversationStart?.Invoke();
            _currentLayerIndex = 0;
        }
    }
}
