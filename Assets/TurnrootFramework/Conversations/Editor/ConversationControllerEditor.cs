using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Conversations.Editor
{
    [CustomEditor(typeof(ConversationController))]
    [CanEditMultipleObjects]
    public class ConversationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "_conversationInstances",
                "_currentConversation"
            );

            var instancesProp = serializedObject.FindProperty("_conversationInstances");
            EditorGUILayout.PropertyField(
                instancesProp,
                new GUIContent("Conversation Instances"),
                true
            );

            // Build popup labels from instances
            var labels = new List<string>();
            if (instancesProp != null && instancesProp.arraySize > 0)
            {
                for (int i = 0; i < instancesProp.arraySize; i++)
                {
                    var elem =
                        instancesProp.GetArrayElementAtIndex(i).objectReferenceValue
                        as ConversationInstance;
                    if (elem == null)
                        labels.Add($"{i}: <null>");
                    else
                        labels.Add(
                            $"{i}: {elem.name} -> {(elem.Conversation != null ? elem.Conversation.name : "<no conversation>")}"
                        );
                }
            }
            else
            {
                labels.Add("<No Conversation Instances>");
            }

            var currentProp = serializedObject.FindProperty("_currentConversation");
            int cur = currentProp.intValue;

            // Clamp index into a safe range
            if (labels.Count > 0)
                cur = Mathf.Clamp(cur, 0, labels.Count - 1);
            else
                cur = 0;

            using (new EditorGUI.DisabledScope(labels.Count == 0))
            {
                cur = EditorGUILayout.Popup(
                    new GUIContent("Current Conversation Index"),
                    cur,
                    labels.ToArray()
                );
            }

            currentProp.intValue = cur;

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(Application.isPlaying == false))
            {
                if (
                    GUILayout.Button(
                        "Start Conversation" + (Application.isPlaying ? "" : "(Play Mode only)")
                    )
                )
                {
                    foreach (var obj in targets)
                    {
                        var cc = obj as ConversationController;
                        cc?.StartConversation();
                    }
                }
            }

            if (GUILayout.Button("Next Layer"))
            {
                foreach (var obj in targets)
                {
                    var cc = obj as ConversationController;
                    cc?.NextLayer();
                }
            }
        }
    }
}
