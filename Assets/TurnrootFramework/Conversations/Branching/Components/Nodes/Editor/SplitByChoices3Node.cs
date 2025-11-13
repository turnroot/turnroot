using Turnroot.Skills.Nodes;
using TurnrootFramework.Conversations;
using UnityEngine;
using XNode;

namespace TurnrootFramework.Conversations.Branching
{
    [CreateNodeMenu("Conversation/Split By 3 Choices")]
    public class SplitByChoices3Node : Node
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

        public ConversationChoice choicec;

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
                default:
                    return null;
            }
        }
    }
}
