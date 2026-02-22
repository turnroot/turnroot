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

        [Tooltip("Test value for affectAllTargets in editor mode")]
        public bool testAffectAll = false;

        public override void Execute(BattleContext context)
        {
            if (context?.Participants?.Targets == null || context.Participants.Targets.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("BreakWeapon: No target in context");
#endif
                return;
            }

            bool shouldAffectAll = GetInputBool("affectAllTargets", testAffectAll);

            if (shouldAffectAll)
            {
                foreach (var target in context.Participants.Targets)
                {
                    // Store break weapon command in CustomData
                    context.SetCustomData($"BreakWeapon_{target.Id}", true);
                }

                $"BreakWeapon: Would break weapon for {context.Participants.Targets.Count} targets"
            .LogInfo();
            }
            else
            {
                var target = context.Participants.Targets[0];
                // Store break weapon command in CustomData
                context.SetCustomData($"BreakWeapon_{target.Id}", true);
#if UNITY_EDITOR
                "BreakWeapon: Would break weapon for first target".LogInfo();
#endif
            }
        }
    }
}

