using Turnroot.Conversations;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Conversations.Branching
{
    [CreateNodeMenu("Conversation/Split By 3 Choices")]
    public class SplitByChoices3Node : Node
    {
        [Input]
        public ConversationFlow previous;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceA;

        public string choiceA;

        [Output]
        public ConversationFlow ChoiceB;

        public string choiceB;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceC;

        public string choicec;

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
