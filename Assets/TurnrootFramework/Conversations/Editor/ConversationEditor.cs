using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Conversations.Mermaid.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="Conversation"/> assets using Mermaid source files.
    /// </summary>
    [CustomEditor(typeof(Conversation))]
    public class ConversationEditor : UnityEditor.Editor
    {
        private readonly List<string> _validationWarnings = new();
        private bool _showValidation;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var conversation = (Conversation)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Mermaid Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("<MermaidSource>k__BackingField"),
                new GUIContent("Mermaid File"),
                true
            );

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Parse & Update People", GUILayout.Height(28)))
            {
                ParseAndUpdatePeople(conversation);
            }

            GUI.enabled = conversation.MermaidSource != null;
            if (GUILayout.Button("Validate Only", GUILayout.Height(28)))
            {
                ValidateOnly(conversation);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Conversation People", EditorStyles.boldLabel);
            var peopleProperty = serializedObject.FindProperty("<People>k__BackingField");
            EditorGUILayout.PropertyField(peopleProperty, new GUIContent("People"), true);

            if (_validationWarnings.Count > 0)
            {
                EditorGUILayout.Space(10);
                _showValidation = EditorGUILayout.Foldout(
                    _showValidation,
                    $"Validation ({_validationWarnings.Count})",
                    true,
                    EditorStyles.foldoutHeader
                );
                if (_showValidation)
                {
                    EditorGUI.indentLevel++;
                    foreach (var warning in _validationWarnings)
                    {
                        EditorGUILayout.HelpBox(warning, MessageType.Warning);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ParseAndUpdatePeople(Conversation conversation)
        {
            _validationWarnings.Clear();

            if (conversation.MermaidSource == null)
            {
                EditorUtility.DisplayDialog(
                    "Parse Conversation",
                    "Assign a Mermaid TextAsset first.",
                    "OK"
                );
                return;
            }

            var graph = MermaidConversationParser.Parse(
                conversation.MermaidSource.text,
                conversation.name
            );

            var speakers = graph
                .Nodes.Where(n => n.Kind == MermaidNodeKind.Dialogue)
                .Select(n => n.Speaker)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            var people = conversation.People ?? new List<ConversationPerson>();

            foreach (var speaker in speakers)
            {
                var existing = people.FirstOrDefault(p =>
                    string.Equals(p.SpeakerName, speaker, StringComparison.OrdinalIgnoreCase)
                );

                if (existing != null)
                {
                    continue;
                }

                var character = TryFindCharacterByDisplayName(speaker);
                people.Add(new ConversationPerson { SpeakerName = speaker, Character = character });
            }

            conversation.People = people;
            EditorUtility.SetDirty(conversation);

            ValidateGraph(conversation, graph);

            EditorUtility.DisplayDialog(
                "Parse Conversation",
                $"Found {speakers.Count} speaker(s) and updated the People list.",
                "OK"
            );
        }

        private void ValidateOnly(Conversation conversation)
        {
            _validationWarnings.Clear();
            var graph = MermaidConversationParser.Parse(
                conversation.MermaidSource.text,
                conversation.name
            );
            ValidateGraph(conversation, graph);
        }

        private void ValidateGraph(Conversation conversation, MermaidConversationGraph graph)
        {
            _validationWarnings.Clear();

            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));
            var unknownTargets = graph
                .Edges.Where(e => !nodeIds.Contains(e.ToId))
                .Select(e => e.ToId)
                .Distinct()
                .ToList();

            foreach (var target in unknownTargets)
            {
                _validationWarnings.Add($"Edge points to unknown node '{target}'.");
            }

            foreach (var node in graph.Nodes)
            {
                var idSegments = node.Id.Split('_');
                if (idSegments.Any(s => s.Equals("End", StringComparison.OrdinalIgnoreCase)))
                {
                    _validationWarnings.Add(
                        $"Node '{node.Id}' uses 'End' in its id. 'End' is a Mermaid reserved word; use 'Finish' instead."
                    );
                }

                if (
                    node.Kind == MermaidNodeKind.Dialogue
                    && string.IsNullOrWhiteSpace(node.Speaker)
                )
                {
                    _validationWarnings.Add($"Dialogue node '{node.Id}' has no speaker.");
                }

                if (node.Kind == MermaidNodeKind.Dialogue)
                {
                    var person = conversation.People?.FirstOrDefault(p =>
                        string.Equals(
                            p.SpeakerName,
                            node.Speaker,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                    if (person?.Character == null)
                    {
                        _validationWarnings.Add(
                            $"Speaker '{node.Speaker}' (node '{node.Id}') is not mapped to a CharacterData asset."
                        );
                    }
                }

                if (node.Kind == MermaidNodeKind.Action)
                {
                    if (string.IsNullOrWhiteSpace(node.ActionType))
                    {
                        _validationWarnings.Add($"Action node '{node.Id}' has no action type.");
                    }
                    else if (
                        !IsKnownActionType(node.ActionType)
                        && !node.ActionType.ToUpperInvariant().Contains("SUPPORT")
                    )
                    {
                        _validationWarnings.Add(
                            $"Action node '{node.Id}' uses unknown action type '{node.ActionType}'."
                        );
                    }
                }
            }

            var entries = graph.GetEntryNodes();
            if (entries.Count == 0 && graph.Nodes.Count > 0)
            {
                _validationWarnings.Add(
                    "No entry node found. Add a PART<N>_Start node. Avoid PART<N>_End — 'End' is a Mermaid reserved word; use PART<N>_Finish instead."
                );
            }
        }

        private static bool IsKnownActionType(string actionType)
        {
            var upper = actionType.ToUpperInvariant();
            return upper == "GAINSUPPORT" || upper == "LOSESUPPORT" || upper == "UNLOCKBATTLE";
        }

        private static CharacterData TryFindCharacterByDisplayName(string speakerName)
        {
            if (string.IsNullOrWhiteSpace(speakerName))
            {
                return null;
            }

            var all = Resources.LoadAll<CharacterData>("");
            return all.FirstOrDefault(c =>
                string.Equals(c.DisplayName, speakerName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.name, speakerName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.FullName, speakerName, StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}
