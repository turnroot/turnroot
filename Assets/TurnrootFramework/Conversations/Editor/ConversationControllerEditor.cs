using UnityEditor;
using UnityEngine;

namespace Turnroot.Conversations.Editor
{
    /// <summary>
    /// Custom editor for ConversationController. Provides a play-mode id field and quick actions.
    /// </summary>
    [CustomEditor(typeof(ConversationController))]
    [CanEditMultipleObjects]
    public class ConversationControllerEditor : UnityEditor.Editor
    {
        private string _playModeConversationId = "";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Help button at the top
            if (GUILayout.Button("📖 Show Conversation System Help", GUILayout.Height(30)))
            {
                ConversationControllerHelpWindow.ShowWindowFromButton();
            }

            GUILayout.Space(8);

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            _playModeConversationId = EditorGUILayout.TextField(
                new GUIContent("Conversation Id"),
                _playModeConversationId
            );

            using (new EditorGUI.DisabledScope(Application.isPlaying == false))
            {
                if (
                    GUILayout.Button(
                        "Play Conversation" + (Application.isPlaying ? "" : " (Play Mode only)")
                    )
                )
                {
                    foreach (var obj in targets)
                    {
                        var cc = obj as ConversationController;
                        cc?.PlayConversationById(_playModeConversationId);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying == false))
            {
                if (
                    GUILayout.Button(
                        "Next Layer" + (Application.isPlaying ? "" : " (Play Mode only)")
                    )
                )
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
}
