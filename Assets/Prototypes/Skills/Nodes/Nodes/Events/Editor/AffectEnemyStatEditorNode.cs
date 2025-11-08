#if UNITY_EDITOR
using Assets.Prototypes.Skills.Nodes.Events;
using Assets.Prototypes.Skills.Nodes.Events.Editor;
using UnityEditor;
using XNodeEditor;

/// <summary>
/// Custom editor for AffectEnemyStat nodes.
/// Modifies stats on the target enemy (Targets[0]) or all targeted enemies.
/// </summary>
[CustomNodeEditor(typeof(AffectEnemyStatNode))]
public class AffectEnemyStatEditorNode : AffectStatNodeEditorBase
{
    public override void OnBodyGUI()
    {
        // Call base to draw the standard fields
        base.OnBodyGUI();

        // Add the affectAllEnemies port after the base fields
        serializedObject.Update();

        NodeEditorGUILayout.PortField(target.GetInputPort("affectAllEnemies"));

        // Draw test value for affectAllEnemies
        var testAffectAllProp = serializedObject.FindProperty("testAffectAll");
        if (testAffectAllProp != null)
        {
            EditorGUI.BeginChangeCheck();
            bool newTestValue = EditorGUILayout.Toggle(
                "Test Affect All",
                testAffectAllProp.boolValue
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Change Test Affect All");
                testAffectAllProp.boolValue = newTestValue;
                EditorUtility.SetDirty(target);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
