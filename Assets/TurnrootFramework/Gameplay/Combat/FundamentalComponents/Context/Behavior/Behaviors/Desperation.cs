using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Desperation
        private void EvaluateDesperationGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            float BestUtility = 0f;

            // Calculate health status
            float healthPercent = _context.Unit.UnitInstance.GetHealthPercentage();
            bool isCritical = healthPercent < 0.2f;

            // --- HEAL SELF (Higher Priority) ---
            // Selfish units (SS < 0.7) prioritize self-preservation
            if (behavior.SelfishSelfless < 0.7f && healthPercent <= 0.5f)
            {
                // Dramatically increase utility when critically wounded
                float baseUtility = 8f + (1f - healthPercent) * 15f;

                // Critical health multiplier
                if (isCritical)
                {
                    baseUtility *= 1.5f;
                }

                // Lone wolves prioritize self-healing even more
                baseUtility += behavior.SoldierLoneWolf * 5f;

                // Reduce if very bloodthirsty (would rather die fighting)
                if (behavior.BloodthirstGreed < 0.3f)
                {
                    baseUtility *= 0.7f; // Berserkers less likely to heal
                }

                if (baseUtility > BestUtility)
                {
                    BestUtility = baseUtility;
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

            // --- KILL ENEMY (Desperate Attacks) ---
            var targets = _context.Participants.Targets;
            for (int ti = 0; ti < (targets?.Count ?? 0); ti++)
            {
                var target = targets[ti];
                var targetGridPoint = target.UnitPositionToMapGridPoint(
                    target.MapGridPosition,
                    _context.MapGrid
                );

                if (_reusableAttackTiles.ContainsKey(targetGridPoint))
                {
                    float utility = 8f;

                    // Mindless units attack recklessly
                    utility += (1f - behavior.MindlessCunning) * 3f;

                    // Bloodthirsty units attack even when desperate
                    utility += (1f - behavior.BloodthirstGreed) * 3f;

                    // MAJOR bonus for low-health targets
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

                    // Distance bonus
                    var distance = Vector2.Distance(
                        _context.Unit.UnitInstance.MapGridPosition,
                        target.MapGridPosition
                    );
                    utility += Mathf.Max(0, 3f - distance);

                    // PENALTY for attacking when critically wounded (survival instinct)
                    if (isCritical && behavior.MindlessCunning > 0.5f)
                    {
                        utility *= 0.8f; // Smart units less likely to suicide attack
                    }

                    // Prefer weapons that provide higher potential damage - use DamageCalculator to evaluate
                    var availableWeapons = _context.Unit.UnitInstance.RangeWeaponsCache;
                    ObjectItemInstance bestWeapon = null;
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

                    if (utility > BestUtility)
                    {
                        BestUtility = utility;
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

            // --- DEFENSIVE RETREAT ---
            if (behavior.BrashWary > 0.3f)
            {
                // OPTIMIZATION: Calculate enemy positions once, not per tile
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

                    // Soldiers seek allies when retreating
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

                    if (isIncreasingDistance || isNearAlly)
                    {
                        float utility = 5f + (behavior.BrashWary * 5f); // 6.5-10 base

                        // Major bonus for critical health
                        if (isCritical)
                        {
                            utility += 5f + (5f * behavior.MindlessCunning); // Smart units FLEE when dying
                        }
                        else if (healthPercent < 0.5f)
                        {
                            utility += 5f;
                        }

                        // Distance improvement bonus
                        if (isIncreasingDistance)
                        {
                            float distanceGain = newDistanceToEnemy - currentDistanceToEnemy;
                            utility += distanceGain * 2f;
                        }

                        // Ally proximity bonus
                        if (isNearAlly)
                        {
                            utility += 3f + (behavior.SelfishSelfless * 3f); // Up to +6 for selfless
                        }

                        safeTiles[tile.Key] = utility;
                    }
                }

                // Add retreat goals
                foreach (var safeTile in safeTiles)
                {
                    if (safeTile.Value > BestUtility)
                    {
                        BestUtility = safeTile.Value;
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
        }
        #endregion
    }
}
