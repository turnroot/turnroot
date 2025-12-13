using Turnroot.Characters.StatusEffects;
using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Status/Has Debuff")]
    [NodeLabel("Checks if a unit has a debuff")]
    public class HasDebuffNode : SkillNode
    {
        [Output]
        public BoolValue UnitHasDebuff;

        [Output]
        public BoolValue EnemyHasDebuff;

        [Output]
        public BoolValue AllyHasDebuff;

        [Tooltip("Specific debuff type to check (leave empty to check for any debuff)")]
        public StatusEffectType debuffType;

        [Tooltip("Alternative: Check by debuff name string (used if debuffType is not set)")]
        public string debuffName = "";

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
                "UnitHasDebuff" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Unit
                ),
                "EnemyHasDebuff" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Enemy
                ),
                "AllyHasDebuff" => ConditionHelpers.GetCharacterFromContext(
                    context,
                    ConditionHelpers.CharacterSource.Ally
                ),
                _ => null,
            };

            if (character == null)
            {
                return new BoolValue { value = false };
            }

            // Check for debuffs using the typed StatusEffect system
            bool hasDebuff;
            if (debuffType != null)
            {
                // Check for specific debuff type using the StatusEffectType reference
                hasDebuff = character.HasStatusEffect(debuffType);
            }
            else if (!string.IsNullOrEmpty(debuffName))
            {
                // Fallback: Check by name string
                hasDebuff = character.HasStatusEffectByName(debuffName);
            }
            else
            {
                // Check for any debuff
                hasDebuff = character.HasAnyDebuff();
            }

            return new BoolValue { value = hasDebuff };
        }
    }
}
