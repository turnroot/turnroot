using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Ally Goals

        private void EvaluateHealAlliesGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            using var healGoalsPooled = PooledList<AIGoal>.Get();
            var healGoals = healGoalsPooled.List;
            var allies = _context.Participants.Allies;
            for (int ai = 0; ai < (allies?.Count ?? 0); ai++)
            {
                var ally = allies[ai];
                var allyGridPoint = ally.UnitPositionToMapGridPoint(
                    ally.MapGridPosition,
                    _context.MapGrid
                );

                // Check if ally is in heal range
                if (IsHealable(allyGridPoint))
                {
                    float utility = CalculateHealUtility(ally, behavior);

                    goals.Add(
                        new AIGoal
                        {
                            Type = AIGoal.GoalType.HealAlly,
                            UtilityScore = utility,
                            Target = ally,
                            Destination = DestinationFromTargetGridPoint(allyGridPoint),
                            ActionToTake = AIGoal.Action.Heal,
                        }
                    );
                }
            }
            AddTopGoals(goals, healGoals, 3);
        }

        private void EvaluateProtectAllyGoals(List<AIGoal> goals, CharacterBehavior behavior)
        {
            // This is a more complex one. We look at ally health and position, as well as our health,
            // and whoever attacked the ally last.  We look at this last attacker to see how dangerous
            // they are both to the ally and to ourself.

            using var protectGoalsPooled = PooledList<AIGoal>.Get();
            var protectGoals = protectGoalsPooled.List;

            using var allyLastAttackers =
                new PooledDictionary<CharacterInstance, CharacterInstance>();
            var allies = _context.Participants.Allies;
            for (int ai = 0; ai < (allies?.Count ?? 0); ai++)
            {
                var ally = allies[ai];
                // get: distance to ally, last attacker, ally health, last attacker health,
                // we also get how many squares around the ally
                // are occupied by enemies, how many are occupied by allies
                var distanceToAlly = Vector2.Distance(
                    _context.Unit.UnitInstance.MapGridPosition,
                    ally.MapGridPosition
                );
                var lastAttacker = ally.LastAttacker;
                var lastAttackerHealth = 1f;
                if (lastAttacker != null)
                {
                    lastAttackerHealth = lastAttacker.GetHealthPercentage();
                }

                var adjacency = new Adjacency(ally);
                var allySurroundingEnemies = adjacency.GetAdjacentEnemyCount(_context);
                var allySurroundingAllies = adjacency.GetAdjacentAllyCount(_context);

                // We know everything we need now.
                float utility = 5f;
                utility += 3f * allySurroundingEnemies * behavior.SelfishSelfless;
                utility += 3f * (1f - behavior.SoldierLoneWolf) * allySurroundingAllies; // Lone Wolf doesn't want a crowd
                utility += 3f * (behavior.BrashWary * (1f - lastAttackerHealth)); // Lower attacker health makes Wary happy >:)
                utility += 2f * (behavior.MindlessCunning * (3f - distanceToAlly)); // Cunning prefers enemies closer to the ally
                utility += (1f - behavior.SoldierLoneWolf) * 4F; // Soldiers are far more likely to protect allies

                utility += CalculateTerrainBonusOrPenalty(
                    ally.UnitPositionToMapGridPoint(ally.MapGridPosition, _context.MapGrid),
                    behavior
                );

                protectGoals.Add(
                    new AIGoal
                    {
                        Type = AIGoal.GoalType.ProtectAlly,
                        UtilityScore = utility,
                        Target = ally,
                        Destination = _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                            ally.UnitPositionToMapGridPoint(
                                ally.MapGridPosition,
                                _context.MapGrid
                            ).CoordinatesInt,
                            _context.MapGrid
                        ),
                    }
                );
            }
            AddTopGoals(goals, protectGoals, 3);
        }

        #endregion
    }
}
