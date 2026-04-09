using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        public bool CanRecruit(CharacterInstance character)
        {
            return false;
        }
    }
}
