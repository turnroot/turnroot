using System;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Components
{
    [Serializable]
    public class AptitudeLevel
    {
        [SerializeField]
        private string _value;

        public const string S = "S";
        public const string A = "A";
        public const string B = "B";
        public const string C = "C";
        public const string D = "D";
        public const string E = "E";

        public AptitudeLevel() { }

        public AptitudeLevel(string value)
        {
            if (!IsValid(value))
                throw new System.ArgumentException($"Invalid aptitude level: {value}");
            _value = value;
        }

        public override string ToString() => _value;

        /// <summary>
        /// Checks if a value is valid (exists in the defined constants).
        /// </summary>
        public static bool IsValid(string value)
        {
            return value == S || value == A || value == B || value == C || value == D || value == E;
        }
    }
}
