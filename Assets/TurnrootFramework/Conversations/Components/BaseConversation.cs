using NaughtyAttributes;
using Turnroot.Characters;
using UnityEngine;

namespace TurnrootFramework.Conversations
{
    [System.Serializable]
    public class BaseConversation
    {
        [SerializeField]
        protected string _dialogue;

        protected string _parsedDialogue;
        protected bool _hasBeenParsed;
        public bool HasBeenParsed => _hasBeenParsed;

        [
            SerializeField,
            InfoBox("Replace `#1{them}` or `#fred{them}` with the appropriate pronoun.")
        ]
        private bool _parsePronouns = true;
        public bool ParsePronouns
        {
            get => _parsePronouns;
            set => _parsePronouns = value;
        }

        [
            SerializeField,
            ShowIf(nameof(ParsePronouns)),
            InfoBox("The characters that the pronouns refer to.")
        ]
        private CharacterData[] _referringTo;
        public CharacterData[] ReferringTo
        {
            get => _referringTo;
            set => _referringTo = value;
        }

        public string ParsedDialogue
        {
            get
            {
                if (_parsedDialogue == null)
                    ParseDialogue();
                return _parsedDialogue;
            }
        }

        public string Dialogue
        {
            get => _dialogue;
            set => _dialogue = value;
        }

        public virtual void ParseDialogue()
        {
            _parsedDialogue = ParsePronounsInDialogue(_dialogue);
            _hasBeenParsed = true;
        }

        public string ParsePronounsInDialogue(string Dialogue)
        {
            string _t = Dialogue;
            string[] pronouns = { "they", "them", "their", "theirs" };

            if (ParsePronouns && ReferringTo != null)
            {
                // first the #1 pass
                for (int i = 0; i < ReferringTo.Length; i++)
                {
                    var character = ReferringTo[i];
                    if (character != null)
                    {
                        foreach (string pronoun in pronouns)
                        {
                            string placeholder = $"#{i + 1}{{{pronoun}}}";
                            _t = _t.Replace(
                                placeholder,
                                character.CharacterPronouns.Use($"{{{pronoun}}}")
                            );
                        }
                    }
                }

                // then the #name pass
                for (int i = 0; i < ReferringTo.Length; i++)
                {
                    var character = ReferringTo[i];
                    if (character != null)
                    {
                        string namePlaceholder = $"#{character.DisplayName.ToLower()}";
                        foreach (string pronoun in pronouns)
                        {
                            string placeholder = $"{namePlaceholder}{{{pronoun}}}";
                            _t = _t.Replace(
                                placeholder,
                                character.CharacterPronouns.Use($"{{{pronoun}}}")
                            );
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("No pronouns to parse were found");
                _t = Dialogue;
            }

            return _t;
        }
    }
}
