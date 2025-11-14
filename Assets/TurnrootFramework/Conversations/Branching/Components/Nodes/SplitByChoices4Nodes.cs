using Turnroot.Conversations;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Conversations.Branching
{
    [CreateNodeMenu("Conversation/Split By 4 Choices")]
    public class SplitByChoices4Node : Node
    {
        [Input]
        public ConversationFlow previous;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceA;

        public string choiceA;

        [Output]
        public ConversationFlow ChoiceB;

        public string choiceb;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceC;

        public string choicec;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceD;

        public string choiced;

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
}
