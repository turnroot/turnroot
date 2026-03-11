using System.Collections.Generic;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class StateBrain : BrainComponent
    {
        #region Initialization Helpers

        public void InitializeHighLevelStates()
        {
            if (_highLevelStates != null && _highLevelStates.Length > 0)
            {
                return;
            }

            SetHighLevelStates();
        }

        public void InitializeBattleChildStates()
        {
            var combatState = FindHighLevelState(BrainStateNames.Combat);
            if (combatState.Children != null && combatState.Children.Length > 0)
            {
                return;
            }

            SetBattleChildStates();
        }

        public void InitializeGameStartChildStates()
        {
            var gameStartState = FindHighLevelState(BrainStateNames.GameStart);
            if (gameStartState.Children != null && gameStartState.Children.Length > 0)
            {
                return;
            }

            var childStates = new BrainState[]
            {
                new(BrainStateNames.ChooseSaveFile, gameStartState),
            };

            gameStartState.Children = childStates;
        }

        public OperationResult SetBattleChildStates()
        {
            var combatState = FindHighLevelState(BrainStateNames.Combat);
            if (combatState == null)
            {
                return OperationResult.Failure(
                    "StateBrain: Combat state not found during child state initialization."
                );
            }

            var childStates = new BrainState[]
            {
                new(BrainStateNames.PreBattle, combatState),
                new(BrainStateNames.PreBattleTransitionToBattle, combatState),
                new(BrainStateNames.Battle, combatState),
                new(BrainStateNames.PostBattle, combatState),
            };

            combatState.Children = childStates;
            return OperationResult.Successful();
        }

        public void SetHighLevelStates()
        {
            var states = new List<BrainState>
            {
                new(BrainStateNames.Cutscene),
                new(BrainStateNames.Paused),
                new(BrainStateNames.Combat),
                new(BrainStateNames.GameStart),
                new(BrainStateNames.WorldMap),
                new(BrainStateNames.Shop),
                new(BrainStateNames.Armory),
                new(BrainStateNames.SupportConversation),
                new(BrainStateNames.Barracks),
                new(BrainStateNames.Base),
                new(BrainStateNames.Trading),
                new(BrainStateNames.ClassChange),
                new(BrainStateNames.Forging),
                new(BrainStateNames.Records),
                new(BrainStateNames.Briefing),
                new(BrainStateNames.Deployment),
                new(BrainStateNames.Configuration),
            };

#if TURNROOT_CAMP_MODULE
            states.Add(new BrainState(BrainStateNames.Hub));
#endif

            states.AddRange(
                new[]
                {
                    new BrainState(BrainStateNames.MainMenu),
                    new BrainState(BrainStateNames.GameOver),
                    new BrainState(BrainStateNames.Credits),
                    new BrainState(BrainStateNames.NonCombatGameplay),
                }
            );

            _highLevelStates = states.ToArray();
            Brain.PublishHighLevelStatesInitialized();
        }

        #endregion
    }
}
