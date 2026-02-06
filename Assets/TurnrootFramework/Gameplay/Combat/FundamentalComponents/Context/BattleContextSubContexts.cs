using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Holds context information for a single unit during combat actions.
    /// </summary>
    public class UnitContext
    {
        public CharacterInstance UnitInstance { get; set; }
    }

    /// <summary>
    /// Tracks active skills, skill graphs, and usage counts for the current battle context.
    /// </summary>
    public class SkillContext
    {
        public Skill CurrentSkill { get; set; }
        public List<Skill> ActiveSkills { get; set; }
        public List<SkillGraph> ActiveSkillGraphs { get; set; }
        public Dictionary<Skill, int> SkillUseCount { get; set; }
        public SkillGraph CurrentSkillGraph { get; set; }
    }

    /// <summary>
    /// Contains collections of all participating units categorized by team allegiance and adjacency.
    /// </summary>
    public class BattleParticipants
    {
        public List<CharacterInstance> Targets { get; set; }
        public List<CharacterInstance> Allies { get; set; }
        public List<CharacterInstance> ThirdParty { get; set; }
        public Adjacency AdjacentUnits { get; set; }

        public List<CharacterInstance> GetAllUnits()
        {
            var result = new List<CharacterInstance>();
            result.AddRange(Allies);
            result.AddRange(Targets);
            result.AddRange(ThirdParty);
            return result;
        }
    }

    /// <summary>
    /// Stores combat flags and state modifiers for a specific unit during their turn.
    /// </summary>
    public class UnitFlag
    {
        public CharacterInstance Unit { get; set; }
        public bool WillCriticalHit { get; set; }
        public bool AnotherTurnGranted { get; set; }
        public bool CanFinishMovingAfterAction { get; set; }
    }

    /// <summary>
    /// Global combat flags that affect the flow and state of the current battle phase.
    /// </summary>
    public class CombatFlags
    {
        public bool IsInterrupted { get; set; }
        public UnitFlag ActiveUnitFlags { get; set; }
    }
}
