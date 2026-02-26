#if UNITY_EDITOR
using Turnroot.Skills.Nodes.Events;
using Turnroot.Skills.Nodes.Events.Editor;
using XNodeEditor;

namespace Turnroot.Skills.Nodes
{
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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
