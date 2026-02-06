using System;
using UnityEditor;
using XNodeEditor;

namespace Turnroot.Conversations.Branching.Nodes.Editor
{
    /// <summary>
    /// Shared implementation used by all conversation node editors.
    /// </summary>
    public abstract class ConversationBaseNodeEditor : NodeEditor
    {
        public override int GetWidth() => 350;

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

            var portNames = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var p in target.Ports)
            {
                portNames.Add(p.fieldName);
            }

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
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            EditorGUIUtility.labelWidth = originalLabelWidth;
            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Custom editor for standard ConversationNode.
    /// </summary>
    [CustomNodeEditor(typeof(ConversationNode))]
    public class ConversationNodeEditor : ConversationBaseNodeEditor { }

    /// <summary>
    /// Custom editor for SplitByChoices2Node with two choice branches.
    /// </summary>
    [CustomNodeEditor(typeof(SplitByChoices2Node))]
    public class SplitByChoices2NodeEditor : ConversationBaseNodeEditor { }

    /// <summary>
    /// Custom editor for SplitByChoices3Node with three choice branches.
    /// </summary>
    [CustomNodeEditor(typeof(SplitByChoices3Node))]
    public class SplitByChoices3NodeEditor : ConversationBaseNodeEditor { }

    /// <summary>
    /// Custom editor for SplitByChoices4Node with four choice branches.
    /// </summary>
    [CustomNodeEditor(typeof(SplitByChoices4Node))]
    public class SplitByChoices4NodeEditor : ConversationBaseNodeEditor { }
}
