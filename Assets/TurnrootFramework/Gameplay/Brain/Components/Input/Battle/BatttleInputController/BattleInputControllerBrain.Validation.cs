using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Maps;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {
        #region Validation

        public bool ValidateTileSelection(MapGridPoint point)
        {
            var currentState = _playerTurnFlow.GetCurrentState();

            return currentState switch
            {
                PlayerTurnStates.ChoosingDestination => _validMoveTiles.ContainsKey(point),
                PlayerTurnStates.AttackActionChosenChoosingTarget => _validAttackTiles.ContainsKey(
                    point
                ),
                _ => false,
            };
        }

        public bool ValidateTargetSelection(CharacterInstance target)
        {
            return target != null
                && _playerTurnFlow.GetCurrentState() switch
                {
                    PlayerTurnStates.AttackActionChosenChoosingTarget => BattleContext.IsTarget(
                        target
                    ),
                    PlayerTurnStates.HealActionChosenChoosingTarget => BattleContext.IsAlly(target),
                    _ => false,
                };
        }

        #endregion
    }
}
