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

    [RequireComponent(typeof(BattleBrain))]
    public class TurnRotisserie : MonoBehaviour
    {
        [HideInInspector]
        public bool HasThirdParty => BattleBrain.BattleObject.HasThirdParty;

        private BattleBrain BattleBrain => GetComponent<BattleBrain>();
        private Brain.Brain _brain => BattleBrain.Brain;

        private TurnOrder _currentTurnOrder = TurnOrder.PlayerStart;
        private int _currentRosterIndex = 0;
        private bool UnitTakesAnotherTurn => BattleBrain.BattleObject.Context.AnotherTurnGranted;

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
                    Debug.LogError("Invalid TurnOrder state.");
                    return TurnOrder.PlayerStart;
            }
        }

        /// <summary>
        /// Gets the active units from the current roster, sorted by Order.
        /// </summary>
        private List<CharacterInstance> GetCurrentRosterUnits()
        {
            IReadOnlyList<CharacterInstance> instances = null;
            Roster roster = null;

            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    instances = BattleBrain.PlayerTeamRoster.Instances;
                    roster = BattleBrain.PlayerTeamRoster.roster;
                    break;
                case TurnOrder.EnemyStart:
                    instances = BattleBrain.EnemyTeamRoster.Instances;
                    roster = BattleBrain.EnemyTeamRoster.roster;
                    break;
                case TurnOrder.ThirdPartyStart:
                    instances = BattleBrain.ThirdPartyTeamRoster.Instances;
                    roster = BattleBrain.ThirdPartyTeamRoster.roster;
                    break;
            }

            if (instances == null || roster == null)
            {
                Debug.LogError(
                    "TurnRotisserie: Something is wrong with the battle rosters! They are null!"
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
        /// </summary>
        public bool Progress()
        {
            // Check if current unit gets another turn
            if (UnitTakesAnotherTurn)
            {
                BattleBrain.BattleObject.Context.AnotherTurnGranted = false;
                // Same unit goes again, don't increment roster index
                ActivateCurrentUnit();
                return true;
            }

            // Get current roster
            var units = GetCurrentRosterUnits();

            // Try to find next non-defeated unit
            _currentRosterIndex++;

            while (_currentRosterIndex < units.Count)
            {
                var unit = units[_currentRosterIndex];

                if (!unit.IsDefeatedInCurrentBattle)
                {
                    // Found next active unit
                    ActivateCurrentUnit();
                    return true;
                }

                // Skip defeated unit
                _currentRosterIndex++;
            }

            // All units in this roster have acted, progress to next phase
            return ProgressToNextPhase();
        }

        /// <summary>
        /// Activates the current unit in the current roster.
        /// </summary>
        private void ActivateCurrentUnit()
        {
            var units = GetCurrentRosterUnits();

            if (_currentRosterIndex < 0 || _currentRosterIndex >= units.Count)
            {
                Debug.LogError($"TurnRotisserie: Invalid roster index {_currentRosterIndex}");
                return;
            }

            var activeUnit = units[_currentRosterIndex];

            if (activeUnit == null)
            {
                Debug.LogError(
                    $"TurnRotisserie: Active unit at index {_currentRosterIndex} is null"
                );
                return;
            }

            activeUnit.IncrementCombatCount();
            activeUnit.IncrementTurnsAlive();

            // Update battle context
            ChangeBattleContextData(activeUnit);
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
                    _brain.PublishPlayerTurnStarted();
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

            // Activate first unit of new phase
            return Progress();
        }

        /// <summary>
        /// Updates BattleContext with the active unit and its targets/allies.
        /// </summary>
        public OperationResult ChangeBattleContextData(CharacterInstance activeUnit)
        {
            var context = BattleBrain.BattleObject.Context;

            try
            {
                context.UnitInstance = activeUnit;
                context.Targets.Clear();
                context.Allies.Clear();
                context.ThirdParty.Clear();

                PopulateContext(context);

                context.AdjacentUnits =
                    new Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations.Adjacency(
                        activeUnit
                    );

                // Check if player-controlled
                if (_currentTurnOrder is TurnOrder.PlayerStart or TurnOrder.PlayerEnd)
                {
                    _brain.PublishPlayerControlledUnitActivated(activeUnit);
                }

                return OperationResult.SuccessResult();
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
                    if (unit.IsDefeatedInCurrentBattle || unit == context.UnitInstance)
                    {
                        continue;
                    }

                    if (role == Role.Ally)
                    {
                        context.Allies.Add(unit);
                    }
                    else
                    {
                        context.Targets.Add(unit);
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
                    BattleBrain.BattleObject.Context.AnotherTurnGranted = true;
                    return OperationResult.SuccessResult();
                }
            }
            return OperationResult.Failure("Cannot grant another turn to unit.");
        }
    }
}
