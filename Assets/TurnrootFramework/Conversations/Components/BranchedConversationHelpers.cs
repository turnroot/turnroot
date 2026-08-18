using System.Collections.Generic;
using System.Reflection;
using Turnroot.Utilities;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Utilities for building runtime node data from branching conversation graphs.
    /// </summary>
    public static class BranchedConversationHelpers
    {
        // Build runtime node structures from the XNode graph (two-pass: incoming counts, then node data)
        public static Dictionary<int, NodeData> GetDataFromGraph(
            Branching.Nodes.ConversationGraph conversationGraph
        )
        {
            if (conversationGraph == null)
            {
                return null;
            }

            try
            {
                var nodes = new Dictionary<int, NodeData>();

                var incomingCounts = BuildIncomingCounts(conversationGraph);

                foreach (var node in conversationGraph.nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    var nd = BuildNodeData(node, incomingCounts);
                    nodes[nd.id] = nd;
                }

                return nodes;
            }
            catch (System.Exception ex)
            {
                $"BranchedConversationHelpers.GetDataFromGraph: exception while parsing graph '{conversationGraph?.name}': {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}".LogError();
                return null;
            }
        }

        private static Dictionary<int, int> BuildIncomingCounts(
            Branching.Nodes.ConversationGraph conversationGraph
        )
        {
            var incomingCounts = new Dictionary<int, int>();
            foreach (var node in conversationGraph.nodes)
            {
                if (node == null)
                {
                    continue;
                }

                foreach (var port in node.Ports)
                {
                    if (port.direction != XNode.NodePort.IO.Output)
                    {
                        continue;
                    }

                    if (port.ValueType != typeof(Branching.ConversationFlow))
                    {
                        continue;
                    }

                    var conns = port.GetConnections();
                    if (conns == null)
                    {
                        continue;
                    }

                    foreach (var c in conns)
                    {
                        if (c.node == null)
                        {
                            continue;
                        }

                        var tid = c.node.GetInstanceID();
                        incomingCounts[tid] = !incomingCounts.TryGetValue(tid, out var cnt)
                            ? 1
                            : cnt + 1;
                    }
                }
            }

            return incomingCounts;
        }

        private static NodeData BuildNodeData(XNode.Node node, Dictionary<int, int> incomingCounts)
        {
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
            {
                nd.conversationLayer = conv.conversationLayer;
            }

            var outgoing = GatherOutgoingFlowConnections(node);

            if (outgoing.Count == 1)
            {
                nd.nextTargetId = outgoing[0].target.GetInstanceID();
            }
            else if (outgoing.Count > 1)
            {
                foreach (var (portName, targetNode) in outgoing)
                {
                    var choice = BuildChoiceData(node, portName, targetNode);
                    nd.choices.Add(choice);
                }
            }

            return nd;
        }

        private static List<(string portName, XNode.Node target)> GatherOutgoingFlowConnections(
            XNode.Node node
        )
        {
            var outgoing = new List<(string portName, XNode.Node target)>();

            foreach (var port in node.Ports)
            {
                if (port.direction != XNode.NodePort.IO.Output)
                {
                    continue;
                }

                if (port.ValueType != typeof(Branching.ConversationFlow))
                {
                    continue;
                }

                var conns = port.GetConnections();
                if (conns == null || conns.Count == 0)
                {
                    continue;
                }

                foreach (var c in conns)
                {
                    if (c.node == null)
                    {
                        continue;
                    }

                    outgoing.Add((port.fieldName ?? string.Empty, c.node));
                }
            }

            return outgoing;
        }

        private static ChoiceData BuildChoiceData(
            XNode.Node node,
            string portName,
            XNode.Node targetNode
        )
        {
            var choice = new ChoiceData
            {
                portName = portName,
                targetNodeId = targetNode.GetInstanceID(),
                targetNodeName = targetNode.name,
                label = ResolveLabelForPort(node, portName, targetNode),
            };
            choice.choiceText = choice.label;
            return choice;
        }

        // Resolve a choice label for a port. Returns the final label string (never null).
        public static string ResolveLabelForPort(
            XNode.Node node,
            string portName,
            XNode.Node targetNode
        )
        {
            // Null node -> fallback to target name or portName/Choice
            if (node == null)
            {
                return targetNode?.name ?? (string.IsNullOrEmpty(portName) ? "Choice" : portName);
            }

            if (string.IsNullOrEmpty(portName))
            {
                return targetNode?.name ?? "Choice";
            }

            // Try exact, then lower-first variant
            var candidates = new[] { portName, LowerFirst(portName) };
            foreach (var candidate in candidates)
            {
                if (TryGetStringMember(node, candidate, out var v))
                {
                    return v;
                }
            }

            // Case-insensitive scan
            if (TryGetStringMemberCaseInsensitive(node, portName, out var ci))
            {
                return ci;
            }

            // Final fallback
            return targetNode?.name ?? portName;
        }

        private static bool TryGetStringMember(object obj, string name, out string value)
        {
            value = null;
            var t = obj.GetType();

            var field = t.GetField(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (field != null && field.FieldType == typeof(string))
            {
                value = field.GetValue(obj) as string;
                return !string.IsNullOrEmpty(value);
            }

            var property = t.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (property != null && property.PropertyType == typeof(string))
            {
                value = property.GetValue(obj) as string;
                return !string.IsNullOrEmpty(value);
            }

            return false;
        }

        private static bool TryGetStringMemberCaseInsensitive(
            object obj,
            string portName,
            out string value
        )
        {
            value = null;
            var t = obj.GetType();

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
                    value = f.GetValue(obj) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        return true;
                    }
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
                    value = p.GetValue(obj) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string LowerFirst(string s) =>
            s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
    }

    /// <summary>
    /// Runtime data for a conversation graph node including choices, connections, and content.
    /// </summary>
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

    /// <summary>
    /// Represents a player choice in a branching conversation with text and target node information.
    /// </summary>
    public class ChoiceData
    {
        public string portName;
        public int targetNodeId;
        public string targetNodeName;
        public string choiceText;
        public string label;
    }
}
