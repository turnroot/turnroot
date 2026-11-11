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
        /// <summary>
        /// Allies, enemies, and third-party units participating in the battle, along with their adjacency and targets in range.
        /// Targeted units are the ones the attack or skill is actively looking at for the context of a single action,
        /// in range are in range but not necessarily being targeted right now by this action.
        /// Take enemies: Enemies includes EnemiesInRange which includes CurrentTargetedEnemies
        /// it's almost a hierarchy but it's really more of a funnel
        /// They update in reverse-ish order- CurrentTargetedEnemies should be cleared between all actions, EnemiesInRange probably should too,
        /// but Enemies only changes when a new enemy is spawned or killed. Etc.
        /// </summary>
        public List<CharacterInstance> Enemies { get; set; } // all enemies
        public List<CharacterInstance> CurrentTargetedEnemies { get; set; } // targeted enemies for the current action. Usually just one,
        // special skills or area of attack affects might target multiple
        public List<CharacterInstance> Allies { get; set; } // all allies
        public List<CharacterInstance> CurrentTargetedAllies { get; set; } // targeted allies for enemy action. same as above
        public List<CharacterInstance> ThirdParty { get; set; }

        public List<CharacterInstance> ThirdPartyTargets { get; set; } // targeted third party for current action. same as above
        public Adjacency AdjacentUnits { get; set; } // works for enemies or allies, clear often
        public List<CharacterInstance> EnemiesInRange { get; set; }
        public List<CharacterInstance> AlliesInRange { get; set; }
        public List<CharacterInstance> ThirdPartyInRange { get; set; }

        public List<CharacterInstance> GetCurrentTargets()
        {
            var targets = new List<CharacterInstance>();
            if (CurrentTargetedEnemies != null)
            {
                targets.AddRange(CurrentTargetedEnemies);
            }
            if (CurrentTargetedAllies != null)
            {
                targets.AddRange(CurrentTargetedAllies);
            }
            if (ThirdPartyTargets != null)
            {
                targets.AddRange(ThirdPartyTargets);
            }
            return targets;
        }

        public List<CharacterInstance> GetAllUnits()
        {
            var result = new List<CharacterInstance>();
            result.AddRange(Allies);
            result.AddRange(Enemies);
            result.AddRange(ThirdParty);
            return result;
        }
    }

    public class UnitFlag
    {
        public CharacterInstance Unit { get; set; }
        public bool WillCriticalHit { get; set; }
        public bool WillDodge { get; set; }
        public bool WillCounter { get; set; }
        public bool WillKill { get; set; }
        public bool WillBeKilled { get; set; }
        public bool AnotherTurnGranted { get; set; }
        public bool CanFinishMovingAfterAction { get; set; }
        public bool IsMounted => Unit?.IsMounted ?? false;
    }

    public class CombatFlags
    {
        public bool IsInterrupted { get; set; }
        public UnitFlag ActiveUnitFlags { get; set; }
    }
}
