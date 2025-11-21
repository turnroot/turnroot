using UnityEngine;
using XNode;
#if UNITY_EDITOR
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
#endif

namespace Turnroot.Conversations.Branching.Nodes
{
    [CreateAssetMenu(
        fileName = "NewConversationGraph",
        menuName = "Turnroot/Conversations/Branching Conversation"
    )]
    public partial class ConversationGraph : NodeGraph { }

#if UNITY_EDITOR
    public partial class ConversationGraph
    {
        // Diagnostics removed — OnValidate no longer writes audit/tracing info.

        private static bool HasConnections(XNode.Node node)
        {
            if (node == null || node.Ports == null)
                return false;
            return node.Ports.Any(p => p.GetConnections() != null && p.GetConnections().Count > 0);
        }

        private static bool IsEmptyNodeEditor(XNode.Node node)
        {
            if (node == null)
                return false;

            var so = new SerializedObject(node as UnityEngine.Object);
            var prop = so.GetIterator();
            var ignore = new HashSet<string> { "m_Script", "position", "xnode.graph", "graph" };
            while (prop.NextVisible(true))
            {
                if (ignore.Contains(prop.name))
                    continue;

                if (
                    prop.propertyType == SerializedPropertyType.ObjectReference
                    && prop.objectReferenceValue != null
                )
                    return false;

                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        if (prop.intValue != 0)
                            return false;
                        break;
                    case SerializedPropertyType.Float:
                        if (Mathf.Abs(prop.floatValue) > Mathf.Epsilon)
                            return false;
                        break;
                    case SerializedPropertyType.Boolean:
                        if (prop.boolValue)
                            return false;
                        break;
                    case SerializedPropertyType.String:
                        if (!string.IsNullOrEmpty(prop.stringValue))
                            return false;
                        break;
                    case SerializedPropertyType.Enum:
                        if (prop.intValue != 0)
                            return false;
                        break;
                }
            }

            return true;
        }
    }
#endif
}
