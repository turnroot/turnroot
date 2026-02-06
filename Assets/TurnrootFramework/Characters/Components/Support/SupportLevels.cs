using System;
using Turnroot.CommonAncestors;

namespace Turnroot.Characters.Subclasses
{
    /// <summary>
    /// Represents support relationship levels using a letter-based ranking system (E-S).
    /// </summary>
    [Serializable]
    public class SupportLevels : LeveledLetteredField
    {
        public string Decrease(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Decrease amount must be non-negative.");
            }

            for (int i = 0; i < amount; i++)
            {
                _value = GetPreviousLevel(_value);
            }
            return _value;
        }
    }
}
