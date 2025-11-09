using System;
using TurnrootFramework.CommonAncestors;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Components
{
    [Serializable]
    public class Aptitude : LeveledLetteredField
    {
        public Aptitude() { }

        public Aptitude(string value)
            : base(value) { }
    }
}
