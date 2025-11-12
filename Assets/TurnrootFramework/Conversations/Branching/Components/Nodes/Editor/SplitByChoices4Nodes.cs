using Turnroot.Skills.Nodes;
using TurnrootFramework.Conversations;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conversation/Split By 4 Choices")]
public class SplitByChoices4Node : Node
{
    [Input]
    public ConversationFlow previous;

    [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
    public ConversationFlow ChoiceA;

    public ConversationChoice choiceA;

    [Output]
    public ConversationFlow ChoiceB;

    public ConversationChoice choiceB;

    [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
    public ConversationFlow ChoiceC;

    public ConversationChoice choiceC;

    [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
    public ConversationFlow ChoiceD;

    public ConversationChoice choiceD;

    public override object GetValue(NodePort port)
    {
        switch (port.fieldName)
        {
            case "ChoiceA":
                return ChoiceA;
            case "ChoiceB":
                return ChoiceB;
            case "ChoiceC":
                return ChoiceC;
            case "ChoiceD":
                return ChoiceD;
            default:
                return null;
        }
    }
}
