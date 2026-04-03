using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Subclasses;
using Turnroot.CommonAncestors;
using Turnroot.GameSettings;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "SupportRelationshipTable",
        menuName = "Turnroot/Characters/SupportRelationshipTable"
    )]
    public class SupportRelationshipTable : SingletonScriptableObject<SupportRelationshipTable>
    {
        [Serializable]
        public struct SupportPairing
        {
            [HorizontalLine(color: EColor.Gray)]
            [Tooltip("First character in this support pairing.")]
            public CharacterData CharacterA;

            [Tooltip("Second character in this support pairing.")]
            public CharacterData CharacterB;

            [Tooltip("The maximum support rank this pair can achieve (E/D/C/B/A/S).")]
            public SupportLevels MaxSupportLevel;
            public float SupportGainMultiplier;
        }

        [InfoBox(
            "Define every valid support pairing here. "
                + "Each entry links two characters and sets their maximum achievable support rank."
        )]
        public List<SupportPairing> Pairings = new();

        /// <summary>
        /// Returns the pairing entry for two characters (order-independent), or null if none exists.
        /// </summary>
        public bool TryGetPairing(CharacterData a, CharacterData b, out SupportPairing pairing)
        {
            foreach (var p in Pairings)
            {
                if (
                    (p.CharacterA == a && p.CharacterB == b)
                    || (p.CharacterA == b && p.CharacterB == a)
                )
                {
                    pairing = p;
                    return true;
                }
            }

            pairing = default;
            return false;
        }

        /// <summary>
        /// Returns true if a support pairing exists between the two characters.
        /// </summary>
        public bool HasPairing(CharacterData a, CharacterData b) => TryGetPairing(a, b, out _);

        /// <summary>
        /// Returns a list of initialized <see cref="SupportRelationshipInstance"/> objects for all
        /// pairings that involve <paramref name="character"/>, with the partner set as the other member.
        /// </summary>
        public List<SupportRelationshipInstance> GetInstancesFor(CharacterData character)
        {
            var list = new List<SupportRelationshipInstance>();
            if (character == null || Pairings == null)
            {
                return list;
            }

            foreach (var pairing in Pairings)
            {
                CharacterData partner = null;
                if (pairing.CharacterA == character)
                {
                    partner = pairing.CharacterB;
                }
                else if (pairing.CharacterB == character)
                {
                    partner = pairing.CharacterA;
                }

                if (partner != null)
                {
                    list.Add(new SupportRelationshipInstance(pairing, partner));
                }
            }
            return list;
        }
    }
}
