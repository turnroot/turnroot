using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Partial class containing AI goal evaluation and situational behavior modification logic for battle context.
    /// </summary>
    public partial class BattleContextAIHelper
    {
        private void ChooseEvaluations(List<AIGoal> potentialGoals)
        {
            PublishUnitEmotion(CharacterInstance.BattleEmotion.Neutral);

            bool shouldProceed = true;
            modifiedBehaviorSettings = BehaviorSettings.Clone();

            ApplyCriticalSituationalModifiers(potentialGoals, ref shouldProceed);

            ApplySituationalBehaviorModifiers(ref modifiedBehaviorSettings, ref shouldProceed);

            EvaluateGreedAndTreasure(
                potentialGoals,
                ref modifiedBehaviorSettings,
                ref shouldProceed
            );

            EvaluateStandardArchetypeGoals(
                potentialGoals,
                modifiedBehaviorSettings,
                ref shouldProceed
            );

            EvaluatePositioningGoalsIfNeeded(potentialGoals, modifiedBehaviorSettings);

            // Protect allies / heal allies / explore
            EvaluateSupportAndExplorationGoals(
                potentialGoals,
                modifiedBehaviorSettings,
                ref shouldProceed
            );

            EvaluateAdvancedGoals(potentialGoals, modifiedBehaviorSettings, ref shouldProceed);
        }

        private void ApplyCriticalSituationalModifiers(
            List<AIGoal> potentialGoals,
            ref bool shouldProceed
        )
        {
            if (IsWounded)
            {
                modifiedBehaviorSettings.SelfishSelfless -= 0.2f;
                if (IsSurrounded)
                {
                    PublishUnitEmotion(CharacterInstance.BattleEmotion.Desperate);

                    EvaluateDesperationGoals(potentialGoals, modifiedBehaviorSettings);
                    LogUnitInfo("AI added desperation goals due to being wounded and surrounded");
                }
            }
        }

        private void LogUnitInfo(string message) =>
            $"{message} for unit {_context.Unit.UnitInstance.Id}.".LogInfo();

        private void PublishUnitEmotion(CharacterInstance.BattleEmotion emotion) =>
            _context.Brain.PublishUnitBattleEmotionChanged(_context.Unit.UnitInstance, emotion);

        private void ApplySituationalBehaviorModifiers(
            ref CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            // Wounded units become more cautious (only some personality types)
            if (IsWounded && BrashWary <= 0.3f) // Very brash units don't care
            {
                modifiedBehavior.BrashWary = Mathf.Min(1f, BrashWary + 0.18f);
                PublishUnitEmotion(CharacterInstance.BattleEmotion.Cautious);
            }

            // Revenge bonus: ally died last turn- become more bloodthirsty
            if (AllyDiedLastTurn && SelfishSelfless >= 0.5f) // Only selfless units care
            {
                modifiedBehavior.BloodthirstGreed = Mathf.Min(1f, BloodthirstGreed - 0.4f);
                PublishUnitEmotion(CharacterInstance.BattleEmotion.Enraged);
                // if the unit was especially selfless, also become more brash
                if (SelfishSelfless >= 0.7f)
                {
                    modifiedBehavior.BrashWary = Mathf.Max(0f, BrashWary - 0.2f);
                }
            }

            // Being attacked increases wariness (only for some personality types)
            if (WasDamagedThisTurn && MindlessCunning >= 0.5f) // Smart units adapt
            {
                modifiedBehavior.BrashWary = Mathf.Min(1f, BrashWary + 0.2f);
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cautious
                );
            }

            // Retaliation drive: brash or bloodthirsty units become more aggressive toward their attacker
            if (LastAttackerThisTurn != null && (BrashWary <= 0.4f || BloodthirstGreed <= 0.4f))
            {
                modifiedBehavior.BloodthirstGreed = Mathf.Max(0f, BloodthirstGreed - 0.25f);
                PublishUnitEmotion(CharacterInstance.BattleEmotion.Enraged);
            }

            // If a brash unit got a kill last turn, they become more bloodthirsty and slightly more careless
            if (_context.Unit.UnitInstance.LastTurnKilledEnemy && BrashWary >= 0.5f)
            {
                modifiedBehavior.BloodthirstGreed -= .2f;
                modifiedBehavior.BrashWary -= .1f;
                PublishUnitEmotion(CharacterInstance.BattleEmotion.Cocky);
            }

            // Getting treasure successfully makes a greedy unit more brash
            if (_context.Unit.UnitInstance.LastTurnCollectedTreasure && BloodthirstGreed >= 0.5f)
            {
                modifiedBehavior.BrashWary -= .2f;
                PublishUnitEmotion(CharacterInstance.BattleEmotion.Cocky);
            }

            // Being surrounded makes a lone wolf more self-centered
            if (IsSurrounded && SoldierLoneWolf >= 0.5f)
            {
                modifiedBehavior.SelfishSelfless -= .2f;
                PublishUnitEmotion(CharacterInstance.BattleEmotion.Cautious);
            }
        }

        private void EvaluateGreedAndTreasure(
            List<AIGoal> potentialGoals,
            ref CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            if (
                (
                    modifiedBehavior.BloodthirstGreed + MindlessCunning >= 1.2f
                    || modifiedBehavior.BloodthirstGreed >= .5f
                ) && shouldProceed
            )
            {
                // High greed and cunning- focused units prioritize treasure
                EvaluateTreasureGoals(potentialGoals, modifiedBehavior);

                if (modifiedBehavior.BloodthirstGreed >= .7f)
                {
                    // Very greedy and dumb units only think about treasure
                    if (MindlessCunning <= .2f || modifiedBehavior.BloodthirstGreed >= .9f)
                    {
                        shouldProceed = false;
                    }
                }
            }
        }

        private void EvaluateStandardArchetypeGoals(
            List<AIGoal> potentialGoals,
            CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            // 1. Heal Self (using situational selfishness/selflessness if traits were affected)
            if (
                (
                    modifiedBehavior.SelfishSelfless <= 0.5f
                    || (
                        MindlessCunning >= 0.5
                        && _context.Unit.UnitInstance.GetHealthPercentage() <= 0.3
                    )
                )
                && shouldProceed
                && CanHeal
            )
            {
                EvaluateHealSelfGoals(potentialGoals, modifiedBehavior);
            }

            // 2. Mindless Attack
            if (CanAttack && MindlessCunning <= .4f)
            {
                // mindless enemies attack the closest enemy without thought of strategy
                EvaluateSimpleAttackGoals(potentialGoals, modifiedBehavior);
                LogUnitInfo("AI added simple attack goals");
                if (modifiedBehavior.MindlessCunning < .2f)
                {
                    shouldProceed = false;
                }
            }

            // 3. Standard Attack Goals (using modified bloodthirst)
            if (CanAttack && shouldProceed)
            {
                EvaluateAttackGoals(potentialGoals, modifiedBehavior);
                LogUnitInfo("AI added standard attack goals");
            }
        }

        private void EvaluatePositioningGoalsIfNeeded(
            List<AIGoal> potentialGoals,
            CharacterBehavior modifiedBehavior
        )
        {
            var NoEnemiesCrossRowOrColumnConditions =
                new List<NoEnemiesCrossRowOrColumnBattleCondition>();
            var NoEnemyReachesTileConditions = new List<NoEnemyReachesTilesBattleCondition>();
            foreach (var condition in BattleConditions)
            {
                if (condition is NoEnemiesCrossRowOrColumnBattleCondition necc)
                {
                    NoEnemiesCrossRowOrColumnConditions.Add(necc);
                }
                else if (condition is NoEnemyReachesTilesBattleCondition nert)
                {
                    NoEnemyReachesTileConditions.Add(nert);
                }
            }

            var ConditionCount =
                NoEnemiesCrossRowOrColumnConditions.Count + NoEnemyReachesTileConditions.Count;
            if (ConditionCount > 0)
            {
                LogUnitInfo(
                    $"AI evaluating position goals due to {ConditionCount} active battle conditions"
                );
                EvaluatePositionGoals(
                    potentialGoals,
                    modifiedBehavior,
                    NoEnemiesCrossRowOrColumnConditions,
                    NoEnemyReachesTileConditions
                );
            }
        }

        private void EvaluateSupportAndExplorationGoals(
            List<AIGoal> potentialGoals,
            CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            // 4. Protect Ally Goals (Cunning Selfless)
            if (
                (MindlessCunning + SelfishSelfless >= 1.2f || SelfishSelfless >= .5f)
                && shouldProceed
            )
            {
                EvaluateProtectAllyGoals(potentialGoals, modifiedBehavior);
            }

            // 5. Heal Allies
            if (CanHeal && shouldProceed)
            {
                EvaluateHealAlliesGoals(potentialGoals, modifiedBehavior);

                if (modifiedBehavior.MindlessCunning <= .3f)
                {
                    shouldProceed = false;
                }
            }

            // 6. Explore Villages
            if (MindlessCunning >= 0.5f && shouldProceed)
            {
                EvaluateExploreVillagesGoals(potentialGoals, modifiedBehavior);
                LogUnitInfo("AI added explore village goals");
            }
        }

        private void EvaluateAdvancedGoals(
            List<AIGoal> potentialGoals,
            CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            // 7. Defensive Goals (Wary Wolf) (using modified wariness)
            if (
                (
                    modifiedBehavior.BrashWary + SoldierLoneWolf >= 1.35f
                    || modifiedBehavior.BrashWary > .5f
                ) && shouldProceed
            )
            {
                // High wariness and lone wolf- focused units prioritize defense
                EvaluateDefensiveGoals(potentialGoals, modifiedBehavior);
            }

            // 8. Kill Enemy Goals (Kill Focused) (using modified bloodthirst/wariness)
            if (
                (
                    (1f - modifiedBehavior.BloodthirstGreed) + (1f - modifiedBehavior.BrashWary)
                        >= 1.2f
                    || modifiedBehavior.BloodthirstGreed >= .5f
                ) && shouldProceed
            )
            {
                // Very kill-focused units prioritize eliminating enemies
                EvaluateKillEnemyGoals(potentialGoals, modifiedBehavior);

                if (modifiedBehavior.BrashWary <= .3f)
                {
                    // Very kill-focused and not wary units only think about killing enemies
                    shouldProceed = false;
                }
            }
        }
    }
}
