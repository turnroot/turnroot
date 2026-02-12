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
        #region Desperation
        private void EvaluateDesperationGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            float bestUtility = 0f;

            // Health summary used across all desperation checks
            float healthPercent = _context.Unit.UnitInstance.GetHealthPercentage();
            bool isCritical = healthPercent < 0.2f;

            EvaluateDesperationHealSelf(
                goals,
                behavior,
                healthPercent,
                isCritical,
                ref bestUtility
            );
            EvaluateDesperationKillGoals(goals, behavior, isCritical, ref bestUtility);
            EvaluateDesperationDefensiveRetreat(
                goals,
                behavior,
                healthPercent,
                isCritical,
                ref bestUtility
            );
        }

        private void EvaluateDesperationHealSelf(
            List<AIGoal> goals,
            CharacterBehavior behavior,
            float healthPercent,
            bool isCritical,
            ref float bestUtility
        )
        {
            // Selfish units (SS < 0.7) prioritize self-preservation when below 50% hp
            if (!(behavior.SelfishSelfless < 0.7f && healthPercent <= 0.5f))
            {
                return;
            }

            float baseUtility = 8f + (1f - healthPercent) * 15f;
            if (isCritical)
            {
                baseUtility *= 1.5f;
            }

            baseUtility += behavior.SoldierLoneWolf * 5f; // lone wolves prefer self-heal

            if (behavior.BloodthirstGreed < 0.3f)
            {
                baseUtility *= 0.7f; // Berserkers less likely to heal
            }

            if (baseUtility > bestUtility)
            {
                bestUtility = baseUtility;
                goals.Add(
                    new AIGoal
                    {
                        Type = AIGoal.GoalType.HealSelf,
                        UtilityScore = baseUtility,
                        Target = _context.Unit.UnitInstance,
                        Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                            _context.Unit.UnitInstance.MapGridPosition,
                            _context.MapGrid
                        ),
                    }
                );
            }
        }

        private void EvaluateDesperationKillGoals(
            List<AIGoal> goals,
            CharacterBehavior behavior,
            bool isCritical,
            ref float bestUtility
        )
        {
            var targets = _context.Participants.Targets;
            for (int ti = 0; ti < (targets?.Count ?? 0); ti++)
            {
                var target = targets[ti];
                var targetGridPoint = target.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    _context.MapGrid
                );

                if (!IsAttackable(targetGridPoint))
                {
                    continue;
                }

                float utility = 8f;
                utility += (1f - behavior.MindlessCunning) * 3f; // mindless units attack recklessly
                utility += (1f - behavior.BloodthirstGreed) * 3f; // bloodthirst bonus

                float targetHealthPercent =
                    target.GetBoundedStat(Characters.Stats.BoundedStatType.Health).Current
                    / target.GetBoundedStat(Characters.Stats.BoundedStatType.Health).Max;

                if (targetHealthPercent < 0.3f)
                {
                    utility += 8f; // Killing blow opportunity
                }
                else if (targetHealthPercent < 0.5f)
                {
                    utility += 4f; // Wounded target
                }

                var distance = Vector2.Distance(
                    _context.Unit.UnitInstance.MapGridPosition,
                    target.MapGridPosition
                );
                utility += Mathf.Max(0, 3f - distance);

                if (isCritical && behavior.MindlessCunning > 0.5f)
                {
                    utility *= 0.8f; // Smart units less likely to suicide attack
                }

                var bestWeapon = ChooseBestWeaponForTarget(target);

                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    goals.Add(
                        new AIGoal
                        {
                            Type = AIGoal.GoalType.KillEnemy,
                            UtilityScore = utility,
                            Target = target,
                            Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                                _context.Unit.UnitInstance.MapGridPosition,
                                _context.MapGrid
                            ),
                            ChosenWeapon = bestWeapon,
                        }
                    );
                }
            }
        }

        private Objects.ObjectItemInstance ChooseBestWeaponForTarget(CharacterInstance target)
        {
            var availableWeapons = _context.Unit.UnitInstance.RangeWeaponsCache;
            Objects.ObjectItemInstance bestWeapon = null;
            float bestPotential = 0f;

            foreach (var w in availableWeapons)
            {
                int perHit = DamageCalculator.CalculatePotentialDamage(
                    _context.Unit.UnitInstance,
                    target,
                    w,
                    _context
                );
                int attackCount = DamageCalculator.CalculateAttackCount(
                    _context.Unit.UnitInstance,
                    target
                );
                float total = perHit * attackCount;
                if (w == _context.Unit.UnitInstance.GetEquippedWeapon())
                {
                    total *= 1.05f; // small preference for equipped weapon
                }

                if (total > bestPotential)
                {
                    bestPotential = total;
                    bestWeapon = w;
                }
            }

            return bestWeapon;
        }

        private void EvaluateDesperationDefensiveRetreat(
            List<AIGoal> goals,
            CharacterBehavior behavior,
            float healthPercent,
            bool isCritical,
            ref float bestUtility
        )
        {
            if (!(behavior.BrashWary > 0.3f))
            {
                return;
            }

            var enemyData = FindClosestAndFurthestEnemies(
                new List<CharacterInstance>(_context.Participants.Targets)
            );
            float currentDistanceToEnemy = enemyData.closestDist;

            using var safeTilesPooled = PooledDictionary<MapGridPoint, float>.Get();
            var safeTiles = safeTilesPooled.Dictionary;

            foreach (var tile in _reusableMoveTiles)
            {
                float newDistanceToEnemy = Vector2.Distance(
                    tile.Key.Coordinates(),
                    enemyData.closest
                );

                bool isIncreasingDistance = newDistanceToEnemy > currentDistanceToEnemy;
                bool isNearAlly = false;

                if (behavior.SoldierLoneWolf < 0.5f)
                {
                    var allies = _context.Participants.Allies;
                    for (int ai = 0; ai < (allies?.Count ?? 0); ai++)
                    {
                        var ally = allies[ai];
                        if (ally == _context.Unit.UnitInstance)
                        {
                            continue;
                        }

                        var distanceToAlly = Vector2.Distance(
                            tile.Key.Coordinates(),
                            ally.MapGridPosition
                        );

                        if (distanceToAlly <= 1.5f)
                        {
                            isNearAlly = true;
                            break;
                        }
                    }
                }

                if (!(isIncreasingDistance || isNearAlly))
                {
                    continue;
                }

                float utility = 5f + (behavior.BrashWary * 5f);

                if (isCritical)
                {
                    utility += 5f + (5f * behavior.MindlessCunning);
                }
                else if (healthPercent < 0.5f)
                {
                    utility += 5f;
                }

                if (isIncreasingDistance)
                {
                    float distanceGain = newDistanceToEnemy - currentDistanceToEnemy;
                    utility += distanceGain * 2f;
                }

                if (isNearAlly)
                {
                    utility += 3f + (behavior.SelfishSelfless * 3f);
                }

                safeTiles[tile.Key] = utility;
            }

            // Add retreat goals
            foreach (var safeTile in safeTiles)
            {
                if (safeTile.Value > bestUtility)
                {
                    bestUtility = safeTile.Value;
                    goals.Add(
                        new AIGoal
                        {
                            Type = AIGoal.GoalType.DefensiveRetreat,
                            UtilityScore = safeTile.Value,
                            Target = _context.Unit.UnitInstance,
                            Destination = safeTile.Key,
                        }
                    );
                }
            }
        }
        #endregion
    }
}
