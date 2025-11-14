using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Turnroot.Conversations
{
    public static class BranchedConversationHelpers
    {
        // Build runtime node structures from the XNode graph (two-pass: incoming counts, then node data)
        public static Dictionary<int, NodeData> GetDataFromGraph(
            Branching.Nodes.ConversationGraph conversationGraph
        )
        {
            if (conversationGraph == null)
                return null;

            try
            {
                var nodes = new Dictionary<int, NodeData>();

                // --- Pass 1: incoming counts ---
                var incomingCounts = new Dictionary<int, int>();
                foreach (var node in conversationGraph.nodes)
                {
                    if (node == null)
                        continue;
                    foreach (var port in node.Ports)
                    {
                        if (port.direction != XNode.NodePort.IO.Output)
                            continue;
                        if (port.ValueType != typeof(Branching.ConversationFlow))
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
                foreach (var node in conversationGraph.nodes)
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

                    if (node is Branching.ConversationNode conv)
                        nd.conversationLayer = conv.conversationLayer;

                    // gather outgoing ConversationFlow connections
                    var outgoing = new List<(string portName, XNode.Node target)>();
                    foreach (var port in node.Ports)
                    {
                        if (port.direction != XNode.NodePort.IO.Output)
                            continue;
                        if (port.ValueType != typeof(Branching.ConversationFlow))
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

                return nodes;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"BranchedConversationHelpers.GetDataFromGraph: exception while parsing graph '{conversationGraph?.name}': {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}"
                );
                return null;
            }
        }

        // Resolve a choice label for a port. Returns the final label string (never null).
        public static string ResolveLabelForPort(
            XNode.Node node,
            string portName,
            XNode.Node targetNode
        )
        {
            if (node == null)
                return targetNode?.name ?? (string.IsNullOrEmpty(portName) ? "Choice" : portName);

            var t = node.GetType();

            if (string.IsNullOrEmpty(portName))
                return targetNode?.name ?? "Choice";

            // Try exact name, then lower-first variant
            var candidates = new List<string>
            {
                portName,
                char.ToLowerInvariant(portName[0]) + portName.Substring(1),
            };

            foreach (var name in candidates)
            {
                var fi = t.GetField(
                    name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );
                if (fi != null && fi.FieldType == typeof(string))
                {
                    var v = fi.GetValue(node) as string;
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }

                var pi = t.GetProperty(
                    name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );
                if (pi != null && pi.PropertyType == typeof(string))
                {
                    var v = pi.GetValue(node) as string;
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }
            }

            // Case-insensitive scan for a string field/property matching the port name
            foreach (
                var f in t.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                )
            )
            {
                if (
                    f.FieldType == typeof(string)
                    && string.Equals(f.Name, portName, System.StringComparison.OrdinalIgnoreCase)
                )
                {
                    var v = f.GetValue(node) as string;
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }
            }
            foreach (
                var p in t.GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                )
            )
            {
                if (
                    p.PropertyType == typeof(string)
                    && string.Equals(p.Name, portName, System.StringComparison.OrdinalIgnoreCase)
                )
                {
                    var v = p.GetValue(node) as string;
                    if (!string.IsNullOrEmpty(v))
                        return v;
                }
            }

            // Final fallback
            return targetNode?.name ?? portName;
        }
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
}
