using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Breaks the target enemy's equipped weapon.
    /// </summary>
    [CreateNodeMenu("Events/Offensive/Break Weapon")]
    [NodeLabel("Break enemy's equipped weapon")]
    public class BreakWeaponNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, breaks all targeted enemies' weapons; if false, only first target")]
        public BoolValue affectAllTargets;

        public override void Execute(BattleContext context)
        {
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
                "BreakWeapon: No target in context".LogWarning();
                return;
            }

            bool shouldAffectAll = GetInputValue("affectAllTargets", affectAllTargets).value;

            if (shouldAffectAll)
            {
                int broken = 0;
                foreach (var target in context.Participants.Targets)
                {
                    if (target == null)
                    {
                        continue;
                    }
                    // Store break weapon command in CustomData
                    context.SetCustomData($"BreakWeapon_{target.Id}", true);
                    broken++;
                }

                $"BreakWeapon: Would break weapon for {broken} targets".LogInfo();
            }
            else
            {
                var target = context.Participants.Targets[0];
                if (target == null)
                {
                    "BreakWeapon: First target is null".LogWarning();
                    return;
                }
                // Store break weapon command in CustomData
                context.SetCustomData($"BreakWeapon_{target.Id}", true);
                "BreakWeapon: Would break weapon for first target".LogInfo();
            }
        }
    }
}
