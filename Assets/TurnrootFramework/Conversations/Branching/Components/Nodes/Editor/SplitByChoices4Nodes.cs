using Turnroot.Skills.Nodes;
using TurnrootFramework.Conversations;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conversation/Split By 4 Choices")]
public class SplitByChoices4Node : Node
{
    [Input]
    public ConversationFlow previous;

    [Output]
    public ConversationFlow ChoiceA;

    public ConversationChoice choiceA;

    [Output]
    public ConversationFlow ChoiceB;

    public ConversationChoice choiceB;

    [Output]
    public ConversationFlow ChoiceC;

    public ConversationChoice choiceC;

    [Output]
    public ConversationFlow ChoiceD;

    public ConversationChoice choiceD;
}
