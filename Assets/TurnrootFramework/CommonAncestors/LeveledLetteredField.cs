using System;
using UnityEngine;

namespace TurnrootFramework.CommonAncestors
{
    [Serializable]
    public class LeveledLetteredField
    {
        [SerializeField]
        protected string _value;

        public const string S = "S";
        public const string A = "A";
        public const string B = "B";
        public const string C = "C";
        public const string D = "D";
        public const string E = "E";

        public LeveledLetteredField() { }

        public LeveledLetteredField(string value)
        {
            if (!IsValid(value))
                throw new System.ArgumentException($"Invalid level: {value}");
            _value = value;
        }

        public override string ToString() => _value;

        public string Increase()
        {
            _value = GetNextLevel(_value);
            return _value;
        }

        protected string GetNextLevel(string current)
        {
            return current switch
            {
                S => S,
                A => S,
                B => A,
                C => B,
                D => C,
                E => D,
                _ => throw new ArgumentException($"Invalid level: {current}"),
            };
        }

        public static bool IsValid(string value)
        {
            return value == S || value == A || value == B || value == C || value == D || value == E;
        }

        public string Value
        {
            get => _value;
            set
            {
                if (!IsValid(value))
                    throw new System.ArgumentException($"Invalid level: {value}");
                _value = value;
            }
        }

        protected string GetPreviousLevel(string current)
        {
            return current switch
            {
                S => A,
                A => B,
                B => C,
                C => D,
                D => E,
                E => E,
                _ => throw new ArgumentException($"Invalid level: {current}"),
            };
        }
    }
}
