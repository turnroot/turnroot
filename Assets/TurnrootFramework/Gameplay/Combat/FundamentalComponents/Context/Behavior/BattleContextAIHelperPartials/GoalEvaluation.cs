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
            _context.Brain.PublishUnitBattleEmotionChanged(
                _context.Unit.UnitInstance,
                CharacterInstance.BattleEmotion.Neutral
            );

            bool shouldProceed = true; // Controls early exit for mindless/extreme archetypes
            modifiedBehaviorSettings = BehaviorSettings.Clone();

            // Apply high-priority critical and situational modifiers
            ApplyCriticalSituationalModifiers(potentialGoals, ref shouldProceed);

            // Apply middle-priority situational modifiers (wounded, revenge, damaged, etc.)
            ApplySituationalBehaviorModifiers(ref modifiedBehaviorSettings, ref shouldProceed);

            // Evaluate treasure/greed-driven behavior
            EvaluateGreedAndTreasure(
                potentialGoals,
                ref modifiedBehaviorSettings,
                ref shouldProceed
            );

            // Standard archetype evaluation (heal, simple attack, standard attack)
            EvaluateStandardArchetypeGoals(
                potentialGoals,
                modifiedBehaviorSettings,
                ref shouldProceed
            );

            // Evaluate positioning and battle conditions
            EvaluatePositioningGoalsIfNeeded(potentialGoals, modifiedBehaviorSettings);

            // Protect allies / heal allies / explore
            EvaluateSupportAndExplorationGoals(
                potentialGoals,
                modifiedBehaviorSettings,
                ref shouldProceed
            );

            // Advanced/complex goals (defense, kill-focused)
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
                    _context.Brain.PublishUnitBattleEmotionChanged(
                        _context.Unit.UnitInstance,
                        CharacterInstance.BattleEmotion.Desperate
                    );

                    EvaluateDesperationGoals(potentialGoals, modifiedBehaviorSettings);
                    TurnrootLogger.Log(
                        $"AI added desperation goals due to being wounded and surrounded for unit {_context.Unit.UnitInstance.Id}."
                    );
                }
            }
        }

        private void ApplySituationalBehaviorModifiers(
            ref CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            // Wounded units become more cautious (only some personality types)
            if (IsWounded && BrashWary <= 0.3f) // Very brash units don't care
            {
                modifiedBehavior.BrashWary = Mathf.Min(1f, BrashWary + 0.18f);
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cautious
                );
                TurnrootLogger.Log(
                    $"AI modified behavior to be more cautious due to being wounded for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Revenge bonus: ally died last turn- become more bloodthirsty
            if (AllyDiedLastTurn && SelfishSelfless >= 0.5f) // Only selfless units care
            {
                modifiedBehavior.BloodthirstGreed = Mathf.Min(1f, BloodthirstGreed - 0.4f);
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Enraged
                );
                // if the unit was especially selfless, also become more brash
                if (SelfishSelfless >= 0.7f)
                {
                    modifiedBehavior.BrashWary = Mathf.Max(0f, BrashWary - 0.2f);
                }
                TurnrootLogger.Log(
                    $"AI modified behavior to be more bloodthirsty due to ally death for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Being attacked increases wariness (only for some personality types)
            if (WasDamagedThisTurn && MindlessCunning >= 0.5f) // Smart units adapt
            {
                modifiedBehavior.BrashWary = Mathf.Min(1f, BrashWary + 0.2f);
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cautious
                );
                TurnrootLogger.Log(
                    $"AI modified behavior to be more cautious due to being attacked for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // If a brash unit got a kill last turn, they become more bloodthirsty and slightly more careless
            if (_context.Unit.UnitInstance.LastTurnKilledEnemy && BrashWary >= 0.5f)
            {
                modifiedBehavior.BloodthirstGreed -= .2f;
                modifiedBehavior.BrashWary -= .1f;
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cocky
                );
                TurnrootLogger.Log(
                    $"AI modified behavior to be more bloodthirsty and careless due to getting a kill for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Getting treasure successfully makes a greedy unit more brash
            if (_context.Unit.UnitInstance.LastTurnCollectedTreasure && BloodthirstGreed >= 0.5f)
            {
                modifiedBehavior.BrashWary -= .2f;
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cocky
                );
                TurnrootLogger.Log(
                    $"AI modified behavior to be more careless due to getting treasure for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Being surrounded makes a lone wolf more self-centered
            if (IsSurrounded && SoldierLoneWolf >= 0.5f)
            {
                modifiedBehavior.SelfishSelfless -= .2f;
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cautious
                );
                TurnrootLogger.Log(
                    $"AI modified behavior to be more self-centered due to being surrounded for unit {_context.Unit.UnitInstance.Id}."
                );
            }
        }

        private void EvaluateGreedAndTreasure(
            List<AIGoal> potentialGoals,
            ref CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            var GreedyCunning = modifiedBehavior.BloodthirstGreed + MindlessCunning; // Use modified Greed
            if (
                (GreedyCunning >= 1.2f || modifiedBehavior.BloodthirstGreed >= .5f) && shouldProceed
            )
            {
                // High greed and cunning- focused units prioritize treasure
                EvaluateTreasureGoals(potentialGoals, modifiedBehavior);
#if UNITY_EDITOR
                TurnrootLogger.Log(
                    $"AI added treasure goals for unit {_context.Unit.UnitInstance.Id}."
                );
#endif

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
#if UNITY_EDITOR
                TurnrootLogger.Log(
                    $"AI added self-heal goals for {_context.Unit.UnitInstance.Id}."
                );
#endif
            }

            // 2. Mindless Attack
            if (CanAttack && MindlessCunning <= .4f)
            {
                // mindless enemies attack the closest enemy without thought of strategy
                EvaluateSimpleAttackGoals(potentialGoals, modifiedBehavior);
                TurnrootLogger.Log(
                    $"AI added simple attack goals for unit {_context.Unit.UnitInstance.Id}."
                );
                if (modifiedBehavior.MindlessCunning < .2f)
                {
                    shouldProceed = false;
                }
            }

            // 3. Standard Attack Goals (using modified bloodthirst)
            if (CanAttack && shouldProceed)
            {
                EvaluateAttackGoals(potentialGoals, modifiedBehavior);
                TurnrootLogger.Log(
                    $"AI added standard attack goals for unit {_context.Unit.UnitInstance.Id}."
                );
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
            foreach (BattleCondition condition in BattleConditions)
            {
                if (condition.GetType() == typeof(NoEnemiesCrossRowOrColumnBattleCondition))
                {
                    NoEnemiesCrossRowOrColumnConditions.Add(
                        (NoEnemiesCrossRowOrColumnBattleCondition)condition
                    );
                }
                else if (condition.GetType() == typeof(NoEnemyReachesTilesBattleCondition))
                {
                    NoEnemyReachesTileConditions.Add((NoEnemyReachesTilesBattleCondition)condition);
                }
            }

            var ConditionCount =
                NoEnemiesCrossRowOrColumnConditions.Count + NoEnemyReachesTileConditions.Count;
            if (ConditionCount > 0)
            {
                TurnrootLogger.Log(
                    $"AI evaluating position goals due to {ConditionCount} active battle conditions for unit {_context.Unit.UnitInstance.Id}."
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
            var CunningSelfless = MindlessCunning + SelfishSelfless;
            if ((CunningSelfless >= 1.2f || SelfishSelfless >= .5f) && shouldProceed)
            {
                EvaluateProtectAllyGoals(potentialGoals, modifiedBehavior);
#if UNITY_EDITOR
                TurnrootLogger.Log(
                    $"AI added protect ally goals for unit {_context.Unit.UnitInstance.Id}."
                );
#endif
            }

            // 5. Heal Allies
            if (CanHeal && shouldProceed)
            {
                EvaluateHealAlliesGoals(potentialGoals, modifiedBehavior);
#if UNITY_EDITOR
                TurnrootLogger.Log(
                    $"AI added heal ally goals for unit {_context.Unit.UnitInstance.Id}."
                );
#endif
                if (modifiedBehavior.MindlessCunning <= .3f)
                {
                    shouldProceed = false;
                }
            }

            // 6. Explore Villages
            if (MindlessCunning >= 0.5f && shouldProceed)
            {
                EvaluateExploreVillagesGoals(potentialGoals, modifiedBehavior);
                TurnrootLogger.Log(
                    $"AI added explore village goals for unit {_context.Unit.UnitInstance.Id}."
                );
            }
        }

        private void EvaluateAdvancedGoals(
            List<AIGoal> potentialGoals,
            CharacterBehavior modifiedBehavior,
            ref bool shouldProceed
        )
        {
            // 7. Defensive Goals (Wary Wolf) (using modified wariness)
            var WaryWolf = modifiedBehavior.BrashWary + SoldierLoneWolf;
            if ((WaryWolf >= 1.35f || modifiedBehavior.BrashWary > .5f) && shouldProceed)
            {
                // High wariness and lone wolf- focused units prioritize defense
                EvaluateDefensiveGoals(potentialGoals, modifiedBehavior);
#if UNITY_EDITOR
                TurnrootLogger.Log(
                    $"AI added defensive goals for unit {_context.Unit.UnitInstance.Id}."
                );
#endif
            }

            // 8. Kill Enemy Goals (Kill Focused) (using modified bloodthirst/wariness)
            var KillFocused =
                (1f - modifiedBehavior.BloodthirstGreed) + (1f - modifiedBehavior.BrashWary);
            if ((KillFocused >= 1.2f || modifiedBehavior.BloodthirstGreed >= .5f) && shouldProceed)
            {
                // Very kill-focused units prioritize eliminating enemies
                EvaluateKillEnemyGoals(potentialGoals, modifiedBehavior);
#if UNITY_EDITOR
                TurnrootLogger.Log(
                    $"AI added kill enemy goals for unit {_context.Unit.UnitInstance.Id}."
                );
#endif
                if (modifiedBehavior.BrashWary <= .3f)
                {
                    // Very kill-focused and not wary units only think about killing enemies
                    shouldProceed = false;
                }
            }
        }
    }
}
