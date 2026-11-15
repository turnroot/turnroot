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
        #region Battle Start Skill Handling
        private void HandleBattleStartSkills()
        {
            ClearSkillCaches();
            var context = BattleObject?.Context;
            if (context == null)
            {
                "BattleBrain: context not available when handling battle start skills".LogWarning();
                return;
            }

            var allUnits = GetBattleUnits(context);
            ClearUnitPassiveSkills(allUnits);
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

                    RegisterBattleStartSkill(unit, skill);
                    CollectUnitTriggerSkills(unit, skill);
                }
            }

            ExecuteBattleStartSkills(allUnits);
        }

        private void OnCharacterLearnedSkillHandler(CharacterInstance unit, Skill skill)
        {
            if (unit != null && skill?.BehaviorGraph != null)
            {
                CollectUnitTriggerSkills(unit, skill);
            }
        }

        private void OnCharacterRemovedSkillHandler(CharacterInstance unit, Skill skill)
        {
            if (
                unit?.SkillInstances?.Exists(instance => instance?.SkillTemplate == skill) == true
            )
            {
                return;
            }

            RemoveFromSkillDict(_battleStartSkills, unit, skill);
            RemoveFromSkillDict(_turnEndsSkills, unit, skill);
            RemoveFromSkillDict(_anyTurnStartsSkills, unit, skill);
            RemoveFromSkillDict(_unitTurnStartsSkills, unit, skill);
            RemoveFromSkillDict(_unitMovesSkills, unit, skill);
            RemoveFromSkillDict(_unitAttacksSkills, unit, skill);
            RemoveFromSkillDict(_enemyAttacksSkills, unit, skill);
            RemoveFromSkillDict(_enemyDefeatedSkills, unit, skill);
            RemoveFromSkillDict(_combatStartSkills, unit, skill);
            RemoveFromSkillDict(_postCombatSkills, unit, skill);
        }

        private void ClearSkillCaches()
        {
            _battleStartSkills.Clear();
            _turnEndsSkills.Clear();
            _anyTurnStartsSkills.Clear();
            _unitTurnStartsSkills.Clear();
            _unitMovesSkills.Clear();
            _unitAttacksSkills.Clear();
            _enemyAttacksSkills.Clear();
            _enemyDefeatedSkills.Clear();
            _combatStartSkills.Clear();
            _postCombatSkills.Clear();
        }

        private static List<CharacterInstance> GetBattleUnits(BattleContext context)
        {
            var units = new List<CharacterInstance>();
            if (context.Participants != null)
            {
                units.AddRange(context.Participants.GetAllUnits());
            }

            return units;
        }

        private static void ClearUnitPassiveSkills(List<CharacterInstance> units)
        {
            foreach (var unit in units)
            {
                unit?.ClearActivePassiveSkills();
            }
        }

        private void RegisterBattleStartSkill(CharacterInstance unit, Skill skill)
        {
            if (!skill.HasBattleStartNode() || !IsSkillEquipped(unit, skill))
            {
                return;
            }

            AddToSkillDict(_battleStartSkills, unit, skill);
            Brain.PublishSkillTriggered(unit, skill);
            Brain.PublishBattleStartSkill(unit, skill);

            if (SkillDebug.VerboseExecutionLogs)
            {
                $"BattleBrain: Battle-start skill '{skill.SkillName}' found on unit {unit.Id}".LogInfo();
            }
        }

        private void CollectUnitTriggerSkills(CharacterInstance unit, Skill skill)
        {
            CollectTriggerSkill(skill.HasTurnEndsNode(), _turnEndsSkills, unit, skill, "Turn-ends");
            CollectTriggerSkill(
                skill.HasAnyTurnStartsNode(),
                _anyTurnStartsSkills,
                unit,
                skill,
                "Any-turn-starts"
            );
            CollectTriggerSkill(
                skill.HasUnitTurnStartsNode(),
                _unitTurnStartsSkills,
                unit,
                skill,
                "Unit-turn-starts"
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

        private void ExecuteBattleStartSkills(List<CharacterInstance> units)
        {
            foreach (var unit in units)
            {
                if (unit == null)
                {
                    continue;
                }

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
            if (!list.Contains(skill))
            {
                list.Add(skill);
            }
        }

        private static void RemoveFromSkillDict(
            Dictionary<CharacterInstance, List<Skill>> dict,
            CharacterInstance unit,
            Skill skill
        )
        {
            if (unit == null || skill == null || !dict.TryGetValue(unit, out var skills))
            {
                return;
            }

            skills.Remove(skill);
            if (skills.Count == 0)
            {
                dict.Remove(unit);
            }
        }

        #endregion

    }
}
