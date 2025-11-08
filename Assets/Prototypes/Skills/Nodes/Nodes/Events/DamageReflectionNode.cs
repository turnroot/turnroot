using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Damage Reflection")]
    [NodeLabel("Reflect damage back to attacker")]
    public class DamageReflectionNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("Percentage of damage to reflect (0-100)")]
        public FloatValue reflectionPercent;

        [Tooltip("Test value for reflection % in editor mode")]
        [Range(0, 100)]
        public float testReflectionPercent = 50.0f;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("DamageReflection: No context provided");
                return;
            }

            float reflectPercent = GetInputFloat("reflectionPercent", testReflectionPercent);

            // Clamp to valid percentage
            reflectPercent = Mathf.Clamp(reflectPercent, 0f, 100f);

            // Store in CustomData for combat system to check when taking damage
            // Key format: "ReflectDamage_{CharacterInstanceId}"
            var reflectionData = new { Percent = reflectPercent };

            context.SetCustomData($"ReflectDamage_{context.UnitInstance.Id}", reflectionData);

            Debug.Log($"DamageReflection: Will reflect {reflectPercent}% of damage");
        }
    }
}
