using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Turnroot.Skills.Nodes.Editor
{
    /// <summary>
    /// Custom NodeEditor that applies category-based tinting to skill nodes.
    /// Automatically determines category from the script's subfolder path.
    /// </summary>
    [CustomNodeEditor(typeof(SkillNode))]
    public class SkillNodeEditor : NodeEditor
    {
        // Cached per-editor-instance values — populated once on first use and
        // cleared when the target changes (OnEnable / etc.).
        private string _cachedScriptPath;
        private bool _scriptPathResolved;
        private bool _isFlowNode;
        private bool _isEventsNode;
        private Color _cachedTint;
        private bool _tintCached;

        // Make sure we clear caches if the editor is reused for a different node.
        public override void OnEnable()
        {
            base.OnEnable();
            InvalidateCaches();
        }

        private void InvalidateCaches()
        {
            _scriptPathResolved = false;
            _tintCached = false;
            _cachedScriptPath = null;
        }

        private string GetScriptPath()
        {
            if (_scriptPathResolved)
            {
                return _cachedScriptPath;
            }

            _scriptPathResolved = true;
            var script = MonoScript.FromScriptableObject(target);
            if (script != null)
            {
                _cachedScriptPath = AssetDatabase.GetAssetPath(script);
                _isFlowNode = _cachedScriptPath?.Contains("/Flow/") ?? false;
                _isEventsNode = _cachedScriptPath?.Contains("/Events/") ?? false;
            }
            return _cachedScriptPath;
        }

        public override int GetWidth() => 300;

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
            if (_tintCached)
            {
                return _cachedTint;
            }

            _tintCached = true;
            string scriptPath = GetScriptPath();

            // Try to get color from settings asset first
            var settings = SkillGraphEditorSettings.Instance;
            if (settings != null && scriptPath != null)
            {
                Color color = settings.GetColorForNodeCategory(scriptPath);
                if (color != Color.gray)
                {
                    _cachedTint = color;
                    return _cachedTint;
                }
            }

            // Fall back to NodeCategoryAttribute path matching
            if (scriptPath != null)
            {
                if (_isFlowNode)
                    _cachedTint = NodeCategoryAttribute.GetCategoryColor(NodeCategory.Flow);
                else if (_isEventsNode)
                    _cachedTint = NodeCategoryAttribute.GetCategoryColor(NodeCategory.Events);
                else if (scriptPath.Contains("/Math/"))
                    _cachedTint = NodeCategoryAttribute.GetCategoryColor(NodeCategory.Math);
                else if (scriptPath.Contains("/Conditions/"))
                    _cachedTint = NodeCategoryAttribute.GetCategoryColor(NodeCategory.Conditions);
                else
                    _cachedTint = base.GetTint();
            }
            else
            {
                _cachedTint = base.GetTint();
            }

            return _cachedTint;
        }

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // Store original label width
            float originalLabelWidth = EditorGUIUtility.labelWidth;

            // Set label width to 40% of the node width to give more space
            EditorGUIUtility.labelWidth = GetWidth() * 0.5f;

            // Use cached flags (populated by GetScriptPath on first call)
            GetScriptPath();
            bool isFlowNode = _isFlowNode;
            bool isEventsNode = _isEventsNode;

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
                    if (iterator.name is "graph" or "position" or "ports" or "OnNodeExecute")
                    {
                        continue;
                    }

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
