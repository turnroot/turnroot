using System.Collections;
using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles swap animation: when two units exchange battlefield positions, each travels a
    /// short arced path that curves laterally so they pass beside each other rather than
    /// clipping through. Reuses <see cref="AnimateAlongSplinePath"/> from Spline.cs.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        private void HandleSwapLogicCompleted(CharacterInstance unit, CharacterInstance target)
        {
            if (unit == null || target == null)
            {
                Brain.PublishSwapAnimationCompleted(unit, target);
                return;
            }

            var mapGrid = Brain.battleBrain.BattleObject.MapGrid;

            // NOTE: By the time this fires, SwapCommand has already mutated MapGridPosition on
            // both units to their post-swap coordinates. We therefore read the new (destination)
            // positions now and reconstruct the original positions from them.
            var unitDestWorld = mapGrid.GetTerrainAdjustedWorldPosition(unit.MapGridPosition);
            var targetDestWorld = mapGrid.GetTerrainAdjustedWorldPosition(target.MapGridPosition);

            // The visual models are still sitting at the pre-swap world positions, so we read
            // them directly from the transforms rather than from the authoritative grid state.
            var unitModel = GetModelForUnit(unit.Id);
            var targetModel = GetModelForUnit(target.Id);

            var unitStartWorld = unitModel != null ? unitModel.transform.position : targetDestWorld;
            var targetStartWorld =
                targetModel != null ? targetModel.transform.position : unitDestWorld;

            // Arc midpoints: offset each unit laterally in opposite directions so they
            // walk in a shallow "X" past each other instead of phasing through each other.
            var midpoint = (unitStartWorld + unitDestWorld) * 0.5f;
            var direction = (unitDestWorld - unitStartWorld);
            direction.y = 0f;

            Vector3 perpendicular;
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                perpendicular = new Vector3(-direction.z, 0f, direction.x);
            }
            else
            {
                perpendicular = Vector3.right;
            }

            var offset = perpendicular * _settings.SwapArcOffset;

            var unitPath = new List<Vector3> { unitStartWorld, midpoint + offset, unitDestWorld };

            var targetPath = new List<Vector3>
            {
                targetStartWorld,
                midpoint - offset,
                targetDestWorld,
            };

            StartCoroutine(AnimateSwapCoroutine(unit, target, unitPath, targetPath));
        }

        private IEnumerator AnimateSwapCoroutine(
            CharacterInstance unit,
            CharacterInstance target,
            List<Vector3> unitPath,
            List<Vector3> targetPath
        )
        {
            // Clear tile highlights once before both animations start, the same way a regular
            // move does, but only once since both units share the same board.
            Brain.battleBrain.BattleObject.TileHighlighter.ClearAll();

            var remaining = 2;

            StartCoroutine(
                AnimateAlongSplinePath(
                    unit,
                    unitPath,
                    clearTileHighlights: false,
                    onComplete: () => remaining--
                )
            );

            StartCoroutine(
                AnimateAlongSplinePath(
                    target,
                    targetPath,
                    clearTileHighlights: false,
                    onComplete: () => remaining--
                )
            );

            yield return new WaitUntil(() => remaining == 0);

            Brain.PublishSwapAnimationCompleted(unit, target);
        }
    }
}
