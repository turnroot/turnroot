using System;
using System.Collections.Generic;
using Turnroot.Skills.Nodes;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Turnroot.Conversations.Branching.Nodes
{
    [CustomNodeGraphEditor(typeof(ConversationGraph))]
    public class ConversationGraphEditor : NodeGraphEditor
    {
        // Helper: build a GenericMenu containing only CreateNodeMenu entries that start with "Conversation/"
        private GenericMenu BuildConversationMenu()
        {
            var menu = new GenericMenu();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = null;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }
                foreach (var t in types)
                {
                    if (t == null)
                        continue;
                    if (!typeof(Node).IsAssignableFrom(t))
                        continue;
                    if (t.IsAbstract)
                        continue;

                    // Inspect custom attribute data to find CreateNodeMenu attribute's path
                    foreach (var cad in t.GetCustomAttributesData())
                    {
                        if (cad.AttributeType.Name != "CreateNodeMenuAttribute")
                            continue;
                        if (cad.ConstructorArguments.Count == 0)
                            continue;
                        var arg = cad.ConstructorArguments[0].Value as string;
                        if (string.IsNullOrEmpty(arg))
                            continue;
                        if (!arg.StartsWith("Conversation/"))
                            continue;

                        // Use the attribute's path as the label so nested paths are preserved
                        string label = arg.Substring("Conversation/".Length);
                        // Keep subfolders in label - GenericMenu will render them as nested items if label contains '/'
                        // Capture locals for closure
                        string menuArg = arg;
                        string menuLabel = label;
                        menu.AddItem(
                            new GUIContent(menuLabel),
                            false,
                            () =>
                            {
                                var graph = target as NodeGraph;
                                if (graph == null)
                                    return;
                                var created = graph.AddNode(t);
                                // Create a short nickname from the full path: use last segment and strip trailing " Node" or "Node"
                                if (created != null)
                                {
                                    string shortName = menuArg;
                                    int lastSlash = shortName.LastIndexOf('/');
                                    if (lastSlash >= 0 && lastSlash < shortName.Length - 1)
                                        shortName = shortName.Substring(lastSlash + 1);
                                    // Strip common suffixes
                                    if (shortName.EndsWith(" Node", StringComparison.Ordinal))
                                    {
                                        int newLen = shortName.Length - " Node".Length;
                                        shortName = shortName.Substring(0, newLen);
                                    }
                                    else if (shortName.EndsWith("Node", StringComparison.Ordinal))
                                    {
                                        int newLen = shortName.Length - "Node".Length;
                                        shortName = shortName.Substring(0, newLen);
                                    }
                                    shortName = shortName.Trim();
                                    created.name = shortName;
                                    UnityEditor.EditorUtility.SetDirty(created);
                                }
                                if (NodeEditorWindow.current != null)
                                    NodeEditorWindow.current.Repaint();
                            }
                        );
                        break;
                    }
                }
            }

            return menu;
        }

        public override void OnGUI()
        {
            window.titleContent = new GUIContent("Conversation Graph Editor");
            // Top toolbar: Add Conversation dropdown grouped by subtype/folder
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            // Create a fixed rect for the button so we can reliably position the dropdown under it
            Rect convoBtnRect = GUILayoutUtility.GetRect(
                180,
                EditorGUIUtility.singleLineHeight,
                GUILayout.Width(180)
            );
            if (GUI.Button(convoBtnRect, "Add Conversation ▾", EditorStyles.toolbarButton))
            {
                var menu = BuildConversationMenu();
                // Show dropdown directly under the button
                menu.DropDown(new Rect(convoBtnRect.x, convoBtnRect.y + convoBtnRect.height, 0, 0));
            }
            EditorGUILayout.EndHorizontal();

            // Intercept right-click and show a conversation-scoped "Add Node" menu.
            Event evt = Event.current;
            bool isRightClick =
                evt != null
                && (
                    evt.type == EventType.ContextClick
                    || (evt.type == EventType.MouseUp && evt.button == 1)
                );
            if (isRightClick)
            {
                var menu = BuildConversationMenu();
                menu.ShowAsContext();
                evt.Use();
            }

            base.OnGUI();

            // Handle delete/backspace key for selected nodes
            Event e = Event.current;
            if (
                e.type == EventType.KeyDown
                && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            )
            {
                // Get all selected nodes
                foreach (var node in Selection.objects)
                {
                    if (node is Node xNode && target.nodes.Contains(xNode))
                    {
                        // Remove the node from the graph
                        target.RemoveNode(xNode);
                    }
                }

                // Mark the event as used
                e.Use();

                // Repaint the window
                window.Repaint();
            }
        }
    }
}
