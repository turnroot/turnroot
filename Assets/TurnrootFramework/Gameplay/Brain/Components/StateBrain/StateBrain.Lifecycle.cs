using System.Linq;
using Turnroot.Gameplay.Brain.Events;

namespace Turnroot.Gameplay.Brain
{
    public partial class StateBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Highest;

        protected override void Awake()
        {
            base.Awake();
            InitializeHighLevelStates();
            InitializeBattleChildStates();
            InitializeGameStartChildStates();
        }

        protected override void SubscribeToBrainEvents() =>
            _brain.OnPreBattleCompleted += HandlePreBattleCompleted;

        public void HandlePreBattleTransitionToBattleCompleted() =>
            ActivateChildState(BrainStateNames.Battle);

        private void HandlePreBattleCompleted()
        {
            if (CurrentState?.Parent != null)
            {
                var newState = CurrentState.Parent.Children.FirstOrDefault(child =>
                    child.Name == BrainStateNames.PreBattleTransitionToBattle
                );
                if (newState != null)
                {
                    SetCurrentState(newState);
                }
                else
                {
                    ActivateChildState(BrainStateNames.PreBattleTransitionToBattle);
                }
            }
            else
            {
                ActivateChildState(BrainStateNames.PreBattleTransitionToBattle);
            }
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            if (_brain != null)
            {
                _brain.OnPreBattleCompleted -= HandlePreBattleCompleted;
            }
        }
    }
}
