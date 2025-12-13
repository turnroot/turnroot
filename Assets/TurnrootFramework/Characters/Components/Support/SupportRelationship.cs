using System;
using System.Collections.Generic;
using Turnroot.Characters.Subclasses;
using UnityEngine;

namespace Turnroot.Characters.Components.Support
{
    [Serializable]
    public class SupportRelationship
    {
        [SerializeField]
        private CharacterData _character;

        [SerializeField]
        private SupportLevels _supportLevel;

        [SerializeField]
        private SupportLevels _maxLevel;

        [SerializeField, Range(1, 5)]
        private int _supportSpeed = 1;

        public SupportRelationship()
        {
            _supportLevel = new SupportLevels { Value = "E" };
            _maxLevel = new SupportLevels { Value = "A" };
            _supportSpeed = 1;
        }

        public CharacterData Character
        {
            get => _character;
            set => _character = value;
        }

        public SupportLevels SupportLevel
        {
            get
            {
                _supportLevel ??= new SupportLevels { Value = "E" };

                return _supportLevel;
            }
        }

        public string MaxLevel
        {
            get
            {
                _maxLevel ??= new SupportLevels { Value = "A" };

                return _maxLevel.Value;
            }
            set => _maxLevel = new SupportLevels() { Value = value };
        }

        public int SupportSpeed
        {
            get => _supportSpeed > 0 ? _supportSpeed : 1;
            set => _supportSpeed = Mathf.Clamp(value, 1, 5);
        }

        public void InitializeDefaults()
        {
            _supportLevel ??= new SupportLevels { Value = "E" };

            _maxLevel ??= new SupportLevels { Value = "A" };

            if (_supportSpeed <= 0)
            {
                _supportSpeed = 1;
            }
        }

        /// <summary>
        /// Sanitize a list of support relationships for a character.
        /// Removes self-references and initializes defaults on remaining entries.
        /// Returns a list of removed entries for optional logging.
        /// </summary>
        public static List<SupportRelationship> SanitizeForCharacter(
            CharacterData owner,
            List<SupportRelationship> relationships
        )
        {
            var removed = new List<SupportRelationship>();
            if (relationships == null)
            {
                return removed;
            }

            for (int i = relationships.Count - 1; i >= 0; i--)
            {
                var rel = relationships[i];
                if (rel == null)
                {
                    continue;
                }

                if (rel.Character == owner)
                {
                    removed.Add(rel);
                    relationships.RemoveAt(i);
                }
                else
                {
                    rel.InitializeDefaults();
                }
            }

            return removed;
        }
    }
}
