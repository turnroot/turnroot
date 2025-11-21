using System;
using System.Collections.Generic;
using Turnroot.Skills.Nodes;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Turnroot.Skills.Nodes.Editor
{
    /// <summary>
    /// Custom NodeGraphEditor that overrides port colors for skill node socket types.
    /// Colors from Tailwind CSS 500 shades.
    /// </summary>
    [CustomNodeGraphEditor(typeof(SkillGraph))]
    public class SkillGraphEditor : NodeGraphEditor
    {
        public override void OnGUI()
        {
            // Top toolbar: Add Skill dropdown grouped by category (uses CreateNodeMenu paths)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            // Top-level skill categories - render one dropdown button per category
            string[] categories = new string[] { "Flow", "Math", "Events", "Conditions" };
            foreach (var cat in categories)
            {
                Rect btnRect = GUILayoutUtility.GetRect(
                    110,
                    EditorGUIUtility.singleLineHeight,
                    GUILayout.Width(110)
                );
                if (GUI.Button(btnRect, cat + " ▾", EditorStyles.toolbarButton))
                {
                    var menu = BuildSkillMenu(cat);
                    menu.DropDown(new Rect(btnRect.x, btnRect.y + btnRect.height, 0, 0));
                }
                GUILayout.Space(4);
            }

            EditorGUILayout.EndHorizontal();

            // Right-click context menu offering skill nodes
            Event evt = Event.current;
            bool isRightClick =
                evt != null
                && (
                    evt.type == EventType.ContextClick
                    || (evt.type == EventType.MouseUp && evt.button == 1)
                );
            if (isRightClick)
            {
                var menu = BuildSkillMenu();
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
                        // Ensure any subasset saved for the node is removed as well
                        try
                        {
                            var nodePath = AssetDatabase.GetAssetPath(xNode as UnityEngine.Object);
                            if (!string.IsNullOrEmpty(nodePath))
                                AssetDatabase.RemoveObjectFromAsset(xNode as UnityEngine.Object);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning(
                                $"SkillGraphEditor: failed to remove node subasset: {ex.Message}"
                            );
                        }
                    }
                }

                // Mark the event as used
                e.Use();

                // Repaint the window
                window.Repaint();
            }
        }

        private GenericMenu BuildSkillMenu(string categoryPrefix = null)
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

                    // Only include nodes that derive from SkillNode (covers types with or without namespaces)
                    if (!typeof(SkillNode).IsAssignableFrom(t))
                        continue;

                    foreach (var cad in t.GetCustomAttributesData())
                    {
                        if (cad.AttributeType.Name != "CreateNodeMenuAttribute")
                            continue;
                        if (cad.ConstructorArguments.Count == 0)
                            continue;
                        var arg = cad.ConstructorArguments[0].Value as string;
                        if (string.IsNullOrEmpty(arg))
                            continue;

                        // Optionally filter by top-level category prefix and strip it from the label
                        string label = arg;
                        if (!string.IsNullOrEmpty(categoryPrefix))
                        {
                            string prefix = categoryPrefix + "/";
                            if (!arg.StartsWith(prefix, StringComparison.Ordinal))
                                continue;
                            label = arg.Substring(prefix.Length);
                            if (string.IsNullOrEmpty(label))
                                label = arg; // fallback to full label if nothing left
                        }
                        // Capture for closure
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
                                if (created != null)
                                {
                                    string shortName = menuArg;
                                    int lastSlash = shortName.LastIndexOf('/');
                                    if (lastSlash >= 0 && lastSlash < shortName.Length - 1)
                                        shortName = shortName.Substring(lastSlash + 1);
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

        public override Color GetTypeColor(Type type)
        {
            // Try to get color from settings asset
            var settings = SkillGraphEditorSettings.Instance;
            if (settings != null)
            {
                return settings.GetColorForType(type);
            }

            // Fall back to default for other types
            return base.GetTypeColor(type);
        }
    }
}
