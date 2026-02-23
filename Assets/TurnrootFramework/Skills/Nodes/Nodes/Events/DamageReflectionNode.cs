using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Reflects a percentage of received damage back to the attacker.
    /// </summary>
    [CreateNodeMenu("Events/Defensive/Damage Reflection")]
    [NodeLabel("Reflect damage back to attacker")]
    public class DamageReflectionNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("Percentage of damage to reflect (0-100)")]
        public FloatValue reflectionPercent;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var refPort = GetInputPort("reflectionPercent");
            if (refPort == null || !refPort.IsConnected)
            {
                Debug.LogWarning("DamageReflectionNode: 'reflectionPercent' input not provided");
                return;
            }
            float reflectPercent = GetInputFloat("reflectionPercent", 0f);

            // Clamp to valid percentage
            reflectPercent = Mathf.Clamp(reflectPercent, 0f, 100f);

            // Store in CustomData for combat system to check when taking damage
            // Key format: "ReflectDamage_{CharacterInstanceId}"
            var reflectionData = new { Percent = reflectPercent };

            context.SetCustomData($"ReflectDamage_{context.Unit.UnitInstance.Id}", reflectionData);
            $"DamageReflection: Will reflect {reflectPercent}% of damage".LogInfo();
        }
    }
}
