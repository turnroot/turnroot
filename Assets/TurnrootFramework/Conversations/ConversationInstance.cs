using UnityEngine;

namespace Turnroot.Conversations
{
    /// <summary>
    /// Scene-level wrapper for a Conversation ScriptableObject.
    /// </summary>
    public class ConversationInstance : MonoBehaviour
    {
        [Header("Conversation Reference")]
        public Conversation Conversation;
    }
}
