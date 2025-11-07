using System;
using System.Collections.Generic;
using Assets.Prototypes.Skills.Nodes;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Assets.Prototypes.Skills.Nodes.Editor
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
