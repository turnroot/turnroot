using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public partial class BattlePreparationObject
    {
        #region High-Level Placement Operations

        /// <summary>
        /// Place a unit at a position.
        /// </summary>
        public OperationResult PlaceUnit(Vector2Int pos, CharacterInstance unit)
        {
            if (!IsPlayerSpawnPoint(pos))
            {
                return OperationResult.Failure("Cannot place unit: invalid position");
            }

            var data = unit?.CharacterTemplate;
            if (data == null)
            {
                return OperationResult.Failure("Cannot place unit: CharacterData missing");
            }

            placements[pos] = data;
            Brain?.PublishPlacementsSyncRequested(
                persist: false,
                forceApplyPlacementsOnLoad: false
            );
            return OperationResult.Successful();
        }

        /// <summary>
        /// Swap two positions - updates BOTH logical placements AND model tracking.
        /// </summary>
        private void ApplySwap(Vector2Int from, Vector2Int to)
        {
            // Capture data references BEFORE the swap
            placements.TryGetValue(from, out var dataFrom);
            placements.TryGetValue(to, out var dateTo);

            // Update UI
            StartingPositionsComponent.SetSwap(to);

            // Swap logical placements
            (placements[from], placements[to]) = (placements[to], placements[from]);

            // Update model tracking
            var modelA = GetModelAtPosition(from);
            var modelB = GetModelAtPosition(to);
            var unitIdA = GetUnitIdAtPosition(from);
            var unitIdB = GetUnitIdAtPosition(to);

            if (
                modelA != null
                && modelB != null
                && !string.IsNullOrEmpty(unitIdA)
                && !string.IsNullOrEmpty(unitIdB)
            )
            {
                // Swap the registrations
                UnregisterModelAtPosition(from);
                UnregisterModelAtPosition(to);
                RegisterModel(from, modelB, unitIdB);
                RegisterModel(to, modelA, unitIdA);
            }

            // Update visual models (transforms)
            StartingPositionsComponent.SwapModels(from, to);

            // Update instance positions
            var gw = Brain?.gamewideContextBrain;
            if (gw != null)
            {
                var instFrom = dataFrom != null ? gw.FindInstanceByTemplate(dataFrom) : null;
                var instTo = dateTo != null ? gw.FindInstanceByTemplate(dateTo) : null;
                if (instFrom != null)
                {
                    instFrom.MapGridPosition = to;
                }

                if (instTo != null)
                {
                    instTo.MapGridPosition = from;
                }
            }
        }

        /// <summary>
        /// Move a unit from one position to another - updates BOTH logical placements AND model tracking.
        /// </summary>
        private void ApplyMove(Vector2Int from, Vector2Int to)
        {
            // Capture data reference BEFORE the move
            placements.TryGetValue(from, out var dataFrom);

            // Update UI
            StartingPositionsComponent.SetSelected(to);

            // Move logical placement
            placements[to] = placements[from];
            placements.Remove(from);

            // Update model tracking
            var model = GetModelAtPosition(from);
            var unitId = GetUnitIdAtPosition(from);

            if (model != null && !string.IsNullOrEmpty(unitId))
            {
                UnregisterModelAtPosition(from);
                RegisterModel(to, model, unitId);
            }

            // Update visual model (transform)
            StartingPositionsComponent.MoveModel(from, to);

            // Update instance position
            var inst =
                dataFrom != null
                    ? Brain?.gamewideContextBrain?.FindInstanceByTemplate(dataFrom)
                    : null;
            if (inst != null)
            {
                inst.MapGridPosition = to;
            }
        }

        #endregion
    }
}
