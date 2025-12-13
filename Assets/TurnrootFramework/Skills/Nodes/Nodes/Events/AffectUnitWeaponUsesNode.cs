using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Neutral/Affect Unit Weapon Uses")]
    [NodeLabel("Modifies the remaining uses of the unit's equipped weapon")]
    public class AffectUnitWeaponUsesNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount to change weapon uses by (positive to restore, negative to reduce)")]
        public FloatValue usesChange;

        [Tooltip("Test value for uses change in editor mode")]
        public float testChange = 5f;

        [Tooltip("Apply to unit's weapon or target's weapon")]
        public bool applyToUnit = true;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var targetCharacter = applyToUnit
                ? context.UnitInstance
                : (
                    context.Targets != null && context.Targets.Count > 0 ? context.Targets[0] : null
                );

            if (targetCharacter == null)
            {
                Debug.LogWarning("AffectUnitWeaponUses: No valid character to affect");
                return;
            }

            int change = (int)GetInputFloat("usesChange", testChange);

            context.Brain?.PublishWeaponUsesChanged(targetCharacter, change);
            string target = applyToUnit ? "unit" : "target";
            Debug.Log(
                $"AffectUnitWeaponUses: Changed {target} ({targetCharacter.CharacterTemplate.DisplayName}) weapon uses by {change}"
            );
        }
    }
}
