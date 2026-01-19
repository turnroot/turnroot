using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public class UnitContext
    {
        public CharacterInstance UnitInstance { get; set; }
    }

    public class SkillContext
    {
        public Skill CurrentSkill { get; set; }
        public List<Skill> ActiveSkills { get; set; }
        public List<SkillGraph> ActiveSkillGraphs { get; set; }
        public Dictionary<Skill, int> SkillUseCount { get; set; }
        public SkillGraph CurrentSkillGraph { get; set; }
    }

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

    public class UnitFlag
    {
        public CharacterInstance Unit { get; set; }
        public bool WillCriticalHit { get; set; }
        public bool AnotherTurnGranted { get; set; }
        public bool CanFinishMovingAfterAction { get; set; }
    }

    public class CombatFlags
    {
        public bool IsInterrupted { get; set; }
        public UnitFlag ActiveUnitFlags { get; set; }
    }
}
