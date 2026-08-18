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
    /// Custom editor for the configurable SplitByChoicesNode.
    /// </summary>
    [CustomNodeEditor(typeof(SplitByChoicesNode))]
    public class SplitByChoicesNodeEditor : ConversationBaseNodeEditor { }

    /// <summary>
    /// Custom editor for ChangeSupportPointsNode.
    /// </summary>
    [CustomNodeEditor(typeof(ChangeSupportPointsNode))]
    public class ChangeSupportPointsNodeEditor : ConversationBaseNodeEditor { }

    /// <summary>
    /// Custom editor for UnlockBattleNode.
    /// </summary>
    [CustomNodeEditor(typeof(UnlockBattleNode))]
    public class UnlockBattleNodeEditor : ConversationBaseNodeEditor { }
}
