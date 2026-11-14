using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Skills.Nodes;

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
            if (
                target == null
                || !BattleTargetIntentMap.TryGetIntent(
                    _playerTurnFlow.GetCurrentState(),
                    out var intent
                )
            )
            {
                return false;
            }

            var state = _playerTurnFlow.GetCurrentState();
            Func<CharacterInstance, bool> isInRange = state switch
            {
                PlayerTurnStates.AttackActionChosenChoosingTarget => IsAttackTargetInRange,
                PlayerTurnStates.HealActionChosenChoosingTarget => BattleContext.IsAlly,
                _ => _ => false,
            };

            return BattleContext.GetPotentialTargets(intent, isInRange).Contains(target);
        }

        private bool IsAttackTargetInRange(CharacterInstance target) =>
            BattleContext.Participants.EnemiesInRange.Contains(target)
            || BattleContext.Participants.AlliesInRange.Contains(target)
            || BattleContext.Participants.ThirdPartyInRange.Contains(target);

        private bool IsStealTargetInRange(CharacterInstance target) =>
            BattleContext.Participants.EnemiesInRange.Contains(target)
            || BattleContext.Participants.AlliesInRange.Contains(target)
            || BattleContext.Participants.ThirdPartyInRange.Contains(target);

        #endregion
    }
}
