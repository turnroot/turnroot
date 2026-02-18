using System;
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
                            var nodePath = AssetDatabase.GetAssetPath(xNode);
                            if (!string.IsNullOrEmpty(nodePath))
                            {
                                AssetDatabase.RemoveObjectFromAsset(xNode);
                            }
                        }
                        catch (Exception ex)
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
            var graph = target;

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
                    {
                        continue;
                    }

                    if (!typeof(Node).IsAssignableFrom(t))
                    {
                        continue;
                    }

                    if (t.IsAbstract)
                    {
                        continue;
                    }

                    // Only include nodes that derive from SkillNode (covers types with or without namespaces)
                    if (!typeof(SkillNode).IsAssignableFrom(t))
                    {
                        continue;
                    }

                    TryAddNodeTypeToMenu(t, menu, categoryPrefix, graph);
                }
            }

            return menu;
        }

        private static void TryAddNodeTypeToMenu(
            Type nodeType,
            GenericMenu menu,
            string categoryPrefix,
            NodeGraph graph
        )
        {
            foreach (var cad in nodeType.GetCustomAttributesData())
            {
                if (cad.AttributeType.Name != "CreateNodeMenuAttribute")
                {
                    continue;
                }

                if (cad.ConstructorArguments.Count == 0)
                {
                    continue;
                }

                var menuPath = cad.ConstructorArguments[0].Value as string;
                if (string.IsNullOrEmpty(menuPath))
                {
                    continue;
                }

                var label = GetFilteredMenuLabel(menuPath, categoryPrefix);
                if (label == null)
                {
                    continue; // Filtered out by category prefix
                }

                AddNodeCreationMenuItem(menu, nodeType, menuPath, label, graph);
                break;
            }
        }

        private static string GetFilteredMenuLabel(string menuPath, string categoryPrefix)
        {
            if (string.IsNullOrEmpty(categoryPrefix))
            {
                return menuPath;
            }

            string prefix = categoryPrefix + "/";
            if (!menuPath.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null; // Filtered out
            }

            string label = menuPath.Substring(prefix.Length);
            return string.IsNullOrEmpty(label) ? menuPath : label;
        }

        private static void AddNodeCreationMenuItem(
            GenericMenu menu,
            Type nodeType,
            string menuPath,
            string label,
            NodeGraph graph
        )
        {
            menu.AddItem(
                new GUIContent(label),
                false,
                () => CreateNodeInGraph(nodeType, menuPath, graph)
            );
        }

        private static void CreateNodeInGraph(Type nodeType, string menuPath, NodeGraph graph)
        {
            if (graph == null)
            {
                return;
            }

            var created = graph.AddNode(nodeType);
            if (created != null)
            {
                string shortName = ExtractShortNameFromMenuPath(menuPath);
                created.name = shortName;
                UnityEditor.EditorUtility.SetDirty(created);

                // Persist the created node as a sub-asset of the graph so the
                // graph's `nodes` list is serialized with non-zero fileIDs.
                try
                {
                    var graphPath = AssetDatabase.GetAssetPath(graph);
                    var createdPath = AssetDatabase.GetAssetPath(created);
                    if (!string.IsNullOrEmpty(graphPath))
                    {
                        if (string.IsNullOrEmpty(createdPath) || createdPath != graphPath)
                        {
                            AssetDatabase.AddObjectToAsset(created, graphPath);
                        }

                        UnityEditor.EditorUtility.SetDirty(graph);
                        AssetDatabase.SaveAssets();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"SkillGraphEditor: failed to persist created node as subasset: {ex.Message}"
                    );
                }
            }

            if (NodeEditorWindow.current != null)
            {
                NodeEditorWindow.current.Repaint();
            }
        }

        private static string ExtractShortNameFromMenuPath(string menuPath)
        {
            string shortName = menuPath;

            // Extract last segment after slash
            int lastSlash = shortName.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < shortName.Length - 1)
            {
                shortName = shortName.Substring(lastSlash + 1);
            }

            // Remove "Node" suffix (with or without space)
            if (shortName.EndsWith(" Node", StringComparison.Ordinal))
            {
                shortName = shortName.Substring(0, shortName.Length - " Node".Length);
            }
            else if (shortName.EndsWith("Node", StringComparison.Ordinal))
            {
                shortName = shortName.Substring(0, shortName.Length - "Node".Length);
            }

            return shortName.Trim();
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
