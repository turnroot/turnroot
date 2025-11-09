using System;
using System.Collections.Generic;
using TurnrootFramework.CommonAncestors;
using UnityEngine;

namespace Turnroot.Characters.Subclasses
{
    [Serializable]
    public class SupportLevels : LeveledLetteredField
    {
        public string Decrease(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Decrease amount must be non-negative.");

            for (int i = 0; i < amount; i++)
            {
                _value = GetPreviousLevel(_value);
            }
            return _value;
        }
    }
}
