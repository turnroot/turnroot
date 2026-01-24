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

    public class TurnRotisserie : MonoBehaviour
    {
        [HideInInspector]
        public BattleBrain battleBrain;

        public bool HasThirdParty => BattleBrain?.BattleObject?.HasThirdParty ?? false;

        private BattleBrain BattleBrain => battleBrain;
        private Brain.Brain _brain => BattleBrain?.Brain;

        private TurnOrder _currentTurnOrder = TurnOrder.PlayerStart;
        private int _currentRosterIndex = 0;
        private bool UnitTakesAnotherTurn =>
            BattleBrain?.BattleObject?.Context?.Flags?.ActiveUnitFlags?.AnotherTurnGranted ?? false;

        private bool UnitFinishesMovingAfterAction =>
            BattleBrain?.BattleObject?.Context?.Flags?.ActiveUnitFlags?.CanFinishMovingAfterAction
            ?? false;

        private void Awake() { /* Binding to BattleBrain must be done explicitly via BindToBattleBrain from BattleBrain.Awake() */
        }

        private void HandlePlayerTurnCompleted()
        {
            var currentUnit = GetActiveUnit();
            if (currentUnit != null)
            {
                Progress();
            }
        }

        private void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnPlayerTurnEnded -= HandlePlayerTurnCompleted;
            }
        }

        /// <summary>
        /// Bind this Rotisserie to the authoritative BattleBrain instance and subscribe to turn events.
        /// </summary>
        public void BindToBattleBrain(BattleBrain b)
        {
            battleBrain = b;
            if (_brain != null)
            {
                _brain.OnPlayerTurnEnded += HandlePlayerTurnCompleted;
            }
            else
            {
                TurnrootLogger.Log(
                    "TurnRotisserie: Bound to BattleBrain but underlying Brain is null.",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        public CharacterInstance GetActiveUnit()
        {
            var units = GetCurrentRosterUnits();
            return _currentRosterIndex >= 0 && _currentRosterIndex < units.Count
                ? units[_currentRosterIndex]
                : null;
        }

        public TurnOrder GetNextTurnOrder()
        {
            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    return TurnOrder.PlayerEnd;
                case TurnOrder.PlayerEnd:
                    return TurnOrder.EnemyStart;
                case TurnOrder.EnemyStart:
                    return TurnOrder.EnemyEnd;
                case TurnOrder.EnemyEnd:
                    return HasThirdParty ? TurnOrder.ThirdPartyStart : TurnOrder.PlayerStart;
                case TurnOrder.ThirdPartyStart:
                    return TurnOrder.ThirdPartyEnd;
                case TurnOrder.ThirdPartyEnd:
                    return TurnOrder.PlayerStart;
                default:
                    return TurnOrder.PlayerStart;
            }
        }

        /// <summary>
        /// Gets the active units from the current roster, sorted by Order.
        /// </summary>
        private List<CharacterInstance> GetCurrentRosterUnits()
        {
            // Diagnostic logging to detect cross-GameObject binding issues
            TurnrootLogger.Log(
                $"🔍 TurnRotisserie: BattleBrain is {(BattleBrain == null ? "NULL" : "not null")}"
            );
            if (BattleBrain != null)
            {
                TurnrootLogger.Log(
                    $"🔍 PlayerTeamRoster is {(BattleBrain.PlayerTeamRoster == null ? "NULL" : "not null")}"
                );
                TurnrootLogger.Log(
                    $"🔍 BattleObject is {(BattleBrain.BattleObject == null ? "NULL" : "not null")}"
                );
            }

            IReadOnlyList<CharacterInstance> instances = null;
            Characters.Roster roster = null;

            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                case TurnOrder.PlayerEnd:
                    instances = BattleBrain?.PlayerTeamRoster?.Instances;
                    roster = BattleBrain?.PlayerTeamRoster?.roster;
                    break;
                case TurnOrder.EnemyStart:
                case TurnOrder.EnemyEnd:
                    instances = BattleBrain?.EnemyTeamRoster?.Instances;
                    roster = BattleBrain?.EnemyTeamRoster?.roster;
                    break;
                case TurnOrder.ThirdPartyStart:
                case TurnOrder.ThirdPartyEnd:
                    instances = BattleBrain?.ThirdPartyTeamRoster?.Instances;
                    roster = BattleBrain?.ThirdPartyTeamRoster?.roster;
                    break;
            }

            if (instances == null || roster == null)
            {
                TurnrootLogger.Log(
                    "TurnRotisserie: Something is wrong with the battle rosters! They are null!",
                    TurnrootLogger.LogLevel.Error
                );
                return new List<CharacterInstance>();
            }

            // Sort by Order field
            return instances
                .OrderBy(unit =>
                {
                    var placement = roster.characters.FirstOrDefault(p =>
                        p.CharacterData == unit.CharacterTemplate
                    );
                    return placement?.Order ?? int.MaxValue;
                })
                .ToList();
        }

        /// <summary>
        /// Progress to the next unit in the current roster, or the next turn phase if all units have acted.
        /// This implementation avoids recursion and caps phase advances to prevent infinite loops when rosters are empty.
        /// </summary>
        public bool Progress()
        {
            // Check if current unit gets another turn
            if (UnitTakesAnotherTurn)
            {
                BattleBrain.BattleObject.Context.Flags.ActiveUnitFlags.AnotherTurnGranted = false;
                // Same unit goes again, don't increment roster index
                if (ActivateCurrentUnit())
                {
                    return true;
                }
                return false;
            }

            int maxPhaseAttempts = System.Enum.GetValues(typeof(TurnOrder)).Length;
            int attempts = 0;

            while (attempts < maxPhaseAttempts)
            {
                // Get current roster
                var units = GetCurrentRosterUnits();
                if (units == null)
                {
                    TurnrootLogger.Log(
                        "TurnRotisserie: Cannot progress turn - current roster units are null.",
                        TurnrootLogger.LogLevel.Error
                    );
                    Debug.Break();
                    return false;
                }

                // Try to find next non-defeated unit
                _currentRosterIndex++;

                while (_currentRosterIndex < units.Count)
                {
                    var unit = units[_currentRosterIndex];

                    if (!unit.IsDefeatedInCurrentBattle)
                    {
                        // Found next active unit
                        if (ActivateCurrentUnit())
                        {
                            return true;
                        }
                        // Activation failed; try next unit in this roster
                    }

                    // Unit is defeated; clear AI helper caches to avoid stale reusable tile data
                    BattleBrain.ClearAICache();

                    // Skip defeated unit
                    _currentRosterIndex++;
                }

                // No active units in this roster; advance phase and try next roster
                ProgressToNextPhase();
                attempts++;
            }

            // If we've tried every phase and found nothing, fail gracefully
            TurnrootLogger.Log(
                "TurnRotisserie: No active units found in any roster during turn progression.",
                TurnrootLogger.LogLevel.Error
            );
            return false;
        }

        /// <summary>
        /// Activates the current unit in the current roster.
        /// </summary>
        private bool ActivateCurrentUnit()
        {
            var units = GetCurrentRosterUnits();

            if (_currentRosterIndex < 0 || _currentRosterIndex >= units.Count)
            {
                TurnrootLogger.Log(
                    $"TurnRotisserie: Invalid roster index {_currentRosterIndex}",
                    TurnrootLogger.LogLevel.Error
                );
                return false;
            }

            var activeUnit = units[_currentRosterIndex];

            if (activeUnit == null)
            {
                TurnrootLogger.Log(
                    $"TurnRotisserie: Active unit at index {_currentRosterIndex} is null",
                    TurnrootLogger.LogLevel.Warning
                );
                return false;
            }

            activeUnit.IncrementCombatCount();
            activeUnit.IncrementTurnsAlive();

            // Update battle context
            var res = ChangeBattleContextData(activeUnit);
            if (!res.Success)
            {
                TurnrootLogger.Log(
                    $"TurnRotisserie: Failed to activate unit {activeUnit?.CharacterTemplate?.DisplayName}: {res.ErrorMessage}",
                    TurnrootLogger.LogLevel.Error
                );
                // Skip this unit and move on
                _currentRosterIndex++;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Progress to the next turn phase and reset roster index.
        /// </summary>
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
                _brain.PublishTurnEnded();
                _brain.PublishTurnBegin();
            }

            // Publish phase-specific events
            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    BattleBrain.playerTurnFlow.StartPlayerTurn();
                    break;
                case TurnOrder.PlayerEnd:
                    _brain.PublishPlayerTurnEnded();
                    break;
                case TurnOrder.EnemyStart:
                    _brain.PublishEnemyTurnStarted();
                    break;
                case TurnOrder.EnemyEnd:
                    _brain.PublishEnemyTurnEnded();
                    break;
                case TurnOrder.ThirdPartyStart:
                    _brain.PublishThirdPartyTurnStarted();
                    break;
                case TurnOrder.ThirdPartyEnd:
                    _brain.PublishThirdPartyTurnEnded();
                    break;
            }

            // Activate first unit of new phase (do not recurse; caller controls iteration)
            return true;
        }

        /// <summary>
        /// Updates BattleContext with the active unit and its targets/allies.
        /// </summary>
        public OperationResult ChangeBattleContextData(CharacterInstance activeUnit)
        {
            if (BattleBrain == null)
            {
                return OperationResult.Failure("BattleBrain is null");
            }

            if (BattleBrain.BattleObject == null)
            {
                return OperationResult.Failure("BattleObject is null");
            }

            var context = BattleBrain.BattleObject.Context;
            if (context == null)
            {
                return OperationResult.Failure("BattleContext is null");
            }

            try
            {
                context.Unit.UnitInstance = activeUnit;
                context.Participants.Targets.Clear();
                context.Participants.Allies.Clear();
                context.Participants.ThirdParty.Clear();

                PopulateContext(context);

                context.Participants.AdjacentUnits = new Locations.Adjacency(activeUnit);

                // Check if player-controlled
                if (_currentTurnOrder is TurnOrder.PlayerStart or TurnOrder.PlayerEnd)
                {
                    _brain.PublishPlayerControlledUnitActivated(activeUnit);
                }

                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"ChangeBattleContextData failed: {ex.Message}");
            }
        }

        private void PopulateContext(BattleContext context)
        {
            var b = BattleBrain;
            var obj = b.BattleObject;

            // 1. Determine Roles based on Turn Order
            var (playerRole, enemyRole, thirdPartyRole) = _currentTurnOrder switch
            {
                TurnOrder.PlayerStart or TurnOrder.PlayerEnd => (
                    Role.Ally,
                    Role.Target,
                    obj.ThirdPartyFightsAllies ? Role.Target : Role.Ally
                ),

                TurnOrder.EnemyStart or TurnOrder.EnemyEnd => (
                    Role.Target,
                    Role.Ally,
                    obj.ThirdPartyFightsEnemies ? Role.Target : Role.Ally
                ),

                _ => // Third Party
                (
                    obj.ThirdPartyFightsAllies ? Role.Target : Role.Ally,
                    obj.ThirdPartyFightsEnemies ? Role.Target : Role.Ally,
                    Role.Ally
                ),
            };

            // 2. Fill the collections using a helper
            Fill(b.PlayerTeamRoster.Instances, playerRole);
            Fill(b.EnemyTeamRoster.Instances, enemyRole);
            Fill(b.ThirdPartyTeamRoster.Instances, thirdPartyRole);

            void Fill(IEnumerable<CharacterInstance> instances, Role role)
            {
                foreach (var unit in instances)
                {
                    if (unit.IsDefeatedInCurrentBattle || unit == context.Unit.UnitInstance)
                    {
                        continue;
                    }

                    if (role == Role.Ally)
                    {
                        context.Participants.Allies.Add(unit);
                    }
                    else
                    {
                        context.Participants.Targets.Add(unit);
                    }
                }
            }
        }

        private enum Role
        {
            Ally,
            Target,
        }

        /// <summary>
        /// Call this when a unit takes another turn.
        /// </summary>
        public OperationResult GrantAnotherTurn(CharacterInstance unit)
        {
            if (unit == null)
            {
                return OperationResult.Failure("Cannot grant another turn to null unit.");
            }

            var currentUnits = GetCurrentRosterUnits();
            if (_currentRosterIndex >= 0 && _currentRosterIndex < currentUnits.Count)
            {
                if (currentUnits[_currentRosterIndex] == unit)
                {
                    BattleBrain.Brain.PublishUnitTakesAnotherTurn(unit);
                    BattleBrain.BattleObject.Context.Flags.ActiveUnitFlags.AnotherTurnGranted =
                        true;
                    return OperationResult.Successful();
                }
            }
            return OperationResult.Failure("Cannot grant another turn to unit.");
        }
    }
}
