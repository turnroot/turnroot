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
                var node = ParseNode(kvp.Key, kvp.Value, conversationName);
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
            return index >= 0 ? line.Substring(0, index) : line;
        }

        private static string UnwrapNodeBody(string wrapped)
        {
            if (string.IsNullOrEmpty(wrapped) || wrapped.Length < 2)
            {
                return wrapped;
            }

            var open = wrapped[0];
            var close = wrapped[wrapped.Length - 1];
            if (
                (open == '[' && close == ']')
                || (open == '(' && close == ')')
                || (open == '{' && close == '}')
            )
            {
                return wrapped.Substring(1, wrapped.Length - 2).Trim();
            }

            return wrapped.Trim();
        }

        private static MermaidNode ParseNode(string id, string text, string conversationName)
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
                node.Kind = MermaidNodeKind.Anchor;
                node.ActionType = "Start";
                node.ActionTarget =
                    tail.Length > "Start".Length + 1 ? tail.Substring("Start".Length + 1) : null;
                return node;
            }

            // "End" is a Mermaid reserved word (closes subgraphs), so we use Finish/Complete.
            if (
                tail.StartsWith("Finish", StringComparison.OrdinalIgnoreCase)
                || tail.StartsWith("Complete", StringComparison.OrdinalIgnoreCase)
            )
            {
                node.Kind = MermaidNodeKind.Anchor;
                node.ActionType = "Finish";
                var prefixLength = tail.StartsWith("Finish", StringComparison.OrdinalIgnoreCase)
                    ? "Finish".Length
                    : "Complete".Length;
                node.ActionTarget =
                    tail.Length > prefixLength + 1 ? tail.Substring(prefixLength + 1) : null;
                return node;
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

            // Default: Dialogue with Speaker_Emotion.
            node.Kind = MermaidNodeKind.Dialogue;
            ParseDialogueNode(tail, node);
            return node;
        }

        private static void ParseDialogueNode(string tail, MermaidNode node)
        {
            var segments = tail.Split('_');
            if (segments.Length >= 2)
            {
                node.Emotion = segments[^1];
                node.Speaker = string.Join("_", segments.Take(segments.Length - 1));
            }
            else
            {
                node.Speaker = tail;
                node.Emotion = "default";
            }
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
                    var possibleStrength = rawTarget.Substring(lastUnderscore + 1);
                    var parsed = ParseStrengthFromId(possibleStrength, out _);
                    if (!string.IsNullOrEmpty(parsed))
                    {
                        strength = parsed;
                        rawTarget = rawTarget.Substring(0, lastUnderscore);
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
                var possibleStrength = rawTarget.Substring(targetLastUnderscore + 1);
                var parsed = ParseStrengthFromId(possibleStrength, out _);
                if (!string.IsNullOrEmpty(parsed))
                {
                    node.ActionStrength = parsed;
                    node.ActionTarget = rawTarget.Substring(0, targetLastUnderscore);
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
            if (upperText.Contains("GAIN") && !upperText.Contains("LOSE"))
            {
                return SupportChangeOperation.Gain;
            }

            if (upperText.Contains("LOSE") && !upperText.Contains("GAIN"))
            {
                return SupportChangeOperation.Lose;
            }

            return null;
        }

        private static string ParseStrengthFromId(string segment, out string baseSegment)
        {
            baseSegment = segment;
            var upper = segment.ToUpperInvariant();

            if (upper.EndsWith("PLUSPLUS") || upper.EndsWith("PP"))
            {
                baseSegment = segment.Substring(
                    0,
                    segment.Length - GetSuffixLength(upper, "PLUSPLUS", "PP")
                );
                return "++";
            }

            if (upper.EndsWith("PLUS") || upper.EndsWith("P"))
            {
                baseSegment = segment.Substring(
                    0,
                    segment.Length - GetSuffixLength(upper, "PLUS", "P")
                );
                return "+";
            }

            if (upper.EndsWith("MINUSMINUS") || upper.EndsWith("MM"))
            {
                baseSegment = segment.Substring(
                    0,
                    segment.Length - GetSuffixLength(upper, "MINUSMINUS", "MM")
                );
                return "--";
            }

            if (upper.EndsWith("MINUS") || upper.EndsWith("M"))
            {
                baseSegment = segment.Substring(
                    0,
                    segment.Length - GetSuffixLength(upper, "MINUS", "M")
                );
                return "-";
            }

            return null;
        }

        private static int GetSuffixLength(string upper, string word, string abbr)
        {
            if (upper.EndsWith(word))
                return word.Length;
            if (upper.EndsWith(abbr))
                return abbr.Length;
            return 0;
        }

        private static string ParseStrengthFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var upper = text.ToUpperInvariant();
            if (upper.Contains("++"))
                return "++";
            if (upper.Contains("--"))
                return "--";
            if (upper.Contains('+'))
                return "+";
            if (upper.Contains('-'))
                return "-";

            return null;
        }
    }
}
