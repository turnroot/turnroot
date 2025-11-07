using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Assets.Prototypes.Skills.Nodes.Editor
{
    /// <summary>
    /// Custom NodeEditor that applies category-based tinting to skill nodes.
    /// Automatically determines category from the script's subfolder path.
    /// </summary>
    [CustomNodeEditor(typeof(SkillNode))]
    public class SkillNodeEditor : NodeEditor
    {
        public override int GetWidth()
        {
            return 300;
        }

        public override void OnHeaderGUI()
        {
            // Draw the default header first (node title)
            base.OnHeaderGUI();

            // Check if the node has a NodeLabel attribute
            var nodeType = target.GetType();
            var labelAttr =
                System.Attribute.GetCustomAttribute(nodeType, typeof(NodeLabelAttribute))
                as NodeLabelAttribute;

            if (labelAttr != null)
            {
                // Create a word-wrapped style for the label
                GUIStyle labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                labelStyle.wordWrap = true;
                labelStyle.alignment = TextAnchor.UpperCenter;

                // Draw the custom label below the title with word wrap
                GUILayout.Label(labelAttr.Label, labelStyle);
            }
        }

        public override Color GetTint()
        {
            // Try to get colors from settings asset first
            var settings = SkillGraphEditorSettings.Instance;
            if (settings != null)
            {
                var script = MonoScript.FromScriptableObject(target);
                if (script != null)
                {
                    string scriptPath = AssetDatabase.GetAssetPath(script);
                    Color color = settings.GetColorForNodeCategory(scriptPath);

                    // Return the color from settings (bypassing fallback to NodeCategoryAttribute)
                    if (color != Color.gray)
                    {
                        return color;
                    }
                }
            }

            // Fall back to NodeCategoryAttribute if no settings
            var script2 = MonoScript.FromScriptableObject(target);
            if (script2 != null)
            {
                string scriptPath = AssetDatabase.GetAssetPath(script2);

                // Check if the path contains a category subfolder
                if (scriptPath.Contains("/Flow/"))
                {
                    return NodeCategoryAttribute.GetCategoryColor(NodeCategory.Flow);
                }
                else if (scriptPath.Contains("/Math/"))
                {
                    return NodeCategoryAttribute.GetCategoryColor(NodeCategory.Math);
                }
                else if (scriptPath.Contains("/Events/"))
                {
                    return NodeCategoryAttribute.GetCategoryColor(NodeCategory.Events);
                }
                else if (scriptPath.Contains("/Conditions/"))
                {
                    return NodeCategoryAttribute.GetCategoryColor(NodeCategory.Conditions);
                }
            }

            // Fall back to default tint
            return base.GetTint();
        }

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // Store original label width
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            // Set label width to 40% of the node width to give more space
            EditorGUIUtility.labelWidth = GetWidth() * 0.5f;

            // Check if this is a Flow node
            var script = MonoScript.FromScriptableObject(target);
            bool isFlowNode = false;
            bool isEventsNode = false;
            if (script != null)
            {
                string scriptPath = AssetDatabase.GetAssetPath(script);
                isFlowNode = scriptPath.Contains("/Flow/");
                isEventsNode = scriptPath.Contains("/Events/");
            }

            // If not a Flow node, we want to hide OnNodeExecute
            if (!(isFlowNode || isEventsNode))
            {
                // Draw all ports and properties except OnNodeExecute
                // Use reflection to get all port fields
                foreach (var port in target.Ports)
                {
                    NodeEditorGUILayout.PortField(port);
                }

                // Draw all serialized properties except internal ones and OnNodeExecute
                SerializedProperty iterator = serializedObject.GetIterator();
                iterator.NextVisible(true); // Skip script property
                while (iterator.NextVisible(false))
                {
                    // Skip xNode internal fields and OnNodeExecute
                    if (
                        iterator.name == "graph"
                        || iterator.name == "position"
                        || iterator.name == "ports"
                        || iterator.name == "OnNodeExecute"
                    )
                        continue;

                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
            else
            {
                // For Flow nodes, draw the default body (includes OnNodeExecute)
                base.OnBodyGUI();
            }

            // Restore original label width
            EditorGUIUtility.labelWidth = originalLabelWidth;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
