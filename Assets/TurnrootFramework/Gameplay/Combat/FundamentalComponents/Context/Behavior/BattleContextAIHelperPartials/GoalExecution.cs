using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        public void PickTileAndAction()
        {
            // TEMPORARY DEBUG: Force recompute every time
            _reusableMoveTiles.Clear();
            _reusableAttackTiles.Clear();
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

#if UNITY_EDITOR
                LogPotentialGoals(potentialGoals);
#else
                // In non-editor builds we still keep the same flow without verbose logs
#endif
                // 6. Choose and execute the best goal- choose (weighted) from top 3 randomly

                var chosenGoal = SelectWeightedGoal(potentialGoals);

                TurnrootLogger.Log(
                    $"AI Chose Goal: {chosenGoal.Type} with Utility: {chosenGoal.UtilityScore}, Action: {chosenGoal.ActionToTake}, Tile: {chosenGoal.Destination?.CoordinatesInt}, Target: {chosenGoal.Target?.Id}"
                );
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
            float currentClosestAllyDist = float.MaxValue;
            if (
                PathfinderHelpers.TryFindClosestAllyPathCost(
                    _context.MapGrid,
                    _context.Unit.UnitInstance,
                    currentStart,
                    _context.Participants.Allies,
                    out float currentCost
                )
            )
            {
                currentClosestAllyDist = currentCost;
            }

            for (int i = 0; i < potentialGoals.Count; i++)
            {
                var goal = potentialGoals[i];

                if (goal.Type == AIGoal.GoalType.GainPosition && goal.Destination != null)
                {
                    var destStart = goal.Destination;
                    float destClosestAllyDist = float.MaxValue;
                    if (
                        PathfinderHelpers.TryFindClosestAllyPathCost(
                            _context.MapGrid,
                            _context.Unit.UnitInstance,
                            destStart,
                            _context.Participants.Allies,
                            out float destCost
                        )
                    )
                    {
                        destClosestAllyDist = destCost;
                    }

                    if (destClosestAllyDist > currentClosestAllyDist + 0.01f)
                    {
                        continue;
                    }
                }

                goal.UtilityScore += formationBonus;
                potentialGoals[i] = goal;
            }
        }

        private void LogPotentialGoals(List<AIGoal> potentialGoals)
        {
#if UNITY_EDITOR
            TurnrootLogger.Log("AI Potential Goals after formation bonus:");
            foreach (var goal in potentialGoals)
            {
                TurnrootLogger.Log(
                    $"Goal: {goal.Type}, Utility: {goal.UtilityScore}, Action: {goal.ActionToTake}, Tile: {goal.Destination?.CoordinatesInt}, Target: {goal.Target?.Id}"
                );
            }
#endif
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
            TurnrootLogger.Log(
                $"AI Chose Goal: {chosenGoal.Type} with Utility: {chosenGoal.UtilityScore}, Action: {chosenGoal.ActionToTake}, Tile: {chosenGoal.Destination?.CoordinatesInt}, Target: {chosenGoal.Target?.Id}"
            );

            ExecuteGoal(chosenGoal, _context);
        }

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
                    _reusableMoveTiles.Clear();
                    _reusableAttackTiles.Clear();
                    _reusableHealTiles.Clear();

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
    }
}
