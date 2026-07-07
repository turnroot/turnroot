using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;

namespace Turnroot.Utilities
{
    public static class CharacterDataUtilities
    {
        public static bool CharacterDataMatches(CharacterData a, CharacterData b)
        {
            if (a == b)
            {
                return true;
            }

            return a == null || b == null
                ? false
                : !string.IsNullOrEmpty(a.FullName)
                && !string.IsNullOrEmpty(b.FullName)
                && string.Equals(a.FullName, b.FullName, StringComparison.Ordinal)
                ? true
                : !string.IsNullOrEmpty(a.name)
                && !string.IsNullOrEmpty(b.name)
                && string.Equals(a.name, b.name, StringComparison.Ordinal);
        }

        public static bool Matches(this CharacterData a, CharacterData b) =>
            CharacterDataMatches(a, b);

        public static bool ContainsMatching(
            this IEnumerable<CharacterData> collection,
            CharacterData target
        ) => collection != null && collection.Any(item => CharacterDataMatches(item, target));
    }
}
