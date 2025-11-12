using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Turnroot.Conversations.Branching.Nodes.Editor
{
    // Shared implementation used by all conversation node editors
    public abstract class ConversationBaseNodeEditor : NodeEditor
    {
        public override int GetWidth()
        {
            return 350;
        }

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // Store original label width
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GetWidth() * 0.5f;

            // Draw all ports
            foreach (var port in target.Ports)
            {
                NodeEditorGUILayout.PortField(port);
            }

            // Draw serialized properties excluding internal xNode fields and any fields
            // that correspond to ports (we already drew those via PortField).
            var portNames = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var p in target.Ports)
                portNames.Add(p.fieldName);

            SerializedProperty iterator = serializedObject.GetIterator();
            iterator.NextVisible(true); // Skip script
            while (iterator.NextVisible(false))
            {
                if (
                    iterator.name == "graph"
                    || iterator.name == "position"
                    || iterator.name == "ports"
                    || portNames.Contains(iterator.name)
                )
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }

            EditorGUIUtility.labelWidth = originalLabelWidth;
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomNodeEditor(typeof(ConversationNode))]
    public class ConversationNodeEditor : ConversationBaseNodeEditor { }

    [CustomNodeEditor(typeof(SplitByChoices2Node))]
    public class SplitByChoices2NodeEditor : ConversationBaseNodeEditor { }

    [CustomNodeEditor(typeof(SplitByChoices3Node))]
    public class SplitByChoices3NodeEditor : ConversationBaseNodeEditor { }

    [CustomNodeEditor(typeof(SplitByChoices4Node))]
    public class SplitByChoices4NodeEditor : ConversationBaseNodeEditor { }
}
