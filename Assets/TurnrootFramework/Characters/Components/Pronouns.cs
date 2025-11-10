using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Characters.Subclasses
{
    [Serializable]
    public class Pronouns
    {
        private static readonly Dictionary<string, string[]> PronounSets = new()
        {
            { "they", new[] { "they", "their", "theirs", "them" } },
            { "she", new[] { "she", "her", "hers", "her" } },
            { "he", new[] { "he", "his", "his", "him" } },
        };

        [SerializeField]
        private string[] _selectedPronouns;

        public string Singular => _selectedPronouns?[0] ?? "they";
        public string PossessiveAdjective => _selectedPronouns?[1] ?? "their";
        public string PossessivePronoun => _selectedPronouns?[2] ?? "theirs";
        public string Objective => _selectedPronouns?[3] ?? "them";

        public Pronouns(string pronounType = "they")
        {
            SetPronounType(pronounType);
        }

        public Pronouns()
        {
            _selectedPronouns = PronounSets["they"];
        }

        public void SetPronounType(string pronounType)
        {
            string key = pronounType?.ToLower() ?? "they";
            _selectedPronouns = PronounSets.ContainsKey(key)
                ? PronounSets[key]
                : PronounSets["they"];
        }

        public string Get(string pronounCase)
        {
            if (string.IsNullOrEmpty(pronounCase))
                return Singular;

            switch (pronounCase.ToLower())
            {
                case "singular":
                case "they":
                    return Singular;
                case "possessiveadjective":
                case "their":
                    return PossessiveAdjective;
                case "possessivepronoun":
                case "theirs":
                    return PossessivePronoun;
                case "objective":
                case "them":
                    return Objective;
                default:
                    return Singular;
            }
        }

        /// <summary>
        /// Replaces pronoun placeholders in text with the appropriate pronouns.
        /// Example: "I saw {them} and {their} friend" -> "I saw him and his friend"
        /// </summary>
        public string Use(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Replace("{they}", Singular)
                .Replace("{them}", Objective)
                .Replace("{their}", PossessiveAdjective)
                .Replace("{theirs}", PossessivePronoun)
                // Capitalized versions
                .Replace("{They}", Capitalize(Singular))
                .Replace("{Them}", Capitalize(Objective))
                .Replace("{Their}", Capitalize(PossessiveAdjective))
                .Replace("{Theirs}", Capitalize(PossessivePronoun));
        }

        private string Capitalize(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
}
