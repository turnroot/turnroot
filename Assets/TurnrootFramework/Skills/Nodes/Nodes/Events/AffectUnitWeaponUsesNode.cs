using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Modifies the remaining uses of a unit's equipped weapon.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Affect Unit Weapon Uses")]
    [NodeLabel("Modifies the remaining uses of the unit's equipped weapon")]
    public class AffectUnitWeaponUsesNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount to change weapon uses by (positive to restore, negative to reduce)")]
        public FloatValue usesChange;

        [Tooltip("Apply to unit's weapon or target's weapon")]
        public bool applyToUnit = true;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var targetCharacter = applyToUnit
                ? context.Unit.UnitInstance
                : (
                    context.Participants.Targets != null && context.Participants.Targets.Count > 0
                        ? context.Participants.Targets[0]
                        : null
                );

            if (targetCharacter == null)
            {
                "AffectUnitWeaponUses: No valid character to affect".LogWarning();
                return;
            }

            var port = GetInputPort("usesChange");
            if (port == null || !port.IsConnected)
            {
                "AffectUnitWeaponUsesNode: 'usesChange' input not provided".LogWarning();
                return;
            }
            int change = (int)GetInputFloat("usesChange", 0f);

            context.Brain.PublishWeaponUsesChanged(targetCharacter, change);
            string target = applyToUnit ? "unit" : "target";

            $"AffectUnitWeaponUses: Changed {target} ({targetCharacter.CharacterTemplate.DisplayName}) weapon uses by {change}".LogInfo();
        }
    }
}
