using Turnroot.Skills.Nodes;
using TurnrootFramework.Conversations;
using UnityEngine;
using XNode;

[CreateNodeMenu("Conversation/Split By 3 Choices")]
public class SplitByChoices3Node : Node
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
}
