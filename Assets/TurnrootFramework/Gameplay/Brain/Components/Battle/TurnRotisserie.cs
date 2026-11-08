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
    public enum BattlePhaseOrder
    {
        PlayerPhaseStart = 0,
        PlayerPhaseEnd = 1,
        EnemyPhaseStart = 2,
        EnemyPhaseEnd = 3,
        ThirdPartyPhaseStart = 4,
        ThirdPartyPhaseEnd = 5,
    }

    /// <summary>
    /// Advances eligible unit turns and faction phases within each battle round.
    /// Reads participants from BattleContext, populated by BattleBrain during initialization.
    /// </summary>
    public class TurnRotisserie : MonoBehaviour
    {
        #region Dependencies (Set by BattleBrain)

        [HideInInspector]
        public BattleBrain battleBrain;

        private Brain.Brain Brain => battleBrain?.Brain;
        private BattleContext Context => battleBrain?.BattleObject.Context;

        [HideInInspector]
        public Utilities.AbstractScripts.BattleSceneFlow _sceneFlow;

        #endregion

        #region State

        private BattlePhaseOrder _currentPhaseOrder = BattlePhaseOrder.PlayerPhaseStart;

        // spin index starts at -1 so the first call to AdvanceToNextUnitOrPhase() increments
        // it to zero and activates the first member of the roster.
        private int _currentRosterIndex = -1;

        private bool UnitTakesAnotherTurn =>
            Context?.Flags?.ActiveUnitFlags?.AnotherTurnGranted ?? false;

        // When EndTurn is requested for AI/third-party units, keep the camera/state on the
        // active unit until its movement/swap visuals are done.
        private readonly HashSet<string> _unitsWithPendingMoveAnimation = new();
        private readonly HashSet<string> _unitsWithPendingSwapAnimation = new();
        private string _pendingEndTurnUnitIdAwaitingAnimation;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy() => CleanupBattle();

        /// <summary>
        /// Cleanup method to be called when battle ends.
        /// Unsubscribes from events and clears cached references.
        /// </summary>
        public void CleanupBattle()
        {
            if (Brain != null)
            {
                Brain.OnUnitTurnAdvanceRequested -= HandleUnitTurnAdvanceRequested;
                Brain.OnCharacterMoveStarted -= HandleCharacterMoveStarted;
                Brain.OnMoveAnimationCompleted -= HandleMoveAnimationCompleted;
                Brain.OnSwapStarted -= HandleSwapStarted;
                Brain.OnSwapAnimationCompleted -= HandleSwapAnimationCompleted;
            }

            _unitsWithPendingMoveAnimation.Clear();
            _unitsWithPendingSwapAnimation.Clear();
            _pendingEndTurnUnitIdAwaitingAnimation = null;
            _sceneFlow = null;
        }

        private void HandleUnitTurnAdvanceRequested(CharacterInstance unit)
        {
            // Handle EndTurn for all factions. For AI/NPC turns, defer advancement until
            // the active unit's movement/swap visuals complete.
            var active = GetActiveUnit();
            if (active == null || active != unit)
            {
                return;
            }

            var shouldWaitForVisuals =
                _currentPhaseOrder
                    is BattlePhaseOrder.EnemyPhaseStart
                        or BattlePhaseOrder.EnemyPhaseEnd
                        or BattlePhaseOrder.ThirdPartyPhaseStart
                        or BattlePhaseOrder.ThirdPartyPhaseEnd;

            if (shouldWaitForVisuals && HasPendingVisualForUnit(unit))
            {
                _pendingEndTurnUnitIdAwaitingAnimation = unit.Id;
                return;
            }

            AdvanceToNextUnitOrPhase();
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

            Brain.OnUnitTurnAdvanceRequested += HandleUnitTurnAdvanceRequested;
            Brain.OnCharacterMoveStarted += HandleCharacterMoveStarted;
            Brain.OnMoveAnimationCompleted += HandleMoveAnimationCompleted;
            Brain.OnSwapStarted += HandleSwapStarted;
            Brain.OnSwapAnimationCompleted += HandleSwapAnimationCompleted;
            Brain.OnPlayerUnitTurnStateChanged += state =>
            {
                var active = GetActiveUnit();
                active?.RecalculateCombatRates();
            };

            // Cache scene flow reference for interrupt coordination
            _sceneFlow = FindFirstObjectByType<Utilities.AbstractScripts.BattleSceneFlow>();

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
        public bool AdvanceToNextUnitOrPhase()
        {
            if (
                battleBrain != null
                && battleBrain.CurrentTurnNumber == 0
                && _currentPhaseOrder == BattlePhaseOrder.PlayerPhaseStart
            )
            {
                battleBrain.IncrementTurnNumber();
                PublishBattleRoundStarted();
            }

            // Check if current unit gets another turn
            if (UnitTakesAnotherTurn)
            {
                Context.Flags.ActiveUnitFlags.AnotherTurnGranted = false;
                return ActivateCurrentUnit().Success;
            }

            // Try to find next active unit in current roster
            _currentRosterIndex++;

            var units = GetCurrentRosterUnits();
            if (units.Count == 0)
            {
                // End phases are transition-only; they don't activate per-unit turns.
                if (
                    _currentPhaseOrder
                    is BattlePhaseOrder.PlayerPhaseEnd
                        or BattlePhaseOrder.EnemyPhaseEnd
                        or BattlePhaseOrder.ThirdPartyPhaseEnd
                )
                {
                    return AdvanceToNextFactionPhase();
                }

                // This should not happen for start phases and likely indicates roster setup issues.
                "TurnRotisserie: No units found in current roster!".LogError();
                Debug.Break();
                return false;
            }
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
            var result = AdvanceToNextFactionPhase();

            // if the rotisserie indicated the battle should end, make sure any
            // remaining round-end handlers still run. (AdvanceToNextFactionPhase never
            // publishes BattleRoundEnded in this case.)
            if (!result)
            {
                PublishBattleRoundEnded();
            }

            return result;
        }

        #endregion

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
                BattlePhaseOrder.EnemyPhaseStart => Context.Participants.Targets,
                BattlePhaseOrder.ThirdPartyPhaseStart => Context.Participants.ThirdParty,
                BattlePhaseOrder.PlayerPhaseEnd or BattlePhaseOrder.EnemyPhaseEnd or BattlePhaseOrder.ThirdPartyPhaseEnd =>
                    new List<CharacterInstance>(),
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

            $"TurnRotisserie: Setting active unit to {activeUnit.CharacterTemplate.DisplayName}".LogInfo();

            // Publish the unit-turn completion before changing
            // Context.Unit so skill graphs still see the correct outgoing unit. This covers all
            // unit types (player, enemy, third-party) since EndUnitTurnCommand is not always called
            // for enemy/third-party units.
            var previousUnit = Context.Unit?.UnitInstance;
            if (previousUnit != null)
            {
                Brain.PublishUnitTurnEnded(previousUnit);
            }

            // Set active unit in context
            Context.Unit.UnitInstance = activeUnit;
            if (Context.Flags?.ActiveUnitFlags == null)
            {
                Context.Flags.ActiveUnitFlags = new UnitFlag();
            }
            Context.Flags.ActiveUnitFlags.Unit = activeUnit;

            // Update participants data for the newly active unit
            Context.UpdateAdjacentUnits();
            Context.UpdateTargetsInRange();

            // Notify all systems that a new unit's turn has started (player, enemy, or third party).
            Brain.PublishUnitTurnStarted(activeUnit);

            if (_currentPhaseOrder is BattlePhaseOrder.PlayerPhaseStart or BattlePhaseOrder.PlayerPhaseEnd)
            {
                Brain.PublishPlayerControlledUnitActivated(activeUnit);
            }

            if (_currentPhaseOrder is BattlePhaseOrder.EnemyPhaseStart or BattlePhaseOrder.ThirdPartyPhaseStart)
            {
                StartCoroutine(ExecuteAITurnCoroutine(activeUnit));
            }

            return OperationResult.Successful();
        }

        private IEnumerator ExecuteAITurnCoroutine(CharacterInstance expectedUnit)
        {
            // Let camera/state listeners react to the unit activation first.
            yield return null;

            var context = Context;
            var active = GetActiveUnit();
            if (context == null || active == null || active != expectedUnit)
            {
                yield break;
            }

            if (context.AIHelper == null)
            {
                "TurnRotisserie: AIHelper missing during AI turn; ending turn to prevent lockup".LogWarning();
                context.CompleteActiveUnitTurn();
                yield break;
            }

            context.AIHelper?.PickTileAndAction();
        }

        private bool HasPendingVisualForUnit(CharacterInstance unit)
        {
            return unit != null && (_unitsWithPendingMoveAnimation.Contains(unit.Id)
                    || _unitsWithPendingSwapAnimation.Contains(unit.Id));
        }

        private void TryProgressDeferredEndTurn()
        {
            if (string.IsNullOrEmpty(_pendingEndTurnUnitIdAwaitingAnimation))
            {
                return;
            }

            var active = GetActiveUnit();
            if (active == null || active.Id != _pendingEndTurnUnitIdAwaitingAnimation)
            {
                _pendingEndTurnUnitIdAwaitingAnimation = null;
                return;
            }

            if (HasPendingVisualForUnit(active))
            {
                return;
            }

            _pendingEndTurnUnitIdAwaitingAnimation = null;
            AdvanceToNextUnitOrPhase();
        }

        private void HandleCharacterMoveStarted(
            CharacterInstance character,
            Maps.MapGridPoint targetPoint
        )
        {
            if (character == null)
            {
                return;
            }

            _unitsWithPendingMoveAnimation.Add(character.Id);
        }

        private void HandleMoveAnimationCompleted(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            _unitsWithPendingMoveAnimation.Remove(character.Id);
            TryProgressDeferredEndTurn();
        }

        private void HandleSwapStarted(CharacterInstance unit, CharacterInstance target)
        {
            if (unit != null)
            {
                _unitsWithPendingSwapAnimation.Add(unit.Id);
            }

            if (target != null)
            {
                _unitsWithPendingSwapAnimation.Add(target.Id);
            }
        }

        private void HandleSwapAnimationCompleted(CharacterInstance unit, CharacterInstance target)
        {
            if (unit != null)
            {
                _unitsWithPendingSwapAnimation.Remove(unit.Id);
            }

            if (target != null)
            {
                _unitsWithPendingSwapAnimation.Remove(target.Id);
            }

            TryProgressDeferredEndTurn();
        }

        #endregion

        #region Event Handlers

        #endregion
    }
}
