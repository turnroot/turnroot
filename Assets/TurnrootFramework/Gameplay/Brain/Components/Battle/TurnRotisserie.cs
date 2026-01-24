using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public enum TurnOrder
    {
        PlayerStart = 0,
        PlayerEnd = 1,
        EnemyStart = 2,
        EnemyEnd = 3,
        ThirdPartyStart = 4,
        ThirdPartyEnd = 5,
    }

    /// <summary>
    /// Manages turn order progression in battle.
    /// Reads from BattleContext (which is populated by BattleBrain during initialization).
    /// </summary>
    public class TurnRotisserie : MonoBehaviour
    {
        #region Dependencies (Set by BattleBrain)

        [HideInInspector]
        public BattleBrain battleBrain;

        private Brain.Brain Brain => battleBrain?.Brain;
        private BattleContext Context => battleBrain?.BattleObject?.Context;

        #endregion

        #region State

        private TurnOrder _currentTurnOrder = TurnOrder.PlayerStart;
        private int _currentRosterIndex = 0;

        private bool UnitTakesAnotherTurn =>
            Context?.Flags?.ActiveUnitFlags?.AnotherTurnGranted ?? false;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (Brain != null)
            {
                Brain.OnPlayerTurnEnded -= HandlePlayerTurnCompleted;
            }
        }

        #endregion

        #region Initialization
        public OperationResult BindToBattleBrain(BattleBrain brain)
        {
            battleBrain = brain;

            if (Brain != null)
            {
                Brain.OnPlayerTurnEnded += HandlePlayerTurnCompleted;
            }
            else
            {
                return OperationResult.Failure("TurnRotisserie: Underlying Brain is null.");
            }
            return OperationResult.Successful();
        }

        #endregion

        #region Public API

        public CharacterInstance GetActiveUnit()
        {
            var units = GetCurrentRosterUnits();
            return _currentRosterIndex >= 0 && _currentRosterIndex < units.Count
                ? units[_currentRosterIndex]
                : null;
        }

        /// <summary>
        /// Progress to the next unit or phase.
        /// Returns false if no valid units remain (battle should end).
        /// </summary>
        public bool Progress()
        {
            // Check if current unit gets another turn
            if (UnitTakesAnotherTurn)
            {
                Context.Flags.ActiveUnitFlags.AnotherTurnGranted = false;
                return ActivateCurrentUnit().Success;
            }

            // Try to find next active unit in current roster
            _currentRosterIndex++;

            var units = GetCurrentRosterUnits();
            while (_currentRosterIndex < units.Count)
            {
                var unit = units[_currentRosterIndex];

                if (!unit.IsDefeatedInCurrentBattle)
                {
                    return ActivateCurrentUnit().Success;
                }

                _currentRosterIndex++;
            }

            // No more units in this roster, move to next phase
            return ProgressToNextPhase();
        }

        #endregion

        #region Turn Order Logic

        private TurnOrder GetNextTurnOrder()
        {
            var hasThirdParty = battleBrain?.BattleObject?.HasThirdParty ?? false;

            return _currentTurnOrder switch
            {
                TurnOrder.PlayerStart => TurnOrder.PlayerEnd,
                TurnOrder.PlayerEnd => TurnOrder.EnemyStart,
                TurnOrder.EnemyStart => TurnOrder.EnemyEnd,
                TurnOrder.EnemyEnd => hasThirdParty
                    ? TurnOrder.ThirdPartyStart
                    : TurnOrder.PlayerStart,
                TurnOrder.ThirdPartyStart => TurnOrder.ThirdPartyEnd,
                TurnOrder.ThirdPartyEnd => TurnOrder.PlayerStart,
                _ => TurnOrder.PlayerStart,
            };
        }

        /// <summary>
        /// Gets active units from BattleContext.Participants (already populated by BattleBrain).
        /// </summary>
        private List<CharacterInstance> GetCurrentRosterUnits()
        {
            if (Context?.Participants == null)
            {
                TurnrootLogger.Log(
                    "TurnRotisserie: BattleContext.Participants is null!",
                    TurnrootLogger.LogLevel.Error
                );
                return new List<CharacterInstance>();
            }

            // Read from context participants (populated during battle init)
            IEnumerable<CharacterInstance> instances = _currentTurnOrder switch
            {
                TurnOrder.PlayerStart or TurnOrder.PlayerEnd => Context.Participants.Allies,
                TurnOrder.EnemyStart or TurnOrder.EnemyEnd => Context.Participants.Targets,
                TurnOrder.ThirdPartyStart or TurnOrder.ThirdPartyEnd => Context
                    .Participants
                    .ThirdParty,
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

            // Update unit statistics
            activeUnit.IncrementCombatCount();
            activeUnit.IncrementTurnsAlive();

            // Update battle context to reflect the new active unit
            var result = SetActiveUnitInContext(activeUnit);

            if (!result.Success)
            {
                return OperationResult.Failure(
                    $"TurnRotisserie: Failed to activate {activeUnit.CharacterTemplate.DisplayName}: {result.ErrorMessage}"
                );
            }

            return OperationResult.Successful();
        }

        private bool ProgressToNextPhase()
        {
            TurnOrder previousOrder = _currentTurnOrder;
            _currentTurnOrder = GetNextTurnOrder();
            _currentRosterIndex = -1; // Will be incremented to 0 on next Progress()

            // Publish phase transition events
            bool newRoundStarted =
                _currentTurnOrder == TurnOrder.PlayerStart
                && previousOrder != TurnOrder.PlayerStart;

            if (newRoundStarted)
            {
                Brain?.PublishTurnEnded();
                Brain?.PublishTurnBegin();
            }

            // Publish phase-specific events
            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    battleBrain?.playerTurnFlow?.StartPlayerTurn();
                    break;
                case TurnOrder.PlayerEnd:
                    Brain?.PublishPlayerTurnEnded();
                    break;
                case TurnOrder.EnemyStart:
                    Brain?.PublishEnemyTurnStarted();
                    break;
                case TurnOrder.EnemyEnd:
                    Brain?.PublishEnemyTurnEnded();
                    break;
                case TurnOrder.ThirdPartyStart:
                    Brain?.PublishThirdPartyTurnStarted();
                    break;
                case TurnOrder.ThirdPartyEnd:
                    Brain?.PublishThirdPartyTurnEnded();
                    break;
            }

            // Recursively activate first unit of new phase
            return Progress();
        }

        #endregion

        #region Context Updates

        /// <summary>
        /// Sets the active unit in BattleContext.
        /// BattleContext.Participants is already populated by BattleBrain during init,
        /// so we just update the active unit reference and adjacency.
        /// </summary>
        private OperationResult SetActiveUnitInContext(CharacterInstance activeUnit)
        {
            if (Context == null)
            {
                return OperationResult.Failure("BattleContext is null");
            }

            try
            {
                // Set the active unit
                Context.Unit.UnitInstance = activeUnit;

                // Update adjacency for the active unit
                Context.Participants.AdjacentUnits = new Locations.Adjacency(activeUnit);

                // Publish activation event if this is a player-controlled unit
                if (_currentTurnOrder is TurnOrder.PlayerStart or TurnOrder.PlayerEnd)
                {
                    Brain?.PublishPlayerControlledUnitActivated(activeUnit);
                }

                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"SetActiveUnitInContext failed: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerTurnCompleted()
        {
            var currentUnit = GetActiveUnit();
            if (currentUnit != null)
            {
                Progress();
            }
        }

        #endregion
    }
}
