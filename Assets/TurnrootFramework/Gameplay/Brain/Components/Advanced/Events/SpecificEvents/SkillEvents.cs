using Turnroot.Characters;
using Turnroot.Skills;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Published when a skill is activated.
    /// </summary>
    public class SkillActivatedEvent : BattleEvent
    {
        public CharacterInstance Caster { get; }
        public Skill Skill { get; }
        public CharacterInstance[] Targets { get; }

        public SkillActivatedEvent(
            CharacterInstance caster,
            Skill skill,
            CharacterInstance[] targets
        )
        {
            Caster = caster;
            Skill = skill;
            Targets = targets ?? System.Array.Empty<CharacterInstance>();
        }
    }

    /// <summary>
    /// Published when a skill finishes executing.
    /// </summary>
    public class SkillCompletedEvent : BattleEvent
    {
        public CharacterInstance Caster { get; }
        public Skill Skill { get; }
        public bool WasSuccessful { get; }

        public SkillCompletedEvent(CharacterInstance caster, Skill skill, bool success)
        {
            Caster = caster;
            Skill = skill;
            WasSuccessful = success;
        }
    }
}
