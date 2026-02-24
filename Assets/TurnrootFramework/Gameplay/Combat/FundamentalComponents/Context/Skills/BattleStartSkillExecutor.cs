using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class BattleStartSkillExecutor
    {
        private readonly BattleBrain _battleBrain;
        private Brain Brain => _battleBrain?.Brain;
        private BattleGameObject BattleObject => _battleBrain?.BattleObject;

        public BattleStartSkillExecutor(BattleBrain battleBrain)
        {
            _battleBrain = battleBrain;
        }

        private readonly Dictionary<CharacterInstance, List<Skill>> _battleStartSkills = new();

        #region Event Subscriptions
        public void SubscribeToEvents()
        {
            if (Brain != null)
            {
                // gather battle-start skills when battle starts but defer execution until precompute
                Brain.OnBattleStarted += HandleBattleStartSkills;

                // run evaluation after precompute has finished assigning classes/stats
                Brain.OnPrecomputeCompleted += EvaluateBattleStartSkills;

                // still re‑evaluate during the turn cycle (in case context changes)
                Brain.OnTurnBegin += EvaluateBattleStartSkills;
                Brain.OnPlayerTurnStarted += OnPlayerTurnStartedHandler;
                Brain.OnEnemyTurnStarted += EvaluateBattleStartSkills;
                Brain.OnThirdPartyTurnStarted += EvaluateBattleStartSkills;
                Brain.OnUnitTurnEnded += OnUnitTurnEndedHandler;
                Brain.OnUnitMoved += OnUnitMovedHandler;
                Brain.OnPlayerTurnStateChanged += OnPlayerTurnStateChangedHandler;

                // if a character changes class (or receives initial class info) retry evaluation
                Brain.OnCharacterClassChanged += OnCharacterClassChangedHandler;
            }
        }

        public void UnsubscribeFromEvents()
        {
            if (Brain != null)
            {
                Brain.OnBattleStarted -= HandleBattleStartSkills;

                Brain.OnTurnBegin -= EvaluateBattleStartSkills;
                Brain.OnPlayerTurnStarted -= OnPlayerTurnStartedHandler;
                Brain.OnEnemyTurnStarted -= EvaluateBattleStartSkills;
                Brain.OnThirdPartyTurnStarted -= EvaluateBattleStartSkills;
                Brain.OnUnitTurnEnded -= OnUnitTurnEndedHandler;
                Brain.OnUnitMoved -= OnUnitMovedHandler;
                Brain.OnPlayerTurnStateChanged -= OnPlayerTurnStateChangedHandler;

                Brain.OnCharacterClassChanged -= OnCharacterClassChangedHandler;
            }
        }
        #endregion

        #region Battle Start Skill Handling
        private void HandleBattleStartSkills()
        {
            _battleStartSkills.Clear();

            var context = BattleObject?.Context;
            if (context == null)
            {
                "BattleBrain: context not available when handling battle start skills".LogWarning();
                return;
            }

            // reset any existing active-skills lists in the context
            context.Skill.ActiveSkills.Clear();
            context.Skill.ActiveSkillGraphs.Clear();

            // gather all relevant units (all allies, enemies, third party) from the battle context
            var allUnits = new List<CharacterInstance>();
            var ctx = BattleObject?.Context;
            if (ctx?.Participants != null)
            {
                allUnits.AddRange(ctx.Participants.GetAllUnits());
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
                        if (!_battleStartSkills.TryGetValue(unit, out var list))
                        {
                            list = new List<Skill>();
                            _battleStartSkills[unit] = list;
                        }
                        list.Add(skill);

                        // publish the skill-triggered event for others to react
                        Brain.PublishSkillTriggered(unit, skill);
                        // and a dedicated "battle start" event
                        Brain.PublishBattleStartSkill(unit, skill);

                        // also populate context lists for easy access later
                        context.Skill.ActiveSkills.Add(skill);
                        if (skill.BehaviorGraph != null)
                        {
                            context.Skill.ActiveSkillGraphs.Add(skill.BehaviorGraph);
                        }

                        // simple log for now
                        if (SkillDebug.VerboseExecutionLogs)
                        {
                            $"BattleBrain: Battle-start skill '{skill.SkillName}' found on unit {unit.Id}".LogInfo();
                        }
                    }
                }
            }
        }

        #endregion

        #region Evaluation Helpers

        private void OnPlayerTurnStartedHandler(CharacterInstance ignored) =>
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

        private void OnUnitTurnEndedHandler(CharacterInstance ignored) =>
            EvaluateBattleStartSkills();

        private void OnUnitMovedHandler(CharacterInstance unit, Vector2Int pos) =>
            EvaluateBattleStartSkills();

        private void EvaluateBattleStartSkills() => EvaluateBattleStartSkillsExecuteGraph();

        private void EvaluateBattleStartSkillsExecuteGraph()
        {
            var context = BattleObject?.Context;
            if (context == null)
            {
                return;
            }

            var allUnits = new List<CharacterInstance>();
            var ctx = BattleObject?.Context;
            if (ctx?.Participants != null)
            {
                allUnits.AddRange(ctx.Participants.GetAllUnits());
            }

            foreach (var unit in allUnits)
            {
                if (unit == null)
                {
                    continue;
                }

                // skip units that do not yet have class data; we will re-run when they change class
                if (unit.CurrentClass == null || unit.CurrentClass.ClassData == null)
                {
                    if (SkillDebug.VerboseExecutionLogs)
                    {
                        $"BattleStartSkillExecutor: skipping evaluation for unit {unit.Id} because class data not assigned".LogInfo();
                    }
                    continue;
                }

                if (!_battleStartSkills.TryGetValue(unit, out var skills))
                {
                    continue;
                }

                context.Unit.UnitInstance = unit;

                // debugging: report map grid and unit location
#if UNITY_EDITOR
                if (SkillDebug.VerboseExecutionLogs)
                {
                    if (context.MapGrid == null)
                    {
                        $"BattleStartSkillExecutor: MapGrid is null when executing skill for unit {unit.Id}".LogInfo();
                    }
                    else
                    {
                        $"BattleStartSkillExecutor: unit {unit.Id} position {unit.MapGridPosition} (terrain logged by node)".LogInfo();
                    }
                }
#endif

                foreach (var skill in skills)
                {
                    skill.ExecuteSkill(context);
                }
            }
        }

        #endregion

        #region Query API
        /// <summary>
        /// Returns the list of skills that were activated for the given character during the most
        /// recent battle-start processing.  An empty list is returned if none were found.
        /// </summary>
        public IReadOnlyList<Skill> GetBattleStartSkills(CharacterInstance unit)
        {
            return unit == null ? new List<Skill>()
                : _battleStartSkills.TryGetValue(unit, out var list) ? list.AsReadOnly()
                : new List<Skill>();
        }

        /// <summary>
        /// Returns true if the given unit had at least one skill trigger at battle start.
        /// </summary>
        public bool HasBattleStartSkill(CharacterInstance unit)
        {
            return unit != null
                && _battleStartSkills.TryGetValue(unit, out var list)
                && list.Count > 0;
        }
        #endregion
    }
}
