using System;
using System.Linq;
using Newtonsoft.Json;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Serialization;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills
{
    /// <summary>
    /// Runtime instance of a skill with state tracking (ready to fire, equipped) and execution context.
    /// </summary>
    [Serializable]
    public class SkillInstance : IPostDeserialize
    {
        [SerializeField, JsonProperty("_skillTemplate")]
        private Skill _skillTemplate;

        // Runtime state - unique per character/entity
        [SerializeField]
        private bool _readyToFire;

        [SerializeField, JsonProperty("_equipped")]
        private bool _equipped;

        public Skill SkillTemplate => _skillTemplate;
        public bool ReadyToFire => _readyToFire;
        public bool Equipped => _equipped;

        public SkillInstance(Skill skillTemplate)
        {
            _skillTemplate = skillTemplate;
            _readyToFire = false;
            _equipped = false;
        }

        // Parameterless constructor for deserialization
        public SkillInstance() { }

        public void OnAfterDeserialize()
        {
            // No special initialization required currently. Provided for future-proofing.
        }

        /// <summary>
        /// Execute this skill instance with the given battle context.
        /// Records the skill activation as a command for replay support.
        /// Individual effects (damage, movement, etc.) are captured by their respective commands.
        /// </summary>
        public void ExecuteSkill(BattleContext context)
        {
            // guard clauses use the shared validation helper so all null logging goes through
            if (!ValidationHelper.ValidateNotNull(_skillTemplate, nameof(_skillTemplate)))
            {
                return;
            }

            if (
                !ValidationHelper.ValidateNotNull(
                    _skillTemplate.BehaviorGraph,
                    "BehaviorGraph",
                    $"Skill {_skillTemplate.SkillName}"
                )
            )
            {
                return;
            }

            // argument validation; helps callers catch mistakes earlier
            ValidationHelper.ThrowIfNull(context.Brain, nameof(context.Brain));

            // Set runtime context
            context.Skill.CurrentSkill = _skillTemplate;
            context.Skill.CurrentSkillGraph = _skillTemplate.BehaviorGraph;

            // Record skill activation for replay
            if (context.Unit.UnitInstance != null)
            {
                var targetIds =
                    context.Participants.Targets?.Select(t => t.Id).ToArray()
                    ?? Array.Empty<string>();
                var command = new SkillCommand(
                    context.Unit.UnitInstance.Id,
                    _skillTemplate.SkillName,
                    targetIds,
                    context.Brain.battleBrain.CurrentTurnNumber
                );
                context.Brain.ExecuteCommand(command);
            }

            // Publish to Brain for centralized tracking
            context.Brain.PublishSkillTriggered(context.Unit.UnitInstance, _skillTemplate);

            // Track use count so SkillUseCountNode can read it during graph execution
            if (context.Skill.SkillUseCount != null)
            {
                context.Skill.SkillUseCount.TryGetValue(_skillTemplate, out int prev);
                context.Skill.SkillUseCount[_skillTemplate] = prev + 1;
            }

            // Execute the behavior graph (individual effects use their own commands)
            _skillTemplate.BehaviorGraph.Execute(context);

            // Reset ready state after execution
            _readyToFire = false;
        }

        public void SetReadyToFire(bool ready) => _readyToFire = ready;

        public void SetEquipped(bool equipped, CharacterInstance owner = null) =>
            _equipped = equipped;
    }
}
