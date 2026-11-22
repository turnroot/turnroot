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
    public partial class TurnRotisserie : MonoBehaviour
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

        public void ResetForBattle()
        {
            StopAllCoroutines();
            _currentPhaseOrder = BattlePhaseOrder.PlayerPhaseStart;
            _currentRosterIndex = -1;
            _unitsWithPendingMoveAnimation.Clear();
            _unitsWithPendingSwapAnimation.Clear();
            _pendingEndTurnUnitIdAwaitingAnimation = null;
            _sceneFlow = FindFirstObjectByType<Utilities.AbstractScripts.BattleSceneFlow>();
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




        #region Event Handlers

        #endregion
    }
}
