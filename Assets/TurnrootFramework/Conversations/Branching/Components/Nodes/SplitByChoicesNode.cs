using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Conversations.Branching
{
    /// <summary>
    /// A conversation node that branches into a configurable number of player choices.
    /// </summary>
    [CreateNodeMenu("Conversation/Split By Choices")]
    public class SplitByChoicesNode : Node
    {
        [Input]
        public ConversationFlow previous;

        [Min(2)]
        public int choiceCount = 3;

        [SerializeField]
        private List<string> _choiceLabels = new();

        public IReadOnlyList<string> ChoiceLabels => _choiceLabels;

        public string GetChoiceLabel(int index) =>
            index >= 0 && index < _choiceLabels.Count ? _choiceLabels[index] : "Choice";

        /// <summary>
        /// Ensures a dynamic output port exists for each configured choice. Called by the
        /// editor and runtime graph parser to keep ports in sync with <see cref="choiceCount"/>.
        /// </summary>
        public void EnsureChoicePorts()
        {
            EnsureChoiceLabels();
            AddDynamicInput(typeof(ConversationFlow), fieldName: "previous");

            for (int i = 0; i < choiceCount; i++)
            {
                var portName = $"Choice{i + 1}";
                if (GetOutputPort(portName) == null)
                {
                    AddDynamicOutput(typeof(ConversationFlow), fieldName: portName);
                }
            }

            var toRemove = new List<NodePort>();
            foreach (var port in DynamicPorts)
            {
                if (
                    port.fieldName.StartsWith("Choice")
                    && int.TryParse(port.fieldName.Substring(6), out int index)
                    && index > choiceCount
                )
                {
                    toRemove.Add(port);
                }
            }

            foreach (var port in toRemove)
            {
                RemoveDynamicPort(port);
            }
        }

        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            EnsureChoicePorts();
            base.OnCreateConnection(from, to);
        }

        public override void OnRemoveConnection(NodePort port)
        {
            EnsureChoicePorts();
            base.OnRemoveConnection(port);
        }

        public override object GetValue(NodePort port)
        {
            return null;
        }

        private void EnsureChoiceLabels()
        {
            _choiceLabels ??= new List<string>();

            while (_choiceLabels.Count < choiceCount)
            {
                _choiceLabels.Add($"Choice {(char)('A' + _choiceLabels.Count)}");
            }
            while (_choiceLabels.Count > choiceCount)
            {
                _choiceLabels.RemoveAt(_choiceLabels.Count - 1);
            }
        }

#if UNITY_EDITOR
        protected override void Init()
        {
            base.Init();
            EnsureChoicePorts();
        }
#endif
    }
}
