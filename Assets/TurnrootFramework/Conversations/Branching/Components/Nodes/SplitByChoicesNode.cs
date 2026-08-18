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

        [SerializeField, HideInInspector]
        private List<ConversationFlow> _choices = new();

        public IReadOnlyList<string> ChoiceLabels => _choiceLabels;

        public override void OnCreateConnection(NodePort from, NodePort to)
        {
            EnsureChoiceLists();
            base.OnCreateConnection(from, to);
        }

        public override void OnRemoveConnection(NodePort port)
        {
            EnsureChoiceLists();
            base.OnRemoveConnection(port);
        }

        public override object GetValue(NodePort port)
        {
            EnsureChoiceLists();
            int index = PortIndex(port.fieldName);
            return index >= 0 && index < _choices.Count ? _choices[index] : null;
        }

        private void EnsureChoiceLists()
        {
            _choices ??= new List<ConversationFlow>();
            _choiceLabels ??= new List<string>();

            while (_choices.Count < choiceCount)
            {
                _choices.Add(default);
            }
            while (_choices.Count > choiceCount)
            {
                _choices.RemoveAt(_choices.Count - 1);
            }

            while (_choiceLabels.Count < choiceCount)
            {
                _choiceLabels.Add($"Choice {(char)('A' + _choiceLabels.Count)}");
            }
            while (_choiceLabels.Count > choiceCount)
            {
                _choiceLabels.RemoveAt(_choiceLabels.Count - 1);
            }
        }

        private int PortIndex(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName) || fieldName.Length < 7)
            {
                return -1;
            }

            if (
                fieldName[0] == 'C'
                && fieldName[1] == 'h'
                && fieldName[2] == 'o'
                && fieldName[3] == 'i'
                && fieldName[4] == 'c'
                && fieldName[5] == 'e'
            )
            {
                if (int.TryParse(fieldName.Substring(6), out int index))
                {
                    return index - 1;
                }
            }

            return -1;
        }

#if UNITY_EDITOR
        protected override void Init()
        {
            base.Init();
            EnsureChoiceLists();
        }
#endif
    }
}
