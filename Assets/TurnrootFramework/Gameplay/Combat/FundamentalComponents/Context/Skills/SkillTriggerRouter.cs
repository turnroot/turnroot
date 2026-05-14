using System.Collections.Generic;
using Turnroot.Characters;
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

        // CombatStarts — fired before the first strike of a combat exchange, for both attacker and defender
        private readonly Dictionary<CharacterInstance, List<Skill>> _combatStartSkills = new();

        // PostCombat — fired after all strikes in a combat exchange resolve, for both attacker and defender
        private readonly Dictionary<CharacterInstance, List<Skill>> _postCombatSkills = new();

        #region Event Subscriptions
        public void SubscribeToEvents()
        {
            if (Brain == null)
            {
                return;
            }

            Brain.OnBattleStarted += HandleBattleStartSkills;
            Brain.OnUnitTurnEnded += OnUnitTurnEndedHandler;
            Brain.OnUnitTurnStarted += OnUnitTurnStartedHandler;
            Brain.OnUnitMoved += OnUnitMovedHandler;
            Brain.OnAttackLogicCompleted += OnUnitAttacksHandler;
            Brain.OnLastAttackerSet += OnLastAttackerSetHandler;
            Brain.OnCombatStarted += OnCombatStartedHandler;
            Brain.OnCombatEnded += OnCombatEndedHandler;
            Brain.Subscribe<UnitDefeatedEvent>(OnUnitDefeatedHandler, EventPriority.Normal);
        }

        public void UnsubscribeFromEvents()
        {
            if (Brain == null)
            {
                return;
            }

            Brain.OnBattleStarted -= HandleBattleStartSkills;
            Brain.OnUnitTurnEnded -= OnUnitTurnEndedHandler;
            Brain.OnUnitTurnStarted -= OnUnitTurnStartedHandler;
            Brain.OnUnitMoved -= OnUnitMovedHandler;
            Brain.OnAttackLogicCompleted -= OnUnitAttacksHandler;
            Brain.OnLastAttackerSet -= OnLastAttackerSetHandler;
            Brain.OnCombatStarted -= OnCombatStartedHandler;
            Brain.OnCombatEnded -= OnCombatEndedHandler;
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
            _combatStartSkills.Clear();
            _postCombatSkills.Clear();

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
                    CollectTriggerSkill(
                        skill.HasCombatStartsNode(),
                        _combatStartSkills,
                        unit,
                        skill,
                        "Combat-starts"
                    );
                    CollectTriggerSkill(
                        skill.HasPostCombatNode(),
                        _postCombatSkills,
                        unit,
                        skill,
                        "Post-combat"
                    );
                }
            }

            // Execute BattleStart skills exactly once now that all units are collected
            foreach (var unit in allUnits)
            {
                if (unit == null)
                {
                    continue;
                }
                context.Unit.UnitInstance = unit;
                ExecuteTriggerSkills(unit, _battleStartSkills, "BattleStarts");
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

        private void OnUnitTurnEndedHandler(CharacterInstance unit) =>
            ExecuteTriggerSkills(unit, _turnEndsSkills, "TurnEnds");

        private void OnUnitMovedHandler(CharacterInstance unit, Vector2Int pos) =>
            ExecuteTriggerSkills(unit, _unitMovesSkills, "UnitMoves");

        /// <summary>
        /// Fires when any unit completes an attack. Executes that unit's UnitAttacksNode skills.
        /// At this point context.Participants.Targets already contains the combat target(s).
        /// </summary>
        private void OnUnitAttacksHandler(CharacterInstance attacker)
        {
            var context = BattleObject?.Context;
            if (context != null)
            {
                // Set IsInitiatingCombat so skill nodes can check whether this unit started combat
                string initiatorId = context.GetCustomData("CombatInitiatorId", string.Empty);
                context.SetCustomData("IsInitiatingCombat", attacker.Id == initiatorId);
            }
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

        /// <summary>
        /// Fires before the first strike of a combat exchange. Clears combat bonuses first,
        /// then fires CombatStartsNode skills for both the attacker and defender.
        /// </summary>
        private void OnCombatStartedHandler(CharacterInstance attacker, CharacterInstance defender)
        {
            var context = BattleObject?.Context;
            if (context == null)
            {
                return;
            }

            // Reset combat-scoped bonuses and stale damage-reduction data for both participants.
            // CombatStartsNode skills will write fresh values immediately below.
            attacker?.ClearCombatBonuses();
            defender?.ClearCombatBonuses();
            if (attacker != null)
                context.CustomData.Remove($"DamageReduction_{attacker.Id}");
            if (defender != null)
                context.CustomData.Remove($"DamageReduction_{defender.Id}");

            // Fire CombatStarts skills for attacker (target = defender, is initiating = true)
            if (attacker != null)
            {
                context.SetCustomData("IsInitiatingCombat", true);
                var originalTargets = context.Participants.Targets;
                context.Participants.Targets =
                    new System.Collections.Generic.List<CharacterInstance> { defender };
                ExecuteTriggerSkills(attacker, _combatStartSkills, "CombatStarts");
                context.Participants.Targets = originalTargets;
            }

            // Fire CombatStarts skills for defender (target = attacker, is initiating = false)
            if (defender != null)
            {
                context.SetCustomData("IsInitiatingCombat", false);
                var originalTargets = context.Participants.Targets;
                context.Participants.Targets =
                    new System.Collections.Generic.List<CharacterInstance> { attacker };
                ExecuteTriggerSkills(defender, _combatStartSkills, "CombatStarts");
                context.Participants.Targets = originalTargets;
            }
        }

        /// <summary>
        /// Fires after all strikes in a combat exchange resolve. Fires PostCombatNode
        /// skills for both participants, then clears combat-scoped bonuses.
        /// </summary>
        private void OnCombatEndedHandler(CharacterInstance attacker, CharacterInstance defender)
        {
            var context = BattleObject?.Context;
            if (context == null)
            {
                return;
            }

            // Fire PostCombat skills for attacker (target = defender)
            if (attacker != null)
            {
                var originalTargets = context.Participants.Targets;
                context.Participants.Targets =
                    new System.Collections.Generic.List<CharacterInstance> { defender };
                ExecuteTriggerSkills(attacker, _postCombatSkills, "PostCombat");
                context.Participants.Targets = originalTargets;
            }

            // Fire PostCombat skills for defender (target = attacker)
            if (defender != null)
            {
                var originalTargets = context.Participants.Targets;
                context.Participants.Targets =
                    new System.Collections.Generic.List<CharacterInstance> { attacker };
                ExecuteTriggerSkills(defender, _postCombatSkills, "PostCombat");
                context.Participants.Targets = originalTargets;
            }

            // Clear combat-scoped bonuses after post-combat skills have fired
            attacker?.ClearCombatBonuses();
            defender?.ClearCombatBonuses();
        }

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
