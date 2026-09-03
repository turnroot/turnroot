using System;
using System.Collections.Generic;
using System.Linq;

namespace Turnroot.Conversations.Mermaid
{
    /// <summary>
    /// Runtime representation of a parsed Mermaid conversation graph.
    /// </summary>
    [Serializable]
    public class MermaidConversationGraph
    {
        public List<MermaidNode> Nodes = new();
        public List<MermaidEdge> Edges = new();
        public List<string> Errors = new();

        [NonSerialized]
        private Dictionary<string, MermaidNode> _nodeLookup;

        [NonSerialized]
        private Dictionary<string, List<MermaidEdge>> _outgoing;

        [NonSerialized]
        private Dictionary<string, int> _incomingCounts;

        public MermaidNode GetNode(string id)
        {
            EnsureLookup();
            return id != null && _nodeLookup.TryGetValue(id, out var node) ? node : null;
        }

        public List<MermaidNode> GetEntryNodes()
        {
            EnsureLookup();
            var entries = new List<MermaidNode>();
            foreach (var node in Nodes)
            {
                if (node == null)
                {
                    continue;
                }

                var incoming = _incomingCounts.TryGetValue(node.Id, out var count) ? count : 0;
                if (incoming == 0 && node.Kind == MermaidNodeKind.Start)
                {
                    entries.Add(node);
                }
            }

            return entries;
        }

        public List<MermaidEdge> GetOutgoing(string id)
        {
            EnsureLookup();
            return id != null && _outgoing.TryGetValue(id, out var edges)
                ? edges
                : new List<MermaidEdge>();
        }

        public int GetIncomingCount(string id)
        {
            EnsureLookup();
            return id != null && _incomingCounts.TryGetValue(id, out var count) ? count : 0;
        }

        private void EnsureLookup()
        {
            if (_nodeLookup != null)
            {
                return;
            }

            _nodeLookup = new Dictionary<string, MermaidNode>(StringComparer.Ordinal);
            _outgoing = new Dictionary<string, List<MermaidEdge>>(StringComparer.Ordinal);
            _incomingCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var node in Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                _nodeLookup[node.Id] = node;
                _outgoing[node.Id] = new List<MermaidEdge>();
            }

            foreach (var edge in Edges)
            {
                if (edge == null || string.IsNullOrEmpty(edge.FromId))
                {
                    continue;
                }

                if (_outgoing.TryGetValue(edge.FromId, out var list))
                {
                    list.Add(edge);
                }

                if (!string.IsNullOrEmpty(edge.ToId))
                {
                    _incomingCounts[edge.ToId] = _incomingCounts.TryGetValue(edge.ToId, out var c)
                        ? c + 1
                        : 1;
                }
            }
        }
    }

    /// <summary>
    /// A single node inside a Mermaid conversation graph.
    /// </summary>
    [Serializable]
    public class MermaidNode
    {
        public string Id;
        public int PartNumber;
        public MermaidNodeKind Kind;
        public string Speaker;
        public string Emotion;
        public string ActionType;
        public string ActionTarget;
        public string ActionStrength;
        public string Text;

        public string ConditionName => Kind == MermaidNodeKind.Condition ? ActionTarget : null;
    }

    /// <summary>
    /// A directed edge between two Mermaid conversation nodes.
    /// </summary>
    [Serializable]
    public class MermaidEdge
    {
        public string FromId;
        public string ToId;
        public string Label;
    }

    public enum MermaidNodeKind
    {
        Dialogue,
        Choice,
        Action,
        Condition,
        Signal,
        Start,
    }

    public enum SupportChangeOperation
    {
        Gain,
        Lose,
    }

    public enum SupportChangeMagnitude
    {
        PlusPlus,
        Plus,
        MinusMinus,
        Minus,
    }

    /// <summary>
    /// Strongly-typed support change payload extracted from an action node.
    /// </summary>
    public readonly struct SupportChangeAction
    {
        public SupportChangeOperation Operation { get; }
        public SupportChangeMagnitude Magnitude { get; }
        public string TargetSpeaker { get; }

        public SupportChangeAction(
            SupportChangeOperation operation,
            SupportChangeMagnitude magnitude,
            string targetSpeaker
        )
        {
            Operation = operation;
            Magnitude = magnitude;
            TargetSpeaker = targetSpeaker;
        }
    }
}
