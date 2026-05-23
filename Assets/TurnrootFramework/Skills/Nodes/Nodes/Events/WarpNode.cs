using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Teleports a unit to a new position based on a relationship to an ally.
    /// All four modes execute a one-way MoveCommand so brain events
    /// (OnMoveCompleted, OnUnitMoved, etc.) fire for animation/SFX hooks.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Warp")]
    [NodeLabel("Teleport unit to/from an ally's position")]
    public class WarpNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

        [Tooltip(
            "CasterToFarthestAlly: caster warps adjacent to farthest ally\n"
                + "FarthestAllyToCaster: farthest ally warps adjacent to caster (right-first clockwise)\n"
                + "CasterToStrongestAlly: caster warps adjacent to ally with most current HP\n"
                + "StrongestAllyToCaster: that ally warps adjacent to the caster"
        )]
        public WarpMode mode = WarpMode.CasterToFarthestAlly;

        [Tooltip(
            "Maximum tile range between caster and ally (0 = use GameplayGeneralSettings.MaxWarpDistance)"
        )]
        [Range(0, 40)]
        public int maxDistance = 0;

        // 4-directional offsets, clockwise starting from the right (+X axis).
        // Used to find the first free adjacent square around the anchor unit.
        private static readonly Vector2Int[] ClockwiseOffsets =
        {
            new(1, 0),
            new(0, -1),
            new(-1, 0),
            new(0, 1),
        };

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var caster = context.Unit.UnitInstance;
            if (!ValidationHelper.ValidateNotNull(caster, nameof(caster)))
            {
                return;
            }

            var allies = context.Participants?.Allies;
            if (allies == null || allies.Count == 0)
            {
                "Warp: No allies available in context".LogWarning();
                return;
            }

            int effectiveMaxDist =
                maxDistance > 0
                    ? maxDistance
                    : (
                        Turnroot.GameSettings.GameplayGeneralSettings.Instance?.GetMaxWarpDistance()
                        ?? 20
                    );

            switch (mode)
            {
                case WarpMode.CasterToFarthestAlly:
                    ExecuteWarp(
                        context,
                        caster,
                        allies,
                        useFarthest: true,
                        casterMoves: true,
                        effectiveMaxDist
                    );
                    break;

                case WarpMode.FarthestAllyToCaster:
                    ExecuteWarp(
                        context,
                        caster,
                        allies,
                        useFarthest: true,
                        casterMoves: false,
                        effectiveMaxDist
                    );
                    break;

                case WarpMode.CasterToStrongestAlly:
                    ExecuteWarp(
                        context,
                        caster,
                        allies,
                        useFarthest: false,
                        casterMoves: true,
                        effectiveMaxDist
                    );
                    break;

                case WarpMode.StrongestAllyToCaster:
                    ExecuteWarp(
                        context,
                        caster,
                        allies,
                        useFarthest: false,
                        casterMoves: false,
                        effectiveMaxDist
                    );
                    break;
            }
        }

        private static void ExecuteWarp(
            BattleContext context,
            CharacterInstance caster,
            List<CharacterInstance> allies,
            bool useFarthest,
            bool casterMoves,
            int maxDist
        )
        {
            // Select the target ally (farthest from caster by tile distance, or highest current HP)
            var targetAlly = FindAlly(allies, caster, useFarthest, maxDist);
            if (targetAlly == null)
            {
                string criterion = useFarthest ? "farthest" : "strongest";
                $"Warp: No valid {criterion} ally found within range {maxDist}".LogWarning();
                return;
            }

            // The unit that stays put is the anchor; the moving unit goes adjacent to it
            CharacterInstance anchor = casterMoves ? targetAlly : caster;
            CharacterInstance mover = casterMoves ? caster : targetAlly;

            var destSquare = FindAdjacentFreeSquare(anchor.MapGridPosition, context.MapGrid);
            if (!destSquare.HasValue)
            {
                $"Warp: No free square adjacent to {anchor.CharacterTemplate.DisplayName} — surrounded".LogWarning();
                return;
            }

            // MoveCommand fires OnUnitMoved, OnMoveCompleted, etc. — animation hooks are covered
            var result = context.MoveUnitToPointInt(mover, destSquare.Value);
            if (!result.Success)
            {
                $"Warp: MoveCommand failed for {mover.CharacterTemplate.DisplayName}".LogWarning();
                return;
            }

            string modeDesc = casterMoves
                ? $"{mover.CharacterTemplate.DisplayName} warped adjacent to {anchor.CharacterTemplate.DisplayName}"
                : $"{mover.CharacterTemplate.DisplayName} warped adjacent to caster";
            $"Warp: {modeDesc} at {destSquare.Value}".LogInfo();
        }

        /// <summary>
        /// Finds the best ally — either the farthest (by tile distance) or the strongest
        /// (by current HP), excluding the caster and respecting the max-range limit.
        /// </summary>
        private static CharacterInstance FindAlly(
            List<CharacterInstance> allies,
            CharacterInstance caster,
            bool useFarthest,
            int maxDist
        )
        {
            CharacterInstance result = null;
            float bestScore = float.NegativeInfinity;
            var casterPos = caster.MapGridPosition;

            foreach (var ally in allies)
            {
                if (ally == null || ally == caster || ally.IsDefeatedInCurrentBattle)
                {
                    continue;
                }

                float dist = Vector2Int.Distance(ally.MapGridPosition, casterPos);
                if (dist > maxDist)
                {
                    continue;
                }

                float score = useFarthest
                    ? dist
                    : (ally.GetBoundedStat(BoundedStatType.Health)?.Current ?? 0f);

                if (score > bestScore)
                {
                    bestScore = score;
                    result = ally;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the first unoccupied tile adjacent to <paramref name="anchor"/>,
        /// checking clockwise starting from the right (+X) direction.
        /// Returns null if all four adjacent tiles are occupied or out of bounds.
        /// </summary>
        private static Vector2Int? FindAdjacentFreeSquare(Vector2Int anchor, MapGrid grid)
        {
            if (grid == null)
            {
                return null;
            }

            foreach (var offset in ClockwiseOffsets)
            {
                var candidate = anchor + offset;
                var point = grid.GetGridPoint(candidate.x, candidate.y);
                if (point != null && !point.IsOccupied)
                {
                    return candidate;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Defines which unit moves and how the target ally is selected.
    /// </summary>
    public enum WarpMode
    {
        CasterToFarthestAlly, // caster warps to a tile adjacent to the farthest ally
        FarthestAllyToCaster, // farthest ally warps to a tile adjacent to the caster
        CasterToStrongestAlly, // caster warps to a tile adjacent to the ally with most current HP
        StrongestAllyToCaster, // that ally warps to a tile adjacent to the caster
    }
}
