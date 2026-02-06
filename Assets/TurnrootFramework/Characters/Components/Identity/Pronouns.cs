using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Characters.Subclasses
{
    /// <summary>
    /// Manages character pronoun sets (they/them, she/her, he/him).
    /// </summary>
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
            _selectedPronouns = PronounSets.TryGetValue(key, out var pronouns)
                ? pronouns
                : PronounSets["they"];
        }

        public string Get(string pronounCase)
        {
            return string.IsNullOrEmpty(pronounCase)
                ? Singular
                : pronounCase.ToLower() switch
                {
                    "singular" or "they" => Singular,
                    "possessiveadjective" or "their" => PossessiveAdjective,
                    "possessivepronoun" or "theirs" => PossessivePronoun,
                    "objective" or "them" => Objective,
                    _ => Singular,
                };
        }

        public static string[] GetAvailablePronounKeys()
        {
            var keys = new string[PronounSets.Count];
            PronounSets.Keys.CopyTo(keys, 0);
            return keys;
        }

        /// <summary>
        /// Gets the current pronoun key ("they", "she", "he") based on the selected pronouns.
        /// </summary>
        public string GetPronounKey()
        {
            if (_selectedPronouns == null)
            {
                return "they";
            }

            // Find matching key by comparing pronoun arrays
            foreach (var kvp in PronounSets)
            {
                if (kvp.Value.Length == _selectedPronouns.Length)
                {
                    bool matches = true;
                    for (int i = 0; i < kvp.Value.Length; i++)
                    {
                        if (kvp.Value[i] != _selectedPronouns[i])
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (matches)
                    {
                        return kvp.Key;
                    }
                }
            }

            return "they"; // Default if no match found
        }

        /// <summary>
        /// Replaces pronoun placeholders in text with the appropriate pronouns.
        /// Example: "I saw {them} and {their} friend" -> "I saw him and his friend"
        /// </summary>
        public string Use(string text)
        {
            return string.IsNullOrEmpty(text)
                ? text
                : text.Replace("{they}", Singular)
                    .Replace("{them}", Objective)
                    .Replace("{their}", PossessiveAdjective)
                    .Replace("{theirs}", PossessivePronoun)
                    // Capitalized versions
                    .Replace("{They}", Capitalize(Singular))
                    .Replace("{Them}", Capitalize(Objective))
                    .Replace("{Their}", Capitalize(PossessiveAdjective))
                    .Replace("{Theirs}", Capitalize(PossessivePronoun));
        }

        private string Capitalize(string str) =>
            string.IsNullOrEmpty(str) ? str : char.ToUpper(str[0]) + str.Substring(1);
    }
}
