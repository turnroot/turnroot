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

        [Output]
        public ExecutionFlow OutFlow;

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
                "DamageReflectionNode: 'reflectionPercent' input not provided".LogWarning();
                return;
            }
            float reflectPercent = GetInputFloat("reflectionPercent", 0f);

            // Clamp to valid percentage
            reflectPercent = Mathf.Clamp(reflectPercent, 0f, 100f);

            // Store in CustomData for combat system to check when taking damage
            // Key format: "ReflectDamage_{CharacterInstanceId}"
            var reflectionData = new DamageReflectionData { Percent = reflectPercent };

            context.SetCustomData($"ReflectDamage_{context.Unit.UnitInstance.Id}", reflectionData);
            $"DamageReflection: Will reflect {reflectPercent}% of damage".LogInfo();
        }
    }

    /// <summary>
    /// Data written into CustomData under the key <c>ReflectDamage_{CharacterId}</c>.
    /// Read by the combat system when the unit takes damage.
    /// </summary>
    public struct DamageReflectionData
    {
        /// <summary>Percentage of received damage to reflect back to the attacker (0-100).</summary>
        public float Percent;
    }
}
