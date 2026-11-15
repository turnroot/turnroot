using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class SkillTriggerRouter
    {
        #region Trigger Execution
        private sealed class TriggerExecutionScope
        {
            public BattleContext Context;
            public CharacterInstance Unit;
            public List<Skill> Skills;
            public string TriggerName;
            public List<CharacterInstance> Targets;
            public List<CharacterInstance> OriginalTargets;
            public CharacterInstance OriginalUnit;
            public Skill OriginalSkill;
            public SkillGraph OriginalSkillGraph;
            public bool OriginalInterrupted;
            public bool HadOriginalExecutor;
            public object OriginalExecutor;
            public int NextSkillIndex;
            public Skill CurrentSkill;
        }

        private void ExecuteTriggerSkillsForTarget(
            BattleContext context,
            CharacterInstance unit,
            Dictionary<CharacterInstance, List<Skill>> skillDict,
            string triggerName,
            CharacterInstance target
        )
            => ExecuteTriggerSkills(unit, skillDict, triggerName, target);

        private void ExecuteTriggerSkills(
            CharacterInstance unit,
            Dictionary<CharacterInstance, List<Skill>> skillDict,
            string triggerName
        ) => ExecuteTriggerSkills(unit, skillDict, triggerName, target: null);

        private void ExecuteTriggerSkills(
            CharacterInstance unit,
            Dictionary<CharacterInstance, List<Skill>> skillDict,
            string triggerName,
            CharacterInstance target
        )
        {
            if (
                unit == null
                || BattleObject?.Context is not BattleContext context
                || !skillDict.TryGetValue(unit, out var registeredSkills)
            )
            {
                return;
            }

            var equippedSkills = new List<Skill>();
            foreach (var skill in registeredSkills)
            {
                if (skill != null && IsSkillEquipped(unit, skill))
                {
                    equippedSkills.Add(skill);
                }
            }
            if (equippedSkills.Count == 0)
            {
                return;
            }

            _triggerExecutionScopes.Push(
                new TriggerExecutionScope
                {
                    Context = context,
                    Unit = unit,
                    Skills = equippedSkills,
                    TriggerName = triggerName,
                    OriginalTargets = context.GetCurrentTargets(),
                    Targets = target == null
                        ? context.GetCurrentTargets()
                        : new List<CharacterInstance> { target },
                    OriginalUnit = context.Unit.UnitInstance,
                    OriginalSkill = context.Skill.CurrentSkill,
                    OriginalSkillGraph = context.Skill.CurrentSkillGraph,
                    OriginalInterrupted = context.Flags.IsInterrupted,
                    HadOriginalExecutor = context.CustomData.TryGetValue(
                        "_executor",
                        out var originalExecutor
                    ),
                    OriginalExecutor = originalExecutor,
                }
            );
            ExecuteNextTriggerSkill(_triggerExecutionScopes.Peek());
        }

        private void OnSkillExecutionCompletedHandler(
            CharacterInstance unit,
            Skill skill,
            SkillGraph graph
        )
        {
            if (_triggerExecutionScopes.Count == 0)
            {
                return;
            }

            var scope = _triggerExecutionScopes.Peek();
            if (
                scope.Unit != unit
                || scope.CurrentSkill != skill
                || skill?.BehaviorGraph != graph
            )
            {
                return;
            }

            scope.NextSkillIndex++;
            ExecuteNextTriggerSkill(scope);
        }

        private void ExecuteNextTriggerSkill(TriggerExecutionScope scope)
        {
            while (scope.NextSkillIndex < scope.Skills.Count)
            {
                var skill = scope.Skills[scope.NextSkillIndex];
                if (IsSkillEquipped(scope.Unit, skill))
                {
                    scope.CurrentSkill = skill;
                    scope.Context.Unit.UnitInstance = scope.Unit;
                    scope.Context.SetCurrentTargets(scope.Targets);
                    try
                    {
                        if (SkillDebug.VerboseExecutionLogs)
                        {
                            $"SkillTriggerRouter: executing {scope.TriggerName} skill '{skill.SkillName}' for unit {scope.Unit.Id}".LogInfo();
                        }
                        skill.ExecuteSkill(scope.Context);
                    }
                    catch
                    {
                        if (
                            _triggerExecutionScopes.Count > 0
                            && ReferenceEquals(_triggerExecutionScopes.Peek(), scope)
                        )
                        {
                            _triggerExecutionScopes.Pop();
                            RestoreTriggerExecutionScope(scope);
                        }
                        throw;
                    }

                    return;
                }

                scope.NextSkillIndex++;
            }

            if (
                _triggerExecutionScopes.Count > 0
                && ReferenceEquals(_triggerExecutionScopes.Peek(), scope)
            )
            {
                _triggerExecutionScopes.Pop();
                RestoreTriggerExecutionScope(scope);
            }
        }

        private void RestoreTriggerExecutionScope(TriggerExecutionScope scope)
        {
            scope.Context.SetCurrentTargets(scope.OriginalTargets);
            scope.Context.Unit.UnitInstance = scope.OriginalUnit;
            scope.Context.Skill.CurrentSkill = scope.OriginalSkill;
            scope.Context.Skill.CurrentSkillGraph = scope.OriginalSkillGraph;
            scope.Context.Flags.IsInterrupted = scope.OriginalInterrupted;

            if (scope.HadOriginalExecutor)
            {
                scope.Context.SetCustomData("_executor", scope.OriginalExecutor);
            }
            else
            {
                scope.Context.CustomData.Remove("_executor");
            }
        }

        private static bool IsSkillEquipped(CharacterInstance unit, Skill skill) =>
            unit?.SkillInstances?.Exists(instance =>
                instance?.Equipped == true && instance.SkillTemplate == skill
            ) ?? false;

        #endregion
    }
}
