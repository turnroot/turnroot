using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleInputControllerBrain : BrainComponent
    {

        // TODO: Implement damage preview system (priorities.md Phase 4.1) - CalculateAttackPreview with hit%/crit%/counters
        // TODO: Implement movement path preview (priorities.md Phase 4.2) - CalculateMovementPath with A* pathfinding

        public bool ValidateTileSelection(MapGridPoint point)
        {
            var currentState = _playerTurnFlow?.GetCurrentState() ?? PlayerTurnStates.Inactive;

            return currentState switch
            {
                PlayerTurnStates.MoveActionChosenChoosingDestination => _validMoveTiles.ContainsKey(
                    point
                ),
                PlayerTurnStates.AttackActionChosenChoosingTarget => _validAttackTiles.ContainsKey(
                    point
                ),
                _ => false,
            };
            // TODO: Comprehensive action validation (weapons, skills, rescue/trade requirements, audio/visual feedback)
        }

        private CharacterInstance GetUnitAtPosition(MapGridPoint position)
        {
            var cache = BattleContext.GetCurrentUnitPositions();
            return cache.TryGetValue(position.CoordinatesInt, out var unit) ? unit : null;
        }
    }
}
