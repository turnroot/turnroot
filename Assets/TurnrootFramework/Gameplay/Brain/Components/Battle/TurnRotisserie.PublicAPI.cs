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
    }
}