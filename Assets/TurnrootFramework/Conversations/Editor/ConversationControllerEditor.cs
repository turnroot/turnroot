using UnityEditor;
using UnityEngine;

namespace Turnroot.Conversations.Editor
{
    /// <summary>
    /// Custom editor for ConversationController. Conversation playback is now driven entirely by
    /// id-based calls (PlayConversationById, StartConversationById) from code or UnityEvents.
    /// </summary>
    [CustomEditor(typeof(ConversationController))]
    [CanEditMultipleObjects]
    public class ConversationControllerEditor : UnityEditor.Editor
    {
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
        }
    }
}
