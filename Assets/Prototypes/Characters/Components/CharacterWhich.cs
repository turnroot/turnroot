using System;
using UnityEngine;

namespace Assets.Prototypes.Characters.Components
{
    /// <summary>
    /// Defines what type of character this is. Acts like an enum but serialized as a string.
    /// </summary>
    [Serializable]
    public class CharacterWhich
    {
        // Constants for valid types
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
                    Debug.LogWarning(
                        $"Invalid character type '{value}'. Valid types: {AVATAR}, {ENEMY}, {ALLY}, {NPC}. Defaulting to {ENEMY}."
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

        /// <summary>
        /// Check if a string is a valid character type.
        /// </summary>
        public static bool IsValid(string value)
        {
            return value == AVATAR || value == ENEMY || value == ALLY || value == NPC;
        }

        // Implicit conversion to string
        public static implicit operator string(CharacterWhich which) => which._value;

        public override string ToString() => _value;
    }
}
