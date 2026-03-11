using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class StateBrain : BrainComponent
    {
        // core state storage
        [SerializeField]
        private BrainState _currentState;
        public BrainState CurrentState => _currentState;

        // high-level state list and pause memory
        private BrainState[] _highLevelStates;
        private BrainState _savedStateBeforePause;

        // States that require back button and menu UI
        public static readonly string[] StatesThatNeedMenus = new string[]
        {
            BrainStateNames.Paused,
            BrainStateNames.MainMenu,
            BrainStateNames.PreBattle,
        };
    }
}


