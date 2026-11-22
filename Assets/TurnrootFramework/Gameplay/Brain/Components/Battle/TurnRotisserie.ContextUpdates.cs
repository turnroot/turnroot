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

            if (
                _currentPhaseOrder
                is BattlePhaseOrder.PlayerPhaseStart
                    or BattlePhaseOrder.PlayerPhaseEnd
            )
            {
                Brain.PublishPlayerControlledUnitActivated(activeUnit);
            }

            if (
                _currentPhaseOrder
                is BattlePhaseOrder.EnemyPhaseStart
                    or BattlePhaseOrder.ThirdPartyPhaseStart
            )
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
            return unit != null
                && (
                    _unitsWithPendingMoveAnimation.Contains(unit.Id)
                    || _unitsWithPendingSwapAnimation.Contains(unit.Id)
                );
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
    }
}