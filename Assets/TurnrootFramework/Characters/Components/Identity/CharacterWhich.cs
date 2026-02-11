using System;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.Components
{
    /// <summary>
    /// Defines what type of character this is. Acts like an enum but serialized as a string.
    /// </summary>
    [Serializable]
    public class CharacterWhich
    {
        public const string AVATAR = "Avatar";
        public const string ENEMY = "Enemy";
        public const string ALLY = "Ally";
        public const string NPC = "NPC";

        [SerializeField]
        private string _value = ENEMY;

        /// <summary>
        /// Gets or sets the character type. Validates against valid types.
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if (IsValid(value))
                {
                    _value = value;
                }
                else
                {
                    TurnrootLogger.Log(
                        $"Invalid CharacterWhich value: {value}. Defaulting to '{ENEMY}'.",
                        TurnrootLogger.LogLevel.Warning
                    );
                    _value = ENEMY;
                }
            }
        }

        public CharacterWhich() { }

        public CharacterWhich(string value)
        {
            Value = value;
        }

        public static bool IsValid(string value) => value is AVATAR or ENEMY or ALLY or NPC;

        public static implicit operator string(CharacterWhich which) => which?._value;

        public override string ToString() => _value;
    }
}
