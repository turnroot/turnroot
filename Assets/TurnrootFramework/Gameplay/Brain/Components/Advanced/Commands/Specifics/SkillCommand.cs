using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

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
            TurnrootLogger.Log(
                "[SkillCommand] Skill activation record cannot be undone",
                TurnrootLogger.LogLevel.Warning
            );
            return false;
        }
    }
}