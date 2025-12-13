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

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceB;

        public string choiceB;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceC;

        public string choiceC;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceD;

        public string choiceD;

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
