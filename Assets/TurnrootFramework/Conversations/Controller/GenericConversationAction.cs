using UnityEngine.Events;

namespace Turnroot.Conversations
{
    [System.Serializable]
    public struct GenericConversationAction
    {
        public string SegmentId;
        public Conversation Conversation;
        public UnityEvent unityEventFired;
        public readonly void Invoke() => unityEventFired?.Invoke();
    }
}
