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
        private bool _branchingConversation = false;
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
        private Turnroot.Conversations.Branching.Nodes.ConversationGraph _conversationGraph;
        public Turnroot.Conversations.Branching.Nodes.ConversationGraph ConversationGraph =>
            _conversationGraph;

        [
            SerializeField,
            Tooltip(
                "Enable verbose logging when parsing the ConversationGraph (useful for debugging missing/mismatched nodes)."
            )
        ]
        private bool _debugGraphParsing = false;

        // runtime cache built from the graph
        private Dictionary<int, NodeData> _graphNodes;

        // Build runtime node structures from the XNode graph (two-pass: incoming counts, then node data)
        private void GetDataFromGraph()
        {
            if (_conversationGraph == null)
                return;

            try
            {
                var nodes = new Dictionary<int, NodeData>();

                // --- Pass 1: incoming counts ---
                var incomingCounts = new Dictionary<int, int>();
                foreach (var node in _conversationGraph.nodes)
                {
                    if (node == null)
                        continue;
                    foreach (var port in node.Ports)
                    {
                        if (port.direction != XNode.NodePort.IO.Output)
                            continue;
                        if (
                            port.ValueType
                            != typeof(Turnroot.Conversations.Branching.ConversationFlow)
                        )
                            continue;
                        var conns = port.GetConnections();
                        if (conns == null)
                            continue;
                        foreach (var c in conns)
                        {
                            if (c.node == null)
                                continue;
                            var tid = c.node.GetInstanceID();
                            if (!incomingCounts.TryGetValue(tid, out var cnt))
                                incomingCounts[tid] = 1;
                            else
                                incomingCounts[tid] = cnt + 1;
                        }
                    }
                }

                // --- Pass 2: build NodeData ---
                foreach (var node in _conversationGraph.nodes)
                {
                    if (node == null)
                        continue;

                    var nd = new NodeData
                    {
                        node = node,
                        id = node.GetInstanceID(),
                        name = node.name,
                        choices = new List<ChoiceData>(),
                        incomingCount = incomingCounts.TryGetValue(node.GetInstanceID(), out var ic)
                            ? ic
                            : 0,
                    };

                    if (node is Turnroot.Conversations.Branching.ConversationNode conv)
                        nd.conversationLayer = conv.conversationLayer;

                    // gather outgoing ConversationFlow connections
                    var outgoing = new List<(string portName, XNode.Node target)>();
                    foreach (var port in node.Ports)
                    {
                        if (port.direction != XNode.NodePort.IO.Output)
                            continue;
                        if (
                            port.ValueType
                            != typeof(Turnroot.Conversations.Branching.ConversationFlow)
                        )
                            continue;
                        var conns = port.GetConnections();
                        if (conns == null)
                            continue;
                        foreach (var c in conns)
                        {
                            if (c.node == null)
                                continue;
                            outgoing.Add((port.fieldName ?? string.Empty, c.node));
                        }
                    }

                    if (outgoing.Count == 1)
                    {
                        nd.nextTargetId = outgoing[0].target.GetInstanceID();
                    }
                    else if (outgoing.Count > 1)
                    {
                        foreach (var (portName, targetNode) in outgoing)
                        {
                            var choice = new ChoiceData
                            {
                                portName = portName,
                                targetNodeId = targetNode.GetInstanceID(),
                                targetNodeName = targetNode.name,
                            };

                            choice.label = ResolveLabelForPort(node, portName, targetNode);
                            choice.choiceText = choice.label;
                            nd.choices.Add(choice);
                        }
                    }

                    nodes[nd.id] = nd;
                }

                _graphNodes = nodes;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"Conversation.GetDataFromGraph: exception while parsing graph '{_conversationGraph?.name}': {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}"
                );
                _graphNodes = null;
            }
        }

        // Resolve a choice label for a port. Returns the final label string (never null).
        private string ResolveLabelForPort(XNode.Node node, string portName, XNode.Node targetNode)
        {
            object labelVal = null;
            string matchedMemberName = null;
            var t = node.GetType();

            if (!string.IsNullOrEmpty(portName))
            {
                // Try field (exact, then lower-first, then case-insensitive)
                FieldInfo fi = t.GetField(
                    portName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );
                if (fi == null && portName.Length > 0)
                {
                    var s = char.ToLowerInvariant(portName[0]) + portName.Substring(1);
                    fi = t.GetField(
                        s,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );
                }
                if (fi == null)
                {
                    foreach (
                        var f in t.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        )
                    )
                    {
                        if (
                            string.Equals(
                                f.Name,
                                portName,
                                System.StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            fi = f;
                            break;
                        }
                    }
                }

                // If matched field exists but isn't string, prefer a sibling string field
                if (fi != null && fi.FieldType != typeof(string))
                {
                    FieldInfo stringField = null;
                    if (portName.Length > 0)
                    {
                        var lf = char.ToLowerInvariant(portName[0]) + portName.Substring(1);
                        stringField = t.GetField(
                            lf,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        );
                    }
                    if (stringField == null)
                    {
                        foreach (
                            var f in t.GetFields(
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                            )
                        )
                        {
                            if (
                                f.FieldType == typeof(string)
                                && string.Equals(
                                    f.Name,
                                    portName,
                                    System.StringComparison.OrdinalIgnoreCase
                                )
                            )
                            {
                                stringField = f;
                                break;
                            }
                        }
                    }
                    if (stringField != null)
                        fi = stringField;
                }

                if (fi != null)
                {
                    labelVal = fi.GetValue(node);
                    matchedMemberName = fi.Name;
                }

                // property fallback
                if (labelVal == null)
                {
                    PropertyInfo pi = t.GetProperty(
                        portName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    if (pi == null && portName.Length > 0)
                    {
                        var s = char.ToLowerInvariant(portName[0]) + portName.Substring(1);
                        pi = t.GetProperty(
                            s,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        );
                    }

                    if (pi != null && pi.PropertyType != typeof(string))
                    {
                        PropertyInfo stringProp = null;
                        if (portName.Length > 0)
                        {
                            var lf = char.ToLowerInvariant(portName[0]) + portName.Substring(1);
                            stringProp = t.GetProperty(
                                lf,
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                            );
                        }
                        if (stringProp == null)
                        {
                            foreach (
                                var p in t.GetProperties(
                                    BindingFlags.Public
                                        | BindingFlags.NonPublic
                                        | BindingFlags.Instance
                                )
                            )
                            {
                                if (
                                    p.PropertyType == typeof(string)
                                    && string.Equals(
                                        p.Name,
                                        portName,
                                        System.StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                {
                                    stringProp = p;
                                    break;
                                }
                            }
                        }
                        if (stringProp != null)
                            pi = stringProp;
                    }

                    if (pi != null)
                    {
                        labelVal = pi.GetValue(node);
                        matchedMemberName = pi.Name;
                    }
                }
            }

            string finalLabel = null;
            if (labelVal is string sVal && !string.IsNullOrEmpty(sVal))
                finalLabel = sVal;
            else if (labelVal != null)
            {
                var lvType = labelVal.GetType();
                var candidate =
                    lvType.GetField(
                        "label",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    )
                    ?? lvType.GetField(
                        "text",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    )
                    ?? lvType.GetField(
                        "choiceText",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    );
                if (candidate != null)
                {
                    var v = candidate.GetValue(labelVal) as string;
                    if (!string.IsNullOrEmpty(v))
                        finalLabel = v;
                }
                else
                {
                    var pc =
                        lvType.GetProperty(
                            "label",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        )
                        ?? lvType.GetProperty(
                            "text",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        )
                        ?? lvType.GetProperty(
                            "choiceText",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        );
                    if (pc != null)
                    {
                        var v = pc.GetValue(labelVal) as string;
                        if (!string.IsNullOrEmpty(v))
                            finalLabel = v;
                    }
                }
            }

            if (string.IsNullOrEmpty(finalLabel))
                finalLabel = string.IsNullOrEmpty(portName) ? targetNode.name : portName;

            if (_debugGraphParsing)
            {
                var resolved = labelVal is string s
                    ? (string.IsNullOrEmpty(s) ? "(empty)" : s)
                    : (labelVal == null ? "(null)" : labelVal.ToString());
                Debug.Log(
                    $"Conversation.GetDataFromGraph: choice port='{portName}' matchedMember='{matchedMemberName ?? "(none)"}' labelVal='{resolved}' finalLabel='{finalLabel}' targetNode='{targetNode?.name ?? "(null)"}'"
                );
            }

            return finalLabel;
        }

        // Node runtime info populated by GetDataFromGraph
        public class NodeData
        {
            public int id;
            public string name;
            public XNode.Node node;
            public ConversationLayer conversationLayer;
            public List<ChoiceData> choices;
            public int nextTargetId = int.MinValue;
            public int incomingCount = 0;
        }

        public class ChoiceData
        {
            public string portName;
            public int targetNodeId;
            public string targetNodeName;
            public string choiceText;
            public string label;
        }

        // Public accessor to ensure graph data is prepared and returned to callers
        public Dictionary<int, NodeData> GetGraphNodes()
        {
            // Always rebuild runtime node data to avoid holding references to destroyed editor nodes.
            GetDataFromGraph();
            return _graphNodes;
        }

        // Returns names of entry nodes (nodes with no incoming ConversationFlow connections)
        public System.Collections.Generic.List<string> GetGraphEntryNodeNames()
        {
            var entries = new System.Collections.Generic.List<string>();
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
                if (nd.node is Turnroot.Conversations.Branching.ConversationNode conv)
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
