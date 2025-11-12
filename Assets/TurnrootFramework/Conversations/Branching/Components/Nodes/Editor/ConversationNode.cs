using Turnroot.Skills.Nodes;
using TurnrootFramework.Conversations;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conversation/Conversation")]
public class ConversationNode : Node
{
    [Input(ShowBackingValue.Never, ConnectionType.Override)]
    public ConversationFlow previous;

    [Output(ShowBackingValue.Never, ConnectionType.Override)]
    public ConversationFlow next;
    public ConversationLayer conversationLayer;
}

[System.Serializable]
public struct ConversationFlow { }
