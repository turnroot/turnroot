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
                Brain.OnBattleStarted += HandleBattleStartSkills;
                Brain.OnPrecomputeCompleted += EvaluateBattleStartSkills;
                Brain.OnTurnBegin += EvaluateBattleStartSkills;
                Brain.OnPlayerTurnStarted += OnPlayerTurnStartedHandler;
                Brain.OnEnemyTurnStarted += EvaluateBattleStartSkills;
                Brain.OnThirdPartyTurnStarted += EvaluateBattleStartSkills;
                Brain.OnUnitTurnEnded += OnUnitTurnEndedHandler;
                Brain.OnUnitMoved += OnUnitMovedHandler;
                Brain.OnPlayerTurnStateChanged += OnPlayerTurnStateChangedHandler;
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

            var allUnits = new List<CharacterInstance>();
            var ctx = BattleObject?.Context;
            if (ctx?.Participants != null)
            {
                allUnits.AddRange(ctx.Participants.GetAllUnits());
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
                        if (!_battleStartSkills.TryGetValue(unit, out var list))
                        {
                            list = new List<Skill>();
                            _battleStartSkills[unit] = list;
                        }
                        list.Add(skill);

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

                if (!_battleStartSkills.TryGetValue(unit, out var skills))
                {
                    continue;
                }

                context.Unit.UnitInstance = unit;

                foreach (var skill in skills)
                {
                    skill.ExecuteSkill(context);

                    unit.AddActivePassiveSkill(skill);
                    $"BattleStartSkillExecutor: unit {unit.Id} added active passive skill '{skill.SkillName}'".LogInfo();
                }
            }
        }

        #endregion
    }
}
