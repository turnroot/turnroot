using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Affect Unit Weapon Uses")]
    [NodeLabel("Modifies the remaining uses of the unit's equipped weapon")]
    public class AffectUnitWeaponUses : SkillNode
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

        public override void Execute(SkillExecutionContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("AffectUnitWeaponUses: No context provided");
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

            // Get the uses change value
            float change = testChange;
            var changePort = GetInputPort("usesChange");
            if (changePort != null && changePort.IsConnected)
            {
                var inputValue = changePort.GetInputValue();
                if (inputValue is FloatValue floatValue)
                {
                    change = floatValue.value;
                }
            }

            // TODO: Integrate with actual weapon/inventory system
            context.SetCustomData("WeaponUsesChange", change);
            context.SetCustomData("WeaponUsesApplyToUnit", applyToUnit);

            string target = applyToUnit ? "unit" : "target";
            Debug.Log($"AffectUnitWeaponUses: Changed {target} weapon uses by {change}");
        }
    }
}
