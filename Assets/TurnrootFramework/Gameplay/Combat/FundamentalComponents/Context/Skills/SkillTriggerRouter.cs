using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class SkillTriggerRouter
    {
        private readonly BattleBrain _battleBrain;
        private Brain Brain => _battleBrain?.Brain;
        private BattleGameObject BattleObject => _battleBrain?.BattleObject;

        public SkillTriggerRouter(BattleBrain battleBrain)
        {
            _battleBrain = battleBrain;
        }

        // BattleStartsNode — continuously re-evaluated passive auras
        private readonly Dictionary<CharacterInstance, List<Skill>> _battleStartSkills = new();

        // One-shot trigger skills, keyed by the unit that owns the skill
        // TurnEnds / TurnStarts / UnitMoves — non-combat, full enemy list in Targets
        private readonly Dictionary<CharacterInstance, List<Skill>> _turnEndsSkills = new();
        private readonly Dictionary<CharacterInstance, List<Skill>> _turnStartsSkills = new();
        private readonly Dictionary<CharacterInstance, List<Skill>> _unitMovesSkills = new();

        // UnitAttacks — combat, Targets already contains the combat target(s)
        private readonly Dictionary<CharacterInstance, List<Skill>> _unitAttacksSkills = new();

        // EnemyAttacks — fired for the DEFENDER; Targets set to [attacker] during execution
        private readonly Dictionary<CharacterInstance, List<Skill>> _enemyAttacksSkills = new();

        // EnemyDefeated — fired for the KILLER; Targets set to [defeated unit] during execution
        private readonly Dictionary<CharacterInstance, List<Skill>> _enemyDefeatedSkills = new();

        #region Event Subscriptions
        public void SubscribeToEvents()
        {
            if (Brain == null)
            {
                return;
            }

            Brain.OnBattleStarted += HandleBattleStartSkills;
            Brain.OnPrecomputeCompleted += EvaluateBattleStartSkills;
            Brain.OnTurnBegin += EvaluateBattleStartSkills;
            Brain.OnPlayerTurnStarted += OnPlayerTurnStartedHandler;
            Brain.OnEnemyTurnStarted += EvaluateBattleStartSkills;
            Brain.OnThirdPartyTurnStarted += EvaluateBattleStartSkills;
            Brain.OnUnitTurnEnded += OnUnitTurnEndedHandler;
            Brain.OnUnitTurnStarted += OnUnitTurnStartedHandler;
            Brain.OnUnitMoved += OnUnitMovedHandler;
            Brain.OnPlayerTurnStateChanged += OnPlayerTurnStateChangedHandler;
            Brain.OnCharacterClassChanged += OnCharacterClassChangedHandler;
            Brain.OnAttackLogicCompleted += OnUnitAttacksHandler;
            Brain.OnLastAttackerSet += OnLastAttackerSetHandler;
            Brain.Subscribe<UnitDefeatedEvent>(OnUnitDefeatedHandler, EventPriority.Normal);
        }

        public void UnsubscribeFromEvents()
        {
            if (Brain == null)
            {
                return;
            }

            Brain.OnBattleStarted -= HandleBattleStartSkills;
            Brain.OnPrecomputeCompleted -= EvaluateBattleStartSkills;
            Brain.OnTurnBegin -= EvaluateBattleStartSkills;
            Brain.OnPlayerTurnStarted -= OnPlayerTurnStartedHandler;
            Brain.OnEnemyTurnStarted -= EvaluateBattleStartSkills;
            Brain.OnThirdPartyTurnStarted -= EvaluateBattleStartSkills;
            Brain.OnUnitTurnEnded -= OnUnitTurnEndedHandler;
            Brain.OnUnitTurnStarted -= OnUnitTurnStartedHandler;
            Brain.OnUnitMoved -= OnUnitMovedHandler;
            Brain.OnPlayerTurnStateChanged -= OnPlayerTurnStateChangedHandler;
            Brain.OnCharacterClassChanged -= OnCharacterClassChangedHandler;
            Brain.OnAttackLogicCompleted -= OnUnitAttacksHandler;
            Brain.OnLastAttackerSet -= OnLastAttackerSetHandler;
            Brain.Unsubscribe<UnitDefeatedEvent>(OnUnitDefeatedHandler);
        }
        #endregion

        #region Battle Start Skill Handling
        private void HandleBattleStartSkills()
        {
            _battleStartSkills.Clear();
            _turnEndsSkills.Clear();
            _turnStartsSkills.Clear();
            _unitMovesSkills.Clear();
            _unitAttacksSkills.Clear();
            _enemyAttacksSkills.Clear();
            _enemyDefeatedSkills.Clear();

            var context = BattleObject?.Context;
            if (context == null)
            {
                "BattleBrain: context not available when handling battle start skills".LogWarning();
                return;
            }

            // reset any existing active-skills lists in the context
            context.Skill.ActiveSkills.Clear();
            context.Skill.ActiveSkillGraphs.Clear();

            var allUnits = new List<CharacterInstance>();
            if (context.Participants != null)
            {
                allUnits.AddRange(context.Participants.GetAllUnits());
            }
            foreach (var unit in allUnits)
            {
                unit?.ClearActivePassiveSkills();
            }

            foreach (var unit in allUnits)
            {
                if (unit == null)
                {
                    continue;
                }

                foreach (var skillInst in unit.SkillInstances)
                {
                    var skill = skillInst?.SkillTemplate;
                    if (skill == null || skill.BehaviorGraph == null)
                    {
                        continue;
                    }

                    if (skill.HasBattleStartNode())
                    {
                        AddToSkillDict(_battleStartSkills, unit, skill);

                        Brain.PublishSkillTriggered(unit, skill);
                        Brain.PublishBattleStartSkill(unit, skill);

                        context.Skill.ActiveSkills.Add(skill);
                        if (skill.BehaviorGraph != null)
                        {
                            context.Skill.ActiveSkillGraphs.Add(skill.BehaviorGraph);
                        }

                        if (SkillDebug.VerboseExecutionLogs)
                        {
                            $"BattleBrain: Battle-start skill '{skill.SkillName}' found on unit {unit.Id}".LogInfo();
                        }
                    }

                    CollectTriggerSkill(
                        skill.HasTurnEndsNode(),
                        _turnEndsSkills,
                        unit,
                        skill,
                        "Turn-ends"
                    );
                    CollectTriggerSkill(
                        skill.HasTurnStartsNode(),
                        _turnStartsSkills,
                        unit,
                        skill,
                        "Turn-starts"
                    );
                    CollectTriggerSkill(
                        skill.HasUnitMovesNode(),
                        _unitMovesSkills,
                        unit,
                        skill,
                        "Unit-moves"
                    );
                    CollectTriggerSkill(
                        skill.HasUnitAttacksNode(),
                        _unitAttacksSkills,
                        unit,
                        skill,
                        "Unit-attacks"
                    );
                    CollectTriggerSkill(
                        skill.HasEnemyAttacksNode(),
                        _enemyAttacksSkills,
                        unit,
                        skill,
                        "Enemy-attacks"
                    );
                    CollectTriggerSkill(
                        skill.HasEnemyDefeatedNode(),
                        _enemyDefeatedSkills,
                        unit,
                        skill,
                        "Enemy-defeated"
                    );
                }
            }
        }

        private void CollectTriggerSkill(
            bool hasNode,
            Dictionary<CharacterInstance, List<Skill>> dict,
            CharacterInstance unit,
            Skill skill,
            string label
        )
        {
            if (!hasNode)
            {
                return;
            }

            AddToSkillDict(dict, unit, skill);
            if (SkillDebug.VerboseExecutionLogs)
            {
                $"BattleBrain: {label} skill '{skill.SkillName}' found on unit {unit.Id}".LogInfo();
            }
        }

        private static void AddToSkillDict(
            Dictionary<CharacterInstance, List<Skill>> dict,
            CharacterInstance unit,
            Skill skill
        )
        {
            if (!dict.TryGetValue(unit, out var list))
            {
                list = new List<Skill>();
                dict[unit] = list;
            }
            list.Add(skill);
        }

        #endregion

        #region Evaluation Helpers

        // Re-evaluates passive BattleStarts auras. Does NOT fire one-shot triggers.
        private void OnPlayerTurnStartedHandler(CharacterInstance unit) =>
            EvaluateBattleStartSkills();

        private void OnCharacterClassChangedHandler(CharacterInstance character)
        {
            // only re-evaluate if this unit has battle-start skills registered
            if (character != null && _battleStartSkills.ContainsKey(character))
            {
                EvaluateBattleStartSkills();
            }
        }

        private void OnPlayerTurnStateChangedHandler(PlayerTurnStates newState)
        {
            // trigger when entering executing phases
            if (newState is PlayerTurnStates.ExecutingMove or PlayerTurnStates.ExecutingAction)
            {
                EvaluateBattleStartSkills();
            }
        }

        private void OnUnitTurnEndedHandler(CharacterInstance unit)
        {
            EvaluateBattleStartSkills();
            ExecuteTriggerSkills(unit, _turnEndsSkills, "TurnEnds");
        }

        private void OnUnitMovedHandler(CharacterInstance unit, Vector2Int pos)
        {
            EvaluateBattleStartSkills();
            ExecuteTriggerSkills(unit, _unitMovesSkills, "UnitMoves");
        }

        /// <summary>
        /// Fires when any unit completes an attack. Executes that unit's UnitAttacksNode skills.
        /// At this point context.Participants.Targets already contains the combat target(s).
        /// </summary>
        private void OnUnitAttacksHandler(CharacterInstance attacker)
        {
            ExecuteTriggerSkills(attacker, _unitAttacksSkills, "UnitAttacks");
        }

        /// <summary>
        /// Fires for every unit (player, enemy, third-party) at the start of its turn.
        /// Executes that unit's TurnStartsNode skills with the full enemy list in Targets.
        /// </summary>
        private void OnUnitTurnStartedHandler(CharacterInstance unit)
        {
            ExecuteTriggerSkills(unit, _turnStartsSkills, "TurnStarts");
        }

        /// <summary>
        /// Fires when a unit is set as the last attacker — i.e., it just attacked a target.
        /// If the attacker is an enemy and the defender is a player ally, fire EnemyAttacksNode
        /// skills for the defender with Targets temporarily set to [attacker].
        /// </summary>
        private void OnLastAttackerSetHandler(
            CharacterInstance defender,
            CharacterInstance attacker
        )
        {
            var context = BattleObject?.Context;
            if (context == null || defender == null || attacker == null)
            {
                return;
            }

            // Only fire if an enemy attacked a player unit
            if (!context.IsEnemyUnit(attacker) || !context.IsPlayerControlledUnit(defender))
            {
                return;
            }

            // Execute EnemyAttacksNode skills on the defender; temporarily set Targets = [attacker]
            var originalTargets = context.Participants.Targets;
            context.Participants.Targets = new System.Collections.Generic.List<CharacterInstance>
            {
                attacker,
            };
            ExecuteTriggerSkills(defender, _enemyAttacksSkills, "EnemyAttacks");
            context.Participants.Targets = originalTargets;
        }

        /// <summary>
        /// Fires via the typed event bus when any unit is defeated.
        /// If the killer is a player ally and the defeated unit is an enemy, fire EnemyDefeatedNode
        /// skills for the killer with Targets temporarily set to [defeated unit].
        /// </summary>
        private void OnUnitDefeatedHandler(UnitDefeatedEvent evt)
        {
            var context = BattleObject?.Context;
            if (context == null || evt.Unit == null || evt.Killer == null)
            {
                return;
            }

            // Only fire if a player unit killed an enemy
            if (!context.IsPlayerControlledUnit(evt.Killer) || !context.IsEnemyUnit(evt.Unit))
            {
                return;
            }

            var originalTargets = context.Participants.Targets;
            context.Participants.Targets = new System.Collections.Generic.List<CharacterInstance>
            {
                evt.Unit,
            };
            ExecuteTriggerSkills(evt.Killer, _enemyDefeatedSkills, "EnemyDefeated");
            context.Participants.Targets = originalTargets;
        }

        private void EvaluateBattleStartSkills() => EvaluateBattleStartSkillsExecuteGraph();

        private void EvaluateBattleStartSkillsExecuteGraph()
        {
            var context = BattleObject?.Context;
            if (context == null || context.Participants == null)
            {
                return;
            }

            foreach (var unit in context.Participants.GetAllUnits())
            {
                if (unit == null || !_battleStartSkills.TryGetValue(unit, out var skills))
                {
                    continue;
                }

                context.Unit.UnitInstance = unit;

                foreach (var skill in skills)
                {
                    skill.ExecuteSkill(context);
                    unit.AddActivePassiveSkill(skill);

                    if (SkillDebug.VerboseExecutionLogs)
                    {
                        $"SkillTriggerRouter: re-evaluated BattleStart skill '{skill.SkillName}' for unit {unit.Id}".LogInfo();
                    }
                }
            }
        }

        /// <summary>
        /// Executes all skills in <paramref name="skillDict"/> belonging to <paramref name="unit"/>.
        /// Sets <c>context.Unit.UnitInstance</c> to the unit before execution.
        /// For non-combat triggers, <c>context.Participants.Targets</c> retains the full enemy
        /// list so a ForEachEnemyNode inside the graph can iterate them individually.
        /// </summary>
        private void ExecuteTriggerSkills(
            CharacterInstance unit,
            Dictionary<CharacterInstance, List<Skill>> skillDict,
            string triggerName
        )
        {
            if (unit == null)
            {
                return;
            }

            var context = BattleObject?.Context;
            if (context == null)
            {
                return;
            }

            if (!skillDict.TryGetValue(unit, out var skills))
            {
                return;
            }

            context.Unit.UnitInstance = unit;

            foreach (var skill in skills)
            {
                if (SkillDebug.VerboseExecutionLogs)
                {
                    $"SkillTriggerRouter: executing {triggerName} skill '{skill.SkillName}' for unit {unit.Id}".LogInfo();
                }
                skill.ExecuteSkill(context);
            }
        }

        #endregion
    }
}
