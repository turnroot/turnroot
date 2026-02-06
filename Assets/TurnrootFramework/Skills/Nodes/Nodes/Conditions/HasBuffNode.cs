using Turnroot.Characters.StatusEffects;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if a character (unit, enemy, or ally) has a specific buff or any buff.
    /// </summary>
    [CreateNodeMenu("Conditions/Status/Has Buff")]
    [NodeLabel("Checks if a unit has a buff")]
    public class HasBuffNode : SkillNode
    {
        [Output]
        public BoolValue UnitHasBuff;

        [Output]
        public BoolValue EnemyHasBuff;

        [Output]
        public BoolValue AllyHasBuff;

        [Tooltip("Specific buff type to check (leave empty to check for any buff)")]
        public StatusEffectType buffType;

        [Tooltip("Alternative: Check by buff name string (used if buffType is not set)")]
        public string buffName = "";

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                // Return false in editor mode
                return new BoolValue { value = false };
            }

            // Get context
            var context = GetContextFromGraph(skillGraph);
            if (context == null)
            {
                return new BoolValue { value = false };
            }

            // Determine which character to check based on port
            var character = port.fieldName switch
            {
                "UnitHasBuff" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Unit
                ),
                "EnemyHasBuff" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Enemy
                ),
                "AllyHasBuff" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Ally
                ),
                _ => null,
            };

            if (character == null)
            {
                return new BoolValue { value = false };
            }

            // Check for buffs using the typed StatusEffect system
            bool hasBuff;
            if (buffType != null)
            {
                // Check for specific buff type using the StatusEffectType reference
                hasBuff = character.HasStatusEffect(buffType);
            }
            else if (!string.IsNullOrEmpty(buffName))
            {
                // Fallback: Check by name string
                hasBuff = character.HasStatusEffectByName(buffName);
            }
            else
            {
                // Check for any buff
                hasBuff = character.HasAnyBuff();
            }

            return new BoolValue { value = hasBuff };
        }
    }
}
