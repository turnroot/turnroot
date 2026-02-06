using System;
using Turnroot.CommonAncestors;

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
    }
}
