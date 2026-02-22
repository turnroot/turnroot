using System;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Command to activate a skill.
    /// </summary>
    public class SkillCommand : CommandBase
    {
        public string CasterId { get; }
        public string SkillId { get; }
        public string[] TargetIds { get; }

        public SkillCommand(string casterId, string skillId, string[] targetIds, int turn)
            : base(turn)
        {
            CasterId = casterId;
            SkillId = skillId;
            TargetIds = targetIds ?? Array.Empty<string>();
        }

        public override bool Execute(BattleContext context) => true;

        public override bool Undo(BattleContext context)
        {
            // Skill effects are undone through their individual commands (damage, buffs, etc.)
            "[SkillCommand] Skill activation record cannot be undone".LogWarning();
            return false;
        }
    }
}
