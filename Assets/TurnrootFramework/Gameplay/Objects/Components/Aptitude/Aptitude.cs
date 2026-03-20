using System;
using Turnroot.CommonAncestors;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Components
{
    /// <summary>
    /// Represents a character's aptitude level as a leveled and lettered field.
    /// </summary>
    [Serializable]
    public class Aptitude : LeveledLetteredField
    {
        public Aptitude() { }

        public Aptitude(string value)
            : base(value) { }

        public Sprite GetLetterIcon()
        {
            return Value switch
            {
                "S" => GamewideUiSettings.Instance.LetterIcons.S,
                "A" => GamewideUiSettings.Instance.LetterIcons.A,
                "B" => GamewideUiSettings.Instance.LetterIcons.B,
                "C" => GamewideUiSettings.Instance.LetterIcons.C,
                "D" => GamewideUiSettings.Instance.LetterIcons.D,
                "E" => GamewideUiSettings.Instance.LetterIcons.E,
                _ => null,
            };
        }
    }
}
