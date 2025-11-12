using Turnroot.Skills.Nodes;
using TurnrootFramework.Conversations;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conversation/Split By 2 Choices")]
public class SplitByChoices2Node : Node
{
    [Input]
    public ConversationFlow previous;

    [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
    public ConversationFlow ChoiceA;

    public ConversationChoice choiceA;

    [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
    public ConversationFlow ChoiceB;

    public ConversationChoice choiceB;

    public override object GetValue(NodePort port)
    {
        switch (port.fieldName)
        {
            case "ChoiceA":
                return ChoiceA;
            case "ChoiceB":
                return ChoiceB;
            default:
                return null;
        }
    }
}
