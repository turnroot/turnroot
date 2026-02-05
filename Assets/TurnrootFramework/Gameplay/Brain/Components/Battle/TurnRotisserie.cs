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
                Brain.OnEndTurnCompleted -= HandlePlayerActionCompleted;
            }
        }

        private void HandlePlayerActionCompleted(CharacterInstance unit)
        {
            // If the active player chose to Wait or explicitly EndTurn, advance the rotisserie.
            if (
                _currentTurnOrder == TurnOrder.PlayerStart
                || _currentTurnOrder == TurnOrder.PlayerEnd
            )
            {
                var active = GetActiveUnit();
                if (active != null && active == unit)
                {
                    Progress();
                }
            }
        }

        #endregion

        #region Initialization
        public OperationResult BindToBattleBrain(BattleBrain brain)
        {
            battleBrain = brain;

            var validation = OperationResultGuards.RequireNotNull(Brain, nameof(Brain));
            if (!validation.Success)
            {
                return validation;
            }

            Brain.OnPlayerTurnEnded += HandlePlayerTurnCompleted;
            Brain.OnEndTurnCompleted += HandlePlayerActionCompleted;
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
            if (Context.Participants == null)
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

            return !result.Success
                ? OperationResult.Failure(
                    $"TurnRotisserie: Failed to activate {activeUnit.CharacterTemplate.DisplayName}: {result.ErrorMessage}"
                )
                : OperationResult.Successful();
        }

        private bool ProgressToNextPhase()
        {
            TurnOrder previousOrder = _currentTurnOrder;
            _currentTurnOrder = GetNextTurnOrder();
            _currentRosterIndex = -1;

            bool newRoundStarted =
                _currentTurnOrder == TurnOrder.PlayerStart
                && previousOrder != TurnOrder.PlayerStart;

            if (newRoundStarted)
            {
                // Notify end of previous round, increment battle turn counter, then notify start of new round
                Brain.PublishTurnEnded();
                battleBrain?.IncrementTurnNumber();
                Brain.PublishTurnBegin();
            }

            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    battleBrain.playerTurnFlow.StartPlayerTurn();
                    break;
                case TurnOrder.PlayerEnd:
                    Brain.PublishPlayerTurnEnded();
                    break;
                case TurnOrder.EnemyStart:
                    Brain.PublishEnemyTurnStarted();
                    break;
                case TurnOrder.EnemyEnd:
                    Brain.PublishEnemyTurnEnded();
                    break;
                case TurnOrder.ThirdPartyStart:
                    Brain.PublishThirdPartyTurnStarted();
                    break;
                case TurnOrder.ThirdPartyEnd:
                    Brain.PublishThirdPartyTurnEnded();
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
        /// so we just update the active unit reference and adjacency
        /// </summary>
        private OperationResult SetActiveUnitInContext(CharacterInstance activeUnit)
        {
            var validation = OperationResultGuards.RequireNotNull(Context, nameof(Context));
            if (!validation.Success)
            {
                return validation;
            }

            // If the active unit is already set to the requested unit, skip to avoid duplicate activation flows
            if (Context.Unit?.UnitInstance == activeUnit)
            {
                return OperationResult.Successful();
            }

            TurnrootLogger.Log(
                $"TurnRotisserie: Setting active unit to {activeUnit.CharacterTemplate.DisplayName}"
            );

            // Set active unit in context
            Context.Unit.UnitInstance = activeUnit;
            if (Context.Flags?.ActiveUnitFlags == null)
            {
                Context.Flags.ActiveUnitFlags = new UnitFlag();
            }
            Context.Flags.ActiveUnitFlags.Unit = activeUnit;
            Context.Participants.AdjacentUnits = new Locations.Adjacency(activeUnit);
            if (_currentTurnOrder is TurnOrder.PlayerStart or TurnOrder.PlayerEnd)
            {
                Brain.PublishPlayerControlledUnitActivated(activeUnit);
            }

            return OperationResult.Successful();
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
