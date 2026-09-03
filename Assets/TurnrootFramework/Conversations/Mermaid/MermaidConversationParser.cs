using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Turnroot.Utilities;

namespace Turnroot.Conversations.Mermaid
{
    /// <summary>
    /// Parses a Mermaid flowchart into a runtime <see cref="MermaidConversationGraph"/>.
    /// </summary>
    public static class MermaidConversationParser
    {
        // Mermaid node IDs are alphanumeric plus underscores and hyphens only.
        // Strength symbols (++/--) and the reserved word "end" must not appear in IDs.
        private static readonly Regex NodeDefinitionRegex = new(
            @"^\s*([A-Za-z0-9_\-]+)\s*(\[(?:[^\[\]]|\\\[|\\\])*\]|\((?:[^\(\)]|\\\(|\\\))*\)|\{(?:[^\{\}]|\\\{|\\\})*\})\s*$",
            RegexOptions.Compiled
        );

        private static readonly Regex EdgeRegex = new(
            @"([A-Za-z0-9_\-]+)\s*-->(?:\|([^|]*)\|)?\s*([A-Za-z0-9_\-]+)",
            RegexOptions.Compiled
        );

        private static readonly Regex PartPrefixRegex = new(
            @"^PART(?<part>\d+)_(?<tail>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Dialogue tails must be: <Speaker>_<Emotion>-<Descriptor> with no extra
        // underscores or hyphens. Multi-word speaker names remove spaces instead of
        // using underscores.
        private static readonly Regex StrictDialogueTailRegex = new(
            @"^[A-Za-z0-9]+_[A-Za-z0-9]+-[A-Za-z0-9]+$",
            RegexOptions.Compiled
        );

        public static MermaidConversationGraph Parse(string mermaidText, string conversationName)
        {
            var graph = new MermaidConversationGraph();
            if (string.IsNullOrWhiteSpace(mermaidText))
            {
                $"MermaidConversationParser: source is empty for conversation '{conversationName}'.".LogWarning();
                return graph;
            }

            var lines = mermaidText.Split('\n');
            var nodeTexts = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var rawLine in lines)
            {
                var line = StripComment(rawLine).Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Skip Mermaid chart declarations.
                if (
                    line.StartsWith("flowchart", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("graph", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("%%", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                var nodeMatch = NodeDefinitionRegex.Match(line);
                if (nodeMatch.Success)
                {
                    var id = nodeMatch.Groups[1].Value.Trim();
                    var text = UnwrapNodeBody(nodeMatch.Groups[2].Value);

                    if (nodeTexts.ContainsKey(id))
                    {
                        $"MermaidConversationParser: duplicate node id '{id}' in '{conversationName}'. Each node must have a unique ID.".LogWarning();
                    }

                    nodeTexts[id] = text;
                }

                var edgeMatches = EdgeRegex.Matches(line);
                foreach (Match edgeMatch in edgeMatches)
                {
                    var fromId = edgeMatch.Groups[1].Value.Trim();
                    var label = edgeMatch.Groups[2].Success
                        ? edgeMatch.Groups[2].Value.Trim()
                        : null;
                    var toId = edgeMatch.Groups[3].Value.Trim();

                    if (
                        string.Equals(fromId, toId, StringComparison.Ordinal)
                        || string.IsNullOrEmpty(fromId)
                        || string.IsNullOrEmpty(toId)
                    )
                    {
                        continue;
                    }

                    graph.Edges.Add(
                        new MermaidEdge
                        {
                            FromId = fromId,
                            ToId = toId,
                            Label = label,
                        }
                    );
                }
            }

            foreach (var kvp in nodeTexts)
            {
                var node = ParseNode(kvp.Key, kvp.Value, conversationName, graph);
                if (node != null)
                {
                    graph.Nodes.Add(node);
                }
            }

            // Validate edge targets exist.
            var nodeIds = new HashSet<string>(
                graph.Nodes.Select(n => n.Id),
                StringComparer.Ordinal
            );
            foreach (var edge in graph.Edges)
            {
                if (!nodeIds.Contains(edge.ToId))
                {
                    $"MermaidConversationParser: edge from '{edge.FromId}' points to unknown node '{edge.ToId}' in '{conversationName}'.".LogWarning();
                }
            }

            return graph;
        }

        private static string StripComment(string line)
        {
            var index = line.IndexOf("%%", StringComparison.Ordinal);
            return index >= 0 ? line[..index] : line;
        }

        private static string UnwrapNodeBody(string wrapped)
        {
            if (string.IsNullOrEmpty(wrapped) || wrapped.Length < 2)
            {
                return wrapped;
            }

            var open = wrapped[0];
            var close = wrapped[wrapped.Length - 1];
            return
                (open == '[' && close == ']')
                || (open == '(' && close == ')')
                || (open == '{' && close == '}')
                ? wrapped[1..^1].Trim()
                : wrapped.Trim();
        }

        private static MermaidNode ParseNode(
            string id,
            string text,
            string conversationName,
            MermaidConversationGraph graph
        )
        {
            var partMatch = PartPrefixRegex.Match(id);
            if (!partMatch.Success)
            {
                $"MermaidConversationParser: node id '{id}' does not start with 'PART<N>_' in '{conversationName}'. Skipping.".LogWarning();
                return null;
            }

            var node = new MermaidNode
            {
                Id = id,
                PartNumber = int.Parse(partMatch.Groups["part"].Value),
                Text = text,
            };

            var tail = partMatch.Groups["tail"].Value;

            if (tail.StartsWith("Start", StringComparison.OrdinalIgnoreCase))
            {
                node.Kind = MermaidNodeKind.Start;
                node.ActionType = "Start";
                node.ActionTarget =
                    tail.Length > "Start".Length + 1 ? tail[("Start".Length + 1)..] : null;
                return node;
            }

            if (
                tail.StartsWith("Finish", StringComparison.OrdinalIgnoreCase)
                || tail.StartsWith("Complete", StringComparison.OrdinalIgnoreCase)
            )
            {
                $"MermaidConversationParser: '{tail}' is no longer supported. Finish/Complete nodes were removed; end a conversation by leaving the last node with no outgoing arrows.".LogWarning();
                return null;
            }

            var segments = tail.Split('_');
            var kindSegment = segments.Length > 0 ? segments[0] : string.Empty;

            if (kindSegment.Equals("Choice", StringComparison.OrdinalIgnoreCase))
            {
                node.Kind = MermaidNodeKind.Choice;
                node.ActionTarget = segments.Length > 1 ? string.Join("_", segments.Skip(1)) : null;
                return node;
            }

            if (kindSegment.Equals("Action", StringComparison.OrdinalIgnoreCase))
            {
                node.Kind = MermaidNodeKind.Action;
                ParseActionNode(segments, text, node);
                return node;
            }

            if (kindSegment.Equals("Condition", StringComparison.OrdinalIgnoreCase))
            {
                node.Kind = MermaidNodeKind.Condition;
                node.ActionTarget = segments.Length > 1 ? string.Join("_", segments.Skip(1)) : null;
                return node;
            }

            if (kindSegment.Equals("Signal", StringComparison.OrdinalIgnoreCase))
            {
                node.Kind = MermaidNodeKind.Signal;
                node.ActionTarget = segments.Length > 1 ? string.Join("_", segments.Skip(1)) : null;
                return node;
            }

            // Default: Dialogue with Speaker_Emotion-Descriptor.
            node.Kind = MermaidNodeKind.Dialogue;
            ParseDialogueNode(tail, node, conversationName, graph);
            return node;
        }

        private static void ParseDialogueNode(
            string tail,
            MermaidNode node,
            string conversationName,
            MermaidConversationGraph graph
        )
        {
            if (!StrictDialogueTailRegex.IsMatch(tail))
            {
                var message =
                    $"MermaidConversationParser: dialogue node '{node.Id}' has an invalid ID format in '{conversationName}'. "
                    + "Expected 'PART<N>_<Speaker>_<Emotion>-<Descriptor>' with exactly one underscore and one hyphen after the part number. "
                    + "Multi-word speaker names must have spaces removed, not replaced with underscores.";
                message.LogError();
                graph.Errors.Add(message);
                node.Speaker = tail;
                node.Emotion = "default";
                return;
            }

            var underscoreIndex = tail.IndexOf('_');
            var hyphenIndex = tail.IndexOf('-');
            var speaker = tail[..underscoreIndex];
            var emotion = tail[(underscoreIndex + 1)..hyphenIndex];
            var descriptor = tail[(hyphenIndex + 1)..];

            if (string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(emotion))
            {
                var message =
                    $"MermaidConversationParser: dialogue node '{node.Id}' has an empty speaker or emotion in '{conversationName}'.";
                message.LogError();
                graph.Errors.Add(message);
            }

            if (string.IsNullOrWhiteSpace(descriptor))
            {
                var message =
                    $"MermaidConversationParser: dialogue node '{node.Id}' has an empty descriptor in '{conversationName}'.";
                message.LogError();
                graph.Errors.Add(message);
            }

            node.Speaker = speaker;
            node.Emotion = emotion;
        }

        private static void ParseActionNode(string[] segments, string text, MermaidNode node)
        {
            // Action id format: Action_<ActionType>[_<Strength>]_<Target>
            // e.g. Action_GainSupport_Aubrey
            //      Action_GainSupport_Aubrey_PP
            //      Action_GainSupportPlusPlus_Aubrey
            //      Action_UnlockBattle_TakeOutTheTrash
            if (segments.Length < 3)
            {
                node.ActionType = segments.Length > 1 ? segments[1] : null;
                node.ActionTarget = null;
                return;
            }

            var typeSegment = segments[1];
            var rawTarget = string.Join("_", segments.Skip(2));

            // Check for explicit strength suffix on the action type itself.
            var strengthFromType = ParseStrengthFromId(typeSegment, out var baseType);

            // Support operations: GainSupport/GainSupportPlusPlus/GainSupportPP etc.
            var supportOp = ParseSupportOperation(baseType, text);
            if (supportOp.HasValue)
            {
                node.ActionType =
                    supportOp.Value == SupportChangeOperation.Gain ? "GainSupport" : "LoseSupport";

                // Prefer explicit strength in the ID; fall back to symbols in the body text.
                var strength = !string.IsNullOrEmpty(strengthFromType)
                    ? strengthFromType
                    : ParseStrengthFromText(text);

                // Try to strip a trailing strength suffix from the target.
                var lastUnderscore = rawTarget.LastIndexOf('_');
                if (lastUnderscore > 0)
                {
                    var possibleStrength = rawTarget[(lastUnderscore + 1)..];
                    var parsed = ParseStrengthFromId(possibleStrength, out _);
                    if (!string.IsNullOrEmpty(parsed))
                    {
                        strength = parsed;
                        rawTarget = rawTarget[..lastUnderscore];
                    }
                }

                node.ActionStrength = strength;
                node.ActionTarget = rawTarget;
                return;
            }

            // Non-support action.
            node.ActionType = baseType;
            node.ActionStrength = strengthFromType;

            var targetLastUnderscore = rawTarget.LastIndexOf('_');
            if (targetLastUnderscore > 0)
            {
                var possibleStrength = rawTarget[(targetLastUnderscore + 1)..];
                var parsed = ParseStrengthFromId(possibleStrength, out _);
                if (!string.IsNullOrEmpty(parsed))
                {
                    node.ActionStrength = parsed;
                    node.ActionTarget = rawTarget[..targetLastUnderscore];
                    return;
                }
            }

            node.ActionTarget = rawTarget;
        }

        private static SupportChangeOperation? ParseSupportOperation(
            string typeSegment,
            string text
        )
        {
            var upperType = typeSegment.ToUpperInvariant();
            var upperText = text.ToUpperInvariant();

            var isGain = upperType.Contains("GAIN");
            var isLose = upperType.Contains("LOSE");

            if (isGain && !isLose)
            {
                return SupportChangeOperation.Gain;
            }

            if (isLose && !isGain)
            {
                return SupportChangeOperation.Lose;
            }

            // If the type itself is ambiguous, inspect the body text.
            return upperText.Contains("GAIN") && !upperText.Contains("LOSE")
                    ? SupportChangeOperation.Gain
                : upperText.Contains("LOSE") && !upperText.Contains("GAIN")
                    ? SupportChangeOperation.Lose
                : null;
        }

        private static string ParseStrengthFromId(string segment, out string baseSegment)
        {
            baseSegment = segment;
            var upper = segment.ToUpperInvariant();

            if (upper.EndsWith("PLUSPLUS") || upper.EndsWith("PP"))
            {
                baseSegment = segment[..^(GetSuffixLength(upper, "PLUSPLUS", "PP"))];
                return "++";
            }

            if (upper.EndsWith("PLUS") || upper.EndsWith("P"))
            {
                baseSegment = segment[..^(GetSuffixLength(upper, "PLUS", "P"))];
                return "+";
            }

            if (upper.EndsWith("MINUSMINUS") || upper.EndsWith("MM"))
            {
                baseSegment = segment[..^(GetSuffixLength(upper, "MINUSMINUS", "MM"))];
                return "--";
            }

            if (upper.EndsWith("MINUS") || upper.EndsWith("M"))
            {
                baseSegment = segment[..^(GetSuffixLength(upper, "MINUS", "M"))];
                return "-";
            }

            return null;
        }

        private static int GetSuffixLength(string upper, string word, string abbr) =>
            upper.EndsWith(word) ? word.Length
            : upper.EndsWith(abbr) ? abbr.Length
            : 0;

        private static string ParseStrengthFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var upper = text.ToUpperInvariant();
            return upper.Contains("++") ? "++"
                : upper.Contains("--") ? "--"
                : upper.Contains('+') ? "+"
                : upper.Contains('-') ? "-"
                : null;
        }
    }
}
