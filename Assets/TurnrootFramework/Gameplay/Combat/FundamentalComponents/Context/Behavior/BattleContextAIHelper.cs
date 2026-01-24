using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Utility-based AI system for battle decision-making.
    /// Evaluates possible goals, calculates utility scores, and executes the most valuable action.
    /// </summary>
    public partial class BattleContextAIHelper
    {
        private readonly BattleContext _context;
        BattleCondition[] BattleConditions;
        private AStarModified _aStarModified = new();

        // Some basic behavioral bools
        private bool CanMove => BehaviorSettings.MovementDisabled == false;
        private bool CanAttack => BehaviorSettings.AttackDisabled == false;
        private bool CanHeal => _context.Unit.UnitInstance.CurrentClass.ClassData.Identity.CanHeal;

        // Context dependent bools
        private bool WasDamagedThisTurn;
        private bool AllyDiedLastTurn;
        private CharacterInstance LastAttackerThisTurn;

        // Computed situational states
        private bool IsWounded =>
            _context
                .Unit.UnitInstance.GetBoundedStat(Characters.Stats.BoundedStatType.Health)
                .Current < ((MindlessCunning / 2.5f) + .1f);

        private bool IsSurrounded =>
            _context.Participants.AdjacentUnits.GetAdjacentEnemyCount(_context)
            >= (SoldierLoneWolf >= .6f ? 2 : 3);

        private Vector2Int LastTurnPosition;

        private Vector2Int CurrentTurnPosition => _context.Unit.UnitInstance.MapGridPosition;
        private bool HasMovedSinceLastTurn => LastTurnPosition != CurrentTurnPosition;

        // Behavior weights
        private CharacterBehavior BehaviorSettings =>
            _context.Unit.UnitInstance.CharacterTemplate.BehaviorSettings;
        private float BloodthirstGreed => BehaviorSettings.BloodthirstGreed;
        private float BrashWary => BehaviorSettings.BrashWary;
        private float MindlessCunning => BehaviorSettings.MindlessCunning;
        private float SelfishSelfless => BehaviorSettings.SelfishSelfless;
        private float SoldierLoneWolf =>
            BehaviorSettings.SoldierLoneWolf < .75f
                ? BehaviorSettings.SoldierLoneWolf / 3f
                : BehaviorSettings.SoldierLoneWolf;

        private CharacterBehavior modifiedBehaviorSettings;

        // Reusable dictionaries to avoid allocations during AI decision-making
        private readonly Dictionary<MapGridPoint, float> _reusableMoveTiles = new();
        private readonly Dictionary<MapGridPoint, float> _reusableAttackTiles = new();
        private readonly Dictionary<MapGridPoint, float> _reusableHealTiles = new();

        public BattleContextAIHelper(BattleContext context)
        {
            _context = context;
            // Subscribe to unit turn ended so we can capture last-turn position and reset per-turn flags
            if (_context?.Brain != null)
            {
                _context.Brain.OnUnitTurnEnded += HandleUnitTurnEnded;
            }
        }

        private void HandleUnitTurnEnded(CharacterInstance unit)
        {
            // If the unit ending its turn is the one this helper is currently tracking, record position
            if (unit != null && unit == _context.Unit.UnitInstance)
            {
                LastTurnPosition = CurrentTurnPosition;

                // Reset per-turn transient flags
                WasDamagedThisTurn = false;
                AllyDiedLastTurn = false;
                LastAttackerThisTurn = null;
            }
        }

        public OperationResult InitializeAIControlledUnit(CharacterInstance unitInstance)
        {
            if (unitInstance == null)
            {
                return OperationResult.Failure("Cannot initialize AI for null unit");
            }

            _aStarModified ??= new AStarModified();

            if (_context.Brain?.battleBrain?.BattleObject != null)
            {
                BattleConditions = _context.Brain.battleBrain.BattleObject.BattleConditions;
            }
            return OperationResult.Successful();
        }

        #region AI Goal System

        /// <summary>
        /// Represents a potential action the AI can take with its evaluated utility score
        /// </summary>
        public struct AIGoal
        {
            public enum GoalType
            {
                AttackEnemy,
                KillEnemy,
                HealAlly,
                ProtectAlly,
                CollectTreasure,
                ExploreVillages,
                DefensiveRetreat,
                HoldPosition,
                GainPosition,
                HealSelf,
            }

            public GoalType Type;
            public float UtilityScore;
            public CharacterInstance Target; // Enemy or ally, if applicable
            public MapGridPoint Destination; // Store any extra info needed for execution
            public Objects.ObjectItemInstance ChosenWeapon; // Optional chosen weapon for attack goals

            public enum Action
            {
                Attack,
                Heal,
                Move,
                Feature,
            }

            public Action ActionToTake;
        }

        private void ChooseEvaluations(List<AIGoal> potentialGoals)
        {
            _context.Brain.PublishUnitBattleEmotionChanged(
                _context.Unit.UnitInstance,
                CharacterInstance.BattleEmotion.Neutral
            );

            bool shouldProceed = true; // Controls early exit for mindless/extreme archetypes
            modifiedBehaviorSettings = BehaviorSettings.Clone();

            // --- CRITICAL/SITUATIONAL EVALUATIONS (Highest Priority) ---
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
                    Debug.Log(
                        $"AI added desperation goals due to being wounded and surrounded for unit {_context.Unit.UnitInstance.Id}."
                    );
                }
            }

            // --- APPLY SITUATIONAL BEHAVIOR MODIFIERS ---

            // Wounded units become more cautious (only some personality types)
            if (IsWounded && BrashWary <= 0.3f) // Very brash units don't care
            {
                modifiedBehaviorSettings.BrashWary = Mathf.Min(1f, BrashWary + 0.18f);
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cautious
                );
                Debug.Log(
                    $"AI modified behavior to be more cautious due to being wounded for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Revenge bonus: ally died last turn- become more bloodthirsty
            if (AllyDiedLastTurn && SelfishSelfless >= 0.5f) // Only selfless units care
            {
                modifiedBehaviorSettings.BloodthirstGreed = Mathf.Min(1f, BloodthirstGreed - 0.4f);
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Enraged
                );
                // if the unit was especially selfless, also become more brash
                if (SelfishSelfless >= 0.7f)
                {
                    modifiedBehaviorSettings.BrashWary = Mathf.Max(0f, BrashWary - 0.2f);
                }
                Debug.Log(
                    $"AI modified behavior to be more bloodthirsty due to ally death for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Being attacked increases wariness (only for some personality types)
            if (WasDamagedThisTurn && MindlessCunning >= 0.5f) // Smart units adapt
            {
                modifiedBehaviorSettings.BrashWary = Mathf.Min(1f, BrashWary + 0.2f);
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cautious
                );
                Debug.Log(
                    $"AI modified behavior to be more cautious due to being attacked for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // If a brash unit got a kill last turn, they become more bloodthirsty and slightly more careless
            if (_context.Unit.UnitInstance.LastTurnKilledEnemy && BrashWary >= 0.5f)
            {
                modifiedBehaviorSettings.BloodthirstGreed -= .2f;
                modifiedBehaviorSettings.BrashWary -= .1f;
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cocky
                );
                Debug.Log(
                    $"AI modified behavior to be more bloodthirsty and careless due to getting a kill for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Getting treasure successfully makes a greedy unit more brash
            if (_context.Unit.UnitInstance.LastTurnCollectedTreasure && BloodthirstGreed >= 0.5f)
            {
                modifiedBehaviorSettings.BrashWary -= .2f;
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cocky
                );
                Debug.Log(
                    $"AI modified behavior to be more careless due to getting treasure for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // Being surrounded makes a lone wolf more self-centered
            if (IsSurrounded && SoldierLoneWolf >= 0.5f)
            {
                modifiedBehaviorSettings.SelfishSelfless -= .2f;
                _context.Brain.PublishUnitBattleEmotionChanged(
                    _context.Unit.UnitInstance,
                    CharacterInstance.BattleEmotion.Cautious
                );
                Debug.Log(
                    $"AI modified behavior to be more self-centered due to being surrounded for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            var GreedyCunning = modifiedBehaviorSettings.BloodthirstGreed + MindlessCunning; // Use modified Greed
            if (
                (GreedyCunning >= 1.2f || modifiedBehaviorSettings.BloodthirstGreed >= .5f)
                && shouldProceed
            )
            {
                // High greed and cunning- focused units prioritize treasure
                EvaluateTreasureGoals(potentialGoals, modifiedBehaviorSettings);
#if UNITY_EDITOR
                Debug.Log($"AI added treasure goals for unit {_context.Unit.UnitInstance.Id}.");
#endif

                if (modifiedBehaviorSettings.BloodthirstGreed >= .7f)
                {
                    // Very greedy and dumb units only think about treasure
                    if (MindlessCunning <= .2f || modifiedBehaviorSettings.BloodthirstGreed >= .9f)
                    {
                        shouldProceed = false;
                    }
                }
            }

            // --- STANDARD ARCHETYPE EVALUATION (Using Situational Traits) ---
            // 1. Heal Self (using situational selfishness/selflessness if traits were affected)
            if (
                (
                    modifiedBehaviorSettings.SelfishSelfless <= 0.5f
                    || (
                        MindlessCunning >= 0.5
                        && _context.Unit.UnitInstance.GetHealthPercentage() <= 0.3
                    )
                )
                && shouldProceed
                && CanHeal
            )
            {
                EvaluateHealSelfGoals(potentialGoals, modifiedBehaviorSettings);
#if UNITY_EDITOR
                Debug.Log($"AI added self-heal goals for {_context.Unit.UnitInstance.Id}.");
#endif
            }

            // 2. Mindless Attack
            if (CanAttack && MindlessCunning <= .4f)
            {
                // mindless enemies attack the closest enemy without thought of strategy
                EvaluateSimpleAttackGoals(potentialGoals, modifiedBehaviorSettings);
                Debug.Log(
                    $"AI added simple attack goals for unit {_context.Unit.UnitInstance.Id}."
                );
                if (modifiedBehaviorSettings.MindlessCunning < .2f)
                {
                    shouldProceed = false;
                }
            }

            // 3. Standard Attack Goals (using modified bloodthirst)
            if (CanAttack && shouldProceed)
            {
                EvaluateAttackGoals(potentialGoals, modifiedBehaviorSettings);
                Debug.Log(
                    $"AI added standard attack goals for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // If there are positioning goals, prioritize them

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
                Debug.Log(
                    $"AI evaluating position goals due to {ConditionCount} active battle conditions for unit {_context.Unit.UnitInstance.Id}."
                );
                EvaluatePositionGoals(
                    potentialGoals,
                    modifiedBehaviorSettings,
                    NoEnemiesCrossRowOrColumnConditions,
                    NoEnemyReachesTileConditions
                );
            }

            // 4. Protect Ally Goals (Cunning Selfless)
            var CunningSelfless = MindlessCunning + SelfishSelfless;
            if ((CunningSelfless >= 1.2f || SelfishSelfless >= .5f) && shouldProceed)
            {
                EvaluateProtectAllyGoals(potentialGoals, modifiedBehaviorSettings);
#if UNITY_EDITOR
                Debug.Log($"AI added protect ally goals for unit {_context.Unit.UnitInstance.Id}.");
#endif
            }

            // 5. Heal Allies
            if (CanHeal && shouldProceed)
            {
                EvaluateHealAlliesGoals(potentialGoals, modifiedBehaviorSettings);
#if UNITY_EDITOR
                Debug.Log($"AI added heal ally goals for unit {_context.Unit.UnitInstance.Id}.");
#endif
                if (modifiedBehaviorSettings.MindlessCunning <= .3f)
                {
                    shouldProceed = false;
                }
            }

            // 6. Explore Villages
            if (MindlessCunning >= 0.5f && shouldProceed)
            {
                EvaluateExploreVillagesGoals(potentialGoals, modifiedBehaviorSettings);
                Debug.Log(
                    $"AI added explore village goals for unit {_context.Unit.UnitInstance.Id}."
                );
            }

            // --- MORE COMPLEX GOALS (Using Situational Traits) ---

            // 7. Defensive Goals (Wary Wolf) (using modified wariness)
            var WaryWolf = modifiedBehaviorSettings.BrashWary + SoldierLoneWolf;
            if ((WaryWolf >= 1.35f || modifiedBehaviorSettings.BrashWary > .5f) && shouldProceed)
            {
                // High wariness and lone wolf- focused units prioritize defense
                EvaluateDefensiveGoals(potentialGoals, modifiedBehaviorSettings);
#if UNITY_EDITOR
                Debug.Log($"AI added defensive goals for unit {_context.Unit.UnitInstance.Id}.");
#endif
            }

            // 8. Kill Enemy Goals (Kill Focused) (using modified bloodthirst/wariness)
            var KillFocused =
                (1f - modifiedBehaviorSettings.BloodthirstGreed)
                + (1f - modifiedBehaviorSettings.BrashWary);
            if (
                (KillFocused >= 1.2f || modifiedBehaviorSettings.BloodthirstGreed >= .5f)
                && shouldProceed
            )
            {
                // Very kill-focused units prioritize eliminating enemies
                EvaluateKillEnemyGoals(potentialGoals, modifiedBehaviorSettings);
#if UNITY_EDITOR
                Debug.Log($"AI added kill enemy goals for unit {_context.Unit.UnitInstance.Id}.");
#endif
                if (modifiedBehaviorSettings.BrashWary <= .3f)
                {
                    // Very kill-focused and not wary units only think about killing enemies
                    shouldProceed = false;
                }
            }
        }

        /// <summary>
        /// Main AI decision-making method. Evaluates all possible goals and executes the best one.
        /// </summary>
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
                if (modifiedBehaviorSettings.SoldierLoneWolf < 0.5f)
                {
                    int nearbyAllies = _context.Participants.AdjacentUnits.GetAdjacentAllyCount(
                        _context
                    );
                    float formationBonus =
                        (nearbyAllies * (2f - modifiedBehaviorSettings.SoldierLoneWolf))
                        + 1f
                        + modifiedBehaviorSettings.MindlessCunning;

                    for (int i = 0; i < potentialGoals.Count; i++)
                    {
                        var goal = potentialGoals[i];

                        // For position-gaining goals, only apply formation bonus if the move does not
                        // increase the unit's distance to the nearest ally (i.e., not moving away from team)
                        if (goal.Type == AIGoal.GoalType.GainPosition && goal.Destination != null)
                        {
                            // Compute closest ally distance from current position (path-cost aware)
                            float currentClosestAllyDist = float.MaxValue;
                            var currentStart =
                                _context.Unit.UnitInstance.UnitPositionToMapGridPoint(
                                    _context.Unit.UnitInstance.MapGridPosition,
                                    _context.mapGrid
                                );
                            if (
                                PathfinderHelpers.TryFindClosestAllyPathCost(
                                    _context.mapGrid,
                                    _context.Unit.UnitInstance,
                                    currentStart,
                                    _context.Participants.Allies,
                                    out float currentCost
                                )
                            )
                            {
                                currentClosestAllyDist = currentCost;
                            }

                            // Compute closest ally distance from candidate destination (path-cost aware)
                            float destClosestAllyDist = float.MaxValue;
                            var destStart = goal.Destination;
                            if (
                                PathfinderHelpers.TryFindClosestAllyPathCost(
                                    _context.mapGrid,
                                    _context.Unit.UnitInstance,
                                    destStart,
                                    _context.Participants.Allies,
                                    out float destCost
                                )
                            )
                            {
                                destClosestAllyDist = destCost;
                            }

                            // If moving increases distance to the nearest ally, skip formation bonus
                            if (destClosestAllyDist > currentClosestAllyDist + 0.01f)
                            {
                                continue;
                            }
                        }

                        goal.UtilityScore += formationBonus;
                        potentialGoals[i] = goal;
                    }
                }

#if UNITY_EDITOR
                Debug.Log("AI Potential Goals after formation bonus:");
#endif
                foreach (var goal in potentialGoals)
                {
                    Debug.Log(
                        $"Goal: {goal.Type}, Utility: {goal.UtilityScore}, Action: {goal.ActionToTake}, Tile: {goal.Destination?.CoordinatesInt}, Target: {goal.Target?.Id}"
                    );
                }

                // 6. Choose and execute the best goal- choose (weighted) from top 3 randomly

                AIGoal chosenGoal;
                float roll = Random.Range(0f, 1f);
                // Weight based on how many goals we have
                chosenGoal =
                    potentialGoals.Count == 1 ? potentialGoals[0]
                    : potentialGoals.Count == 2
                        ? roll <= 0.9f ? potentialGoals[0]
                            : potentialGoals[1]
                    : roll <= 0.85f ? potentialGoals[0]
                    : roll <= 0.95f ? potentialGoals[1]
                    : potentialGoals[2];

                Debug.Log(
                    $"AI Chose Goal: {chosenGoal.Type} with Utility: {chosenGoal.UtilityScore}, Action: {chosenGoal.ActionToTake}, Tile: {chosenGoal.Destination?.CoordinatesInt}, Target: {chosenGoal.Target?.Id}"
                );
                ExecuteGoal(chosenGoal, _context);
            }
            else
            {
                _context.EndTurn();
            }
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
                            _context.mapGrid
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
                            _context.mapGrid
                        ),
                        _reusableMoveTiles,
                        _reusableAttackTiles,
                        _reusableHealTiles
                    );
                }
            }
        }
        #endregion
    }
}
