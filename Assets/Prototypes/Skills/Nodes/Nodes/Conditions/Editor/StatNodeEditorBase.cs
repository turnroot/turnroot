#if UNITY_EDITOR
using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

/// <summary>
/// Base editor class for stat nodes (UnitStat, EnemyStat).
/// Provides common UI logic for displaying and editing stat-related nodes.
/// </summary>
public abstract class StatNodeEditorBase : NodeEditor
{
    public override void OnHeaderGUI()
    {
        base.OnHeaderGUI();
    }

    public override int GetWidth()
    {
        return 350;
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

        // Fall back to default tint
        return base.GetTint();
    }

    public override void OnBodyGUI()
    {
        serializedObject.Update();

        // Get available stats from DefaultCharacterStats
        var defaultStats = DefaultCharacterStats.Instance;
        if (defaultStats == null)
        {
            EditorGUILayout.HelpBox(
                "DefaultCharacterStats not found! Please create one in Resources/GameSettings/Character/",
                MessageType.Error
            );
            base.OnBodyGUI();
            return;
        }

        // Build list of available stats
        var availableStats = new List<string>();
        var statTypes = new List<(string name, bool isBounded)>();

        foreach (var stat in defaultStats.DefaultBoundedStats)
        {
            availableStats.Add(stat.StatType.ToString());
            statTypes.Add((stat.StatType.ToString(), true));
        }

        foreach (var stat in defaultStats.DefaultUnboundedStats)
        {
            availableStats.Add(stat.StatType.ToString());
            statTypes.Add((stat.StatType.ToString(), false));
        }

        if (availableStats.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No stats configured in DefaultCharacterStats!",
                MessageType.Warning
            );
            base.OnBodyGUI();
            return;
        }

        // Get the node's selected stat and bounded status
        var selectedStatProp = serializedObject.FindProperty("selectedStat");
        var isBoundedStatProp = serializedObject.FindProperty("isBoundedStat");
        var testProp = serializedObject.FindProperty("test");

        if (selectedStatProp == null || isBoundedStatProp == null || testProp == null)
        {
            EditorGUILayout.HelpBox(
                "Required properties not found on node!",
                MessageType.Error
            );
            base.OnBodyGUI();
            return;
        }

        // Find current selection index
        int currentIndex = -1;
        for (int i = 0; i < statTypes.Count; i++)
        {
            if (
                statTypes[i].name == selectedStatProp.stringValue
                && statTypes[i].isBounded == isBoundedStatProp.boolValue
            )
            {
                currentIndex = i;
                break;
            }
        }

        // If no valid selection, default to first stat
        if (currentIndex == -1)
        {
            currentIndex = 0;
            selectedStatProp.stringValue = statTypes[0].name;
            isBoundedStatProp.boolValue = statTypes[0].isBounded;
        }

        // Draw dropdown
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Stat", currentIndex, availableStats.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Change Selected Stat");
            selectedStatProp.stringValue = statTypes[newIndex].name;
            isBoundedStatProp.boolValue = statTypes[newIndex].isBounded;
            EditorUtility.SetDirty(target);
        }

        // Draw test value field
        EditorGUI.BeginChangeCheck();
        float newTestValue = EditorGUILayout.FloatField("Test Value", testProp.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Change Test Value");
            testProp.floatValue = newTestValue;
            EditorUtility.SetDirty(target);
        }

        // Draw output ports
        NodeEditorGUILayout.PortField(target.GetOutputPort("value"));

        // Only show maxValue and percentage ports if the stat is bounded
        if (isBoundedStatProp.boolValue)
        {
            NodeEditorGUILayout.PortField(target.GetOutputPort("maxValue"));
            NodeEditorGUILayout.PortField(target.GetOutputPort("percentage"));
        }

        // Always show bonus and bonusActive ports
        NodeEditorGUILayout.PortField(target.GetOutputPort("bonus"));
        NodeEditorGUILayout.PortField(target.GetOutputPort("bonusActive"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
