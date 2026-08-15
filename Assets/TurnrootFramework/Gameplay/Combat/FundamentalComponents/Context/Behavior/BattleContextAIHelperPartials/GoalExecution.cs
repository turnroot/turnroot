using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Partial class containing AI goal execution, tile computation, and goal selection logic for battle context.
    /// </summary>
    public partial class BattleContextAIHelper
    {
        public void PickTileAndAction()
        {
            // 1. Ensure tiles are computed
            EnsureTilesAreComputed();

            // 2. Create pooled list with proper lifetime management
            using var goalsPooled = PooledList<AIGoal>.Get();
            var potentialGoals = goalsPooled.List;

            // 3. Populate goals
            ChooseEvaluations(potentialGoals);

            // 4. Sort by utility

            if (potentialGoals.Count > 0)
            {
                potentialGoals.Sort((a, b) => b.UtilityScore.CompareTo(a.UtilityScore));

                //5. Add formation bonus to team-oriented units
                ApplyFormationBonus(potentialGoals);

                // 6. Choose and execute the best goal- choose (weighted) from top 3 randomly

                var chosenGoal = SelectWeightedGoal(potentialGoals);

                ExecuteChosenGoal(chosenGoal);
            }
            else
            {
                _context.EndTurn();
            }
        }

        private void ApplyFormationBonus(List<AIGoal> potentialGoals)
        {
            if (modifiedBehaviorSettings.SoldierLoneWolf >= 0.5f)
            {
                return;
            }

            int nearbyAllies = _context.Participants.AdjacentUnits.GetAdjacentAllyCount(_context);
            float formationBonus =
                (nearbyAllies * (2f - modifiedBehaviorSettings.SoldierLoneWolf))
                + 1f
                + modifiedBehaviorSettings.MindlessCunning;

            // Precompute current closest ally path-cost once per unit for efficiency
            var currentStart = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                _context.Unit.UnitInstance.MapGridPosition,
                _context.MapGrid
            );
            float currentClosestAllyDist = PathfinderHelpers.TryFindClosestAllyPathCost(
                _context.MapGrid,
                _context.Unit.UnitInstance,
                currentStart,
                _context.Participants.Allies,
                out float currentCost
            )
                ? currentCost
                : float.MaxValue;

            for (int i = 0; i < potentialGoals.Count; i++)
            {
                var goal = potentialGoals[i];

                if (goal.Type == AIGoal.GoalType.GainPosition && goal.Destination != null)
                {
                    var destStart = goal.Destination;
                    float destClosestAllyDist = PathfinderHelpers.TryFindClosestAllyPathCost(
                        _context.MapGrid,
                        _context.Unit.UnitInstance,
                        destStart,
                        _context.Participants.Allies,
                        out float destCost
                    )
                        ? destCost
                        : float.MaxValue;

                    if (destClosestAllyDist > currentClosestAllyDist + 0.01f)
                    {
                        continue;
                    }
                }

                goal.UtilityScore += formationBonus;
                potentialGoals[i] = goal;
            }
        }

        private AIGoal SelectWeightedGoal(List<AIGoal> potentialGoals)
        {
            AIGoal chosenGoal;
            float roll = Random.Range(0f, 1f);
            chosenGoal =
                potentialGoals.Count == 1 ? potentialGoals[0]
                : potentialGoals.Count == 2
                    ? roll <= 0.9f ? potentialGoals[0]
                        : potentialGoals[1]
                : roll <= 0.85f ? potentialGoals[0]
                : roll <= 0.95f ? potentialGoals[1]
                : potentialGoals[2];

            return chosenGoal;
        }

        private void ExecuteChosenGoal(AIGoal chosenGoal)
        {
            LogChosenGoal(chosenGoal);

            ExecuteGoal(chosenGoal, _context);
        }

        private void LogChosenGoal(AIGoal g) =>
            $"AI Chose Goal: {g.Type} with Utility: {g.UtilityScore}, Action: {g.ActionToTake}, Tile: {g.Destination?.CoordinatesInt}, Target: {g.Target?.Id}".LogInfo();

        private void EnsureTilesAreComputed()
        {
            if (!CanHeal)
            {
                if (_reusableMoveTiles.Count == 0 || HasMovedSinceLastTurn)
                {
                    _reusableMoveTiles.Clear();
                    _reusableAttackTiles.Clear();

                    GetTilesForAINonAlloc(
                        _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                            _context.Unit.UnitInstance.MapGridPosition,
                            _context.MapGrid
                        ),
                        _reusableMoveTiles,
                        _reusableAttackTiles
                    );
                }
            }
            else
            {
                if (
                    _reusableMoveTiles.Count == 0
                    || _reusableHealTiles.Count == 0
                    || HasMovedSinceLastTurn
                )
                {
                    ClearReusableTileLists(true);

                    GetTilesForAIWithHealNonAlloc(
                        _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                            _context.Unit.UnitInstance.MapGridPosition,
                            _context.MapGrid
                        ),
                        _reusableMoveTiles,
                        _reusableAttackTiles,
                        _reusableHealTiles
                    );
                }
            }
        }

        private void ClearReusableTileLists(bool includeHeal = false)
        {
            _reusableMoveTiles.Clear();
            _reusableAttackTiles.Clear();
            if (includeHeal)
            {
                _reusableHealTiles.Clear();
            }
        }
    }
}
