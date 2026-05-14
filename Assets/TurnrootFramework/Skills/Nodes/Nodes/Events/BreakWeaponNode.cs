using Turnroot.Characters;
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

        [Output]
        public ExecutionFlow OutFlow;

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
                    if (target != null && BreakEquippedWeapon(target, context))
                    {
                        broken++;
                    }
                }
                $"BreakWeapon: Broke weapon for {broken} target(s)".LogInfo();
            }
            else
            {
                var target = context.Participants.Targets[0];
                if (target == null)
                {
                    "BreakWeapon: First target is null".LogWarning();
                    return;
                }
                if (BreakEquippedWeapon(target, context))
                {
                    $"BreakWeapon: Broke {target.CharacterTemplate.DisplayName}'s weapon".LogInfo();
                }
            }
        }

        private static bool BreakEquippedWeapon(CharacterInstance target, BattleContext context)
        {
            var weapon = target.GetEquippedWeapon();
            if (weapon == null)
            {
                $"BreakWeapon: {target.CharacterTemplate.DisplayName} has no equipped weapon".LogWarning();
                return false;
            }

            if (weapon.Template?.Durability != true)
            {
                $"BreakWeapon: {target.CharacterTemplate.DisplayName}'s weapon has infinite durability".LogInfo();
                return false;
            }

            // Set CurrentUses to MaxUses so RemainingUses == 0 (weapon is depleted/broken)
            weapon.CurrentUses = weapon.Template.MaxUses;
            context.Brain.PublishWeaponUsesChanged(target, 0);
            return true;
        }
    }
}
