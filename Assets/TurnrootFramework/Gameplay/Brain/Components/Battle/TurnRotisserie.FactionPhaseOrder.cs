using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class TurnRotisserie : MonoBehaviour
    {
        #region Faction Phase Order

        private BattlePhaseOrder GetNextPhaseOrder()
        {
            var hasThirdParty = battleBrain?.BattleObject.HasThirdParty ?? false;

            return _currentPhaseOrder switch
            {
                BattlePhaseOrder.PlayerPhaseStart => BattlePhaseOrder.PlayerPhaseEnd,
                BattlePhaseOrder.PlayerPhaseEnd => BattlePhaseOrder.EnemyPhaseStart,
                BattlePhaseOrder.EnemyPhaseStart => BattlePhaseOrder.EnemyPhaseEnd,
                BattlePhaseOrder.EnemyPhaseEnd => hasThirdParty
                    ? BattlePhaseOrder.ThirdPartyPhaseStart
                    : BattlePhaseOrder.PlayerPhaseStart,
                BattlePhaseOrder.ThirdPartyPhaseStart => BattlePhaseOrder.ThirdPartyPhaseEnd,
                BattlePhaseOrder.ThirdPartyPhaseEnd => BattlePhaseOrder.PlayerPhaseStart,
                _ => BattlePhaseOrder.PlayerPhaseStart,
            };
        }

        /// <summary>
        /// Gets active units from BattleContext.Participants (already populated by BattleBrain).
        /// </summary>
        private List<CharacterInstance> GetCurrentRosterUnits()
        {
            if (Context.Participants == null)
            {
                "TurnRotisserie: BattleContext.Participants is null!".LogError();
                return new List<CharacterInstance>();
            }

            // Read from context participants (populated during battle init)
            IEnumerable<CharacterInstance> instances = _currentPhaseOrder switch
            {
                BattlePhaseOrder.PlayerPhaseStart => Context.Participants.Allies,
                BattlePhaseOrder.EnemyPhaseStart => Context.Participants.Enemies,
                BattlePhaseOrder.ThirdPartyPhaseStart => Context.Participants.ThirdParty,
                BattlePhaseOrder.PlayerPhaseEnd
                or BattlePhaseOrder.EnemyPhaseEnd
                or BattlePhaseOrder.ThirdPartyPhaseEnd => new List<CharacterInstance>(),
                _ => new List<CharacterInstance>(),
            };

            // Filter out defeated units and return as list
            return instances.Where(u => u != null && !u.IsDefeatedInCurrentBattle).ToList();
        }

        private OperationResult ActivateCurrentUnit()
        {
            var units = GetCurrentRosterUnits();

            if (_currentRosterIndex < 0 || _currentRosterIndex >= units.Count)
            {
                return OperationResult.Failure(
                    $"TurnRotisserie: Invalid roster index {_currentRosterIndex}"
                );
            }

            var activeUnit = units[_currentRosterIndex];

            if (activeUnit == null)
            {
                return OperationResult.Failure(
                    $"TurnRotisserie: Active unit at index {_currentRosterIndex} is null"
                );
            }

            // Update unit statistics (combat count is incremented per-exchange in ExecuteCombatExchange)
            activeUnit.IncrementTurnsAlive();

            // Update battle context to reflect the new active unit
            var result = SetActiveUnitInContext(activeUnit);

            // recompute rates immediately after activation
            activeUnit.RecalculateCombatRates();

            return !result.Success
                ? OperationResult.Failure(
                    $"TurnRotisserie: Failed to activate {activeUnit.CharacterTemplate.DisplayName}: {result.ErrorMessage}"
                )
                : OperationResult.Successful();
        }

        private bool AdvanceToNextFactionPhase()
        {
            BattlePhaseOrder previousPhase = _currentPhaseOrder;
            _currentPhaseOrder = GetNextPhaseOrder();
            _currentRosterIndex = -1;

            bool newRoundStarted =
                _currentPhaseOrder == BattlePhaseOrder.PlayerPhaseStart
                && previousPhase != BattlePhaseOrder.PlayerPhaseStart;

            if (newRoundStarted)
            {
                // Notify end of previous round, increment battle turn counter, then notify start of new round
                PublishBattleRoundEnded();
                battleBrain?.IncrementTurnNumber();
                PublishBattleRoundStarted();
            }

            switch (_currentPhaseOrder)
            {
                case BattlePhaseOrder.PlayerPhaseStart:
                    // Notify scene flow that the player phase is beginning (for interrupt system)
                    _sceneFlow?.InitializeMiniBattleState();
                    // defer starting the player‑turn state machine until after the
                    // first unit in the roster has actually been activated.  the
                    // old implementation invoked BeginPlayerPhase() here, which meant
                    // the PublishPlayerPhaseStarted event was fired with whatever unit
                    // was still stored on the context (usually the last enemy/third‑
                    // party unit from the previous phase).
                    break;
                case BattlePhaseOrder.PlayerPhaseEnd:
                    Brain.PublishPlayerPhaseEnded();
                    // Notify scene flow to progress (handles interrupts before next phase)
                    _sceneFlow?.ProgressMiniBattleState();
                    break;
                case BattlePhaseOrder.EnemyPhaseStart:
                    Brain.PublishEnemyPhaseStarted();
                    break;
                case BattlePhaseOrder.EnemyPhaseEnd:
                    Brain.PublishEnemyPhaseEnded();
                    break;
                case BattlePhaseOrder.ThirdPartyPhaseStart:
                    Brain.PublishThirdPartyPhaseStarted();
                    break;
                case BattlePhaseOrder.ThirdPartyPhaseEnd:
                    Brain.PublishThirdPartyPhaseEnded();
                    break;
            }

            // Recursively activate first unit of new phase
            var success = AdvanceToNextUnitOrPhase();

            // if we're now in a player phase, the first player unit should be
            // active; signal the player‑turn flow so it can publish the start
            // event and reset its internal state.  this used to happen before the
            // recursive call which resulted in the wrong unit being reported.
            if (success && _currentPhaseOrder == BattlePhaseOrder.PlayerPhaseStart)
            {
                battleBrain?.playerTurnFlow?.BeginPlayerPhase();
            }

            return success;
        }

        private void PublishBattleRoundStarted()
        {
            Brain.PublishBattleRoundStarted();
            Brain.Publish(new BattleRoundStartedEvent(battleBrain.CurrentTurnNumber));
        }

        private void PublishBattleRoundEnded()
        {
            Brain.PublishBattleRoundEnded();
            Brain.Publish(new BattleRoundEndedEvent(battleBrain.CurrentTurnNumber));
        }

        #endregion
    }
}