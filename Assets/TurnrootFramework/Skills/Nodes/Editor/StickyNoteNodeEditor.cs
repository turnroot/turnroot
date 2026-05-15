using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Turnroot.Skills.Nodes.Editor
{
    [CustomNodeEditor(typeof(Utility.StickyNoteNode))]
    public class StickyNoteNodeEditor : NodeEditor
    {
        private static readonly Color NoteColor = new(0.98f, 0.93f, 0.45f); // sticky-note yellow

        public override int GetWidth() => 250;

        public override Color GetTint() => NoteColor;

        public override void OnHeaderGUI()
        {
            // Suppress default title — the text area is all this node needs.
        }

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            GUIStyle textStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 11,
                normal = { textColor = new Color(0.15f, 0.10f, 0f) },
            };

            var noteProp = serializedObject.FindProperty("note");
            noteProp.stringValue = EditorGUILayout.TextArea(
                noteProp.stringValue,
                textStyle,
                GUILayout.MinHeight(60)
            );

            serializedObject.ApplyModifiedProperties();
        }
    }
}
