using Turnroot.Conversations;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Conversations.Branching
{
    [CreateNodeMenu("Conversation/Split By 2 Choices")]
    public class SplitByChoices2Node : Node
    {
        [Input]
        public ConversationFlow previous;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceA;

        public string choiceA;

        [Output(ShowBackingValue.Unconnected, ConnectionType.Override)]
        public ConversationFlow ChoiceB;

        public string choiceB;

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
}
