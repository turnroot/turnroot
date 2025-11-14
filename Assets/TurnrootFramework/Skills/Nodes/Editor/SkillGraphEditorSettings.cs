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
    /// Settings for SkillGraph editor, including port color mappings.
    /// Create via Assets > Create > Skills > Graph Editor Settings
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillGraphEditorSettings",
        menuName = "Turnroot/Editor Settings/Skills Graph Editor Settings"
    )]
    public class SkillGraphEditorSettings : ScriptableObject
    {
        [Header("Port Colors")]
        [Tooltip("Color for ExecutionFlow ports")]
        public Color executionFlowColor = new(249f / 255f, 115f / 255f, 22f / 255f); // orange-500

        [Tooltip("Color for BoolValue ports")]
        public Color boolValueColor = new(139f / 255f, 92f / 255f, 246f / 255f); // violet-500

        [Tooltip("Color for FloatValue ports")]
        public Color floatValueColor = new(14f / 255f, 165f / 255f, 233f / 255f); // sky-500

        [Tooltip("Color for StringValue ports")]
        public Color stringValueColor = new(20f / 255f, 184f / 255f, 166f / 255f); // teal-500

        [Header("Node Tint Colors (by Folder)")]
        [Tooltip("Tint color for nodes in /Flow/ folder")]
        public Color flowNodeColor = new(20f / 255f, 83f / 255f, 45f / 255f); // green-900

        [Tooltip("Tint color for nodes in /Math/ folder")]
        public Color mathNodeColor = new(19f / 255f, 78f / 255f, 74f / 255f); // teal-900

        [Tooltip("Tint color for nodes in /Events/ folder")]
        public Color eventsNodeColor = new(12f / 255f, 74f / 255f, 110f / 255f); // sky-900

        [Tooltip("Tint color for nodes in /Conditions/ folder")]
        public Color conditionsNodeColor = new(49f / 255f, 46f / 255f, 129f / 255f); // indigo-900

        private static SkillGraphEditorSettings _instance;

        /// <summary>
        /// Get the settings instance. Searches for it in Resources folder.
        /// Always reloads to ensure fresh data.
        /// </summary>
        public static SkillGraphEditorSettings Instance
        {
            get
            {
                // Always reload to get latest changes
                // Try multiple paths in Resources
                _instance = Resources.Load<SkillGraphEditorSettings>(
                    "EditorSettings/SkillGraphEditorSettings"
                );
                if (_instance == null)
                {
                    _instance = Resources.Load<SkillGraphEditorSettings>(
                        "SkillGraphEditorSettings"
                    );
                }
                return _instance;
            }
        }

        /// <summary>
        /// Clear the cached instance to force reload.
        /// </summary>
        public static void ClearCache()
        {
            _instance = null;
        }

        private void OnValidate()
        {
            // When settings change in inspector, update xNode preferences directly
            ClearCache();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                    return; // Asset was deleted

                // Get xNode's settings - but DON'T save them back
                // We only want to update the typeColors dictionary stored in EditorPrefs
                var xnodeSettings = XNodeEditor.NodeEditorPreferences.GetSettings();

                // Update type colors in xNode's settings dictionary using simple type names
                string executionFlowKey = NodeEditorUtilities.PrettyName(typeof(ExecutionFlow));
                string boolValueKey = NodeEditorUtilities.PrettyName(typeof(BoolValue));
                string floatValueKey = NodeEditorUtilities.PrettyName(typeof(FloatValue));
                string stringValueKey = NodeEditorUtilities.PrettyName(typeof(StringValue));

                if (xnodeSettings.typeColors.ContainsKey(executionFlowKey))
                    xnodeSettings.typeColors[executionFlowKey] = executionFlowColor;
                else
                    xnodeSettings.typeColors.Add(executionFlowKey, executionFlowColor);

                if (xnodeSettings.typeColors.ContainsKey(boolValueKey))
                    xnodeSettings.typeColors[boolValueKey] = boolValueColor;
                else
                    xnodeSettings.typeColors.Add(boolValueKey, boolValueColor);

                if (xnodeSettings.typeColors.ContainsKey(floatValueKey))
                    xnodeSettings.typeColors[floatValueKey] = floatValueColor;
                else
                    xnodeSettings.typeColors.Add(floatValueKey, floatValueColor);

                if (xnodeSettings.typeColors.ContainsKey(stringValueKey))
                    xnodeSettings.typeColors[stringValueKey] = stringValueColor;
                else
                    xnodeSettings.typeColors.Add(stringValueKey, stringValueColor);

                // Manually save only the typeColors to EditorPrefs
                // This preserves other settings like selection color, line style, etc.
                string typeColorsJson = JsonUtility.ToJson(
                    new SerializableTypeColorDict(xnodeSettings.typeColors)
                );
                EditorPrefs.SetString("xNode.Settings.typeColors", typeColorsJson);

                // Clear the typeColors cache in NodeEditorPreferences to force reload
                var typeColorsField = typeof(XNodeEditor.NodeEditorPreferences).GetField(
                    "typeColors",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
                );
                if (typeColorsField != null)
                {
                    var typeColorsDict =
                        typeColorsField.GetValue(null) as System.Collections.IDictionary;
                    if (typeColorsDict != null)
                    {
                        typeColorsDict.Clear();
                    }
                }

                // Force repaint all NodeEditorWindows
                XNodeEditor.NodeEditorWindow.RepaintAll();

                UnityEditor.EditorUtility.SetDirty(this);
            };
#endif
        }

        // Helper class for serializing typeColors dictionary
        [System.Serializable]
        private class SerializableTypeColorDict
        {
            public List<string> keys = new();
            public List<Color> values = new();

            public SerializableTypeColorDict(Dictionary<string, Color> dict)
            {
                foreach (var kvp in dict)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Get the color for a specific socket type.
        /// </summary>
        public Color GetColorForType(Type type)
        {
            if (type == typeof(ExecutionFlow))
                return executionFlowColor;
            else if (type == typeof(BoolValue))
                return boolValueColor;
            else if (type == typeof(FloatValue))
                return floatValueColor;
            else if (type == typeof(StringValue))
                return stringValueColor;

            return Color.white;
        }

        /// <summary>
        /// Get the tint color for a node based on its folder path.
        /// </summary>
        public Color GetColorForNodeCategory(string scriptPath)
        {
            if (scriptPath.Contains("/Flow/"))
                return flowNodeColor;
            else if (scriptPath.Contains("/Math/"))
                return mathNodeColor;
            else if (scriptPath.Contains("/Events/"))
                return eventsNodeColor;
            else if (scriptPath.Contains("/Conditions/"))
                return conditionsNodeColor;

            return Color.gray;
        }
    }
}
