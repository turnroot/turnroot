#if UNITY_EDITOR
using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Skills.Nodes.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(UnitStat))]
public class UnitStatEditor : NodeEditor
{
    private UnitStat node;

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
        if (node == null)
            node = target as UnitStat;

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

        // Find current selection index
        int currentIndex = -1;
        for (int i = 0; i < statTypes.Count; i++)
        {
            if (
                statTypes[i].name == node.selectedStat
                && statTypes[i].isBounded == node.isBoundedStat
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
            node.selectedStat = statTypes[0].name;
            node.isBoundedStat = statTypes[0].isBounded;
            EditorUtility.SetDirty(node);
        }

        // Draw dropdown
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Stat", currentIndex, availableStats.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(node, "Change Selected Stat");
            node.selectedStat = statTypes[newIndex].name;
            node.isBoundedStat = statTypes[newIndex].isBounded;
            EditorUtility.SetDirty(node);
        }

        // Draw default value field
        EditorGUI.BeginChangeCheck();
        float newDefaultValue = EditorGUILayout.FloatField("Test Value", node.test);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(node, "Change Test Value");
            node.test = newDefaultValue;
            EditorUtility.SetDirty(node);
        }

        // Draw output ports
        NodeEditorGUILayout.PortField(target.GetOutputPort("value"));

        // Only show maxValue and percentage ports if the stat is bounded
        if (node.isBoundedStat)
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
