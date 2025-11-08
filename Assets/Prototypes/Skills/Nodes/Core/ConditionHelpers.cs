using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Assets.Prototypes.Skills.Nodes
{
    /// <summary>
    /// Helper methods for condition nodes to reduce code duplication.
    /// Provides common patterns for retrieving stats, characters, and creating outputs.
    /// </summary>
    public static class ConditionHelpers
    {
        /// <summary>
        /// Defines where to get the character from in the context.
        /// </summary>
        public enum CharacterSource
        {
            Unit, // From context.UnitInstance
            Enemy, // From context.Targets[0]
            Ally, // From context.Allies[0]
        }

        /// <summary>
        /// Gets a character instance from the context based on the source.
        /// </summary>
        public static CharacterInstance GetCharacterFromContext(
            Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context,
            CharacterSource source
        )
        {
            if (context == null)
                return null;

            return source switch
            {
                CharacterSource.Unit => context.UnitInstance,
                CharacterSource.Enemy => context.Targets != null && context.Targets.Count > 0
                    ? context.Targets[0]
                    : null,
                CharacterSource.Ally => context.Allies != null && context.Allies.Count > 0
                    ? context.Allies[0]
                    : null,
                _ => null,
            };
        }

        /// <summary>
        /// Gets a stat from a character by name, handling both bounded and unbounded stats.
        /// </summary>
        public static BaseCharacterStat GetStatFromCharacter(
            CharacterInstance character,
            string statName,
            bool isBoundedStat
        )
        {
            if (character == null || string.IsNullOrEmpty(statName))
                return null;

            if (isBoundedStat)
            {
                if (System.Enum.TryParse<BoundedStatType>(statName, out var boundedType))
                {
                    return character.GetBoundedStat(boundedType);
                }
            }
            else
            {
                if (System.Enum.TryParse<UnboundedStatType>(statName, out var unboundedType))
                {
                    return character.GetUnboundedStat(unboundedType);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a FloatValue output with a runtime value or default.
        /// </summary>
        public static FloatValue CreateFloatOutput(float defaultValue, float runtimeValue)
        {
            return new FloatValue { value = Application.isPlaying ? runtimeValue : defaultValue };
        }

        /// <summary>
        /// Creates a BoolValue output with a runtime value or default.
        /// </summary>
        public static BoolValue CreateBoolOutput(bool defaultValue, bool runtimeValue)
        {
            return new BoolValue { value = Application.isPlaying ? runtimeValue : defaultValue };
        }

        /// <summary>
        /// Gets stat current value with fallback handling.
        /// </summary>
        public static float GetStatCurrentValue(
            SkillGraph skillGraph,
            SkillNode node,
            CharacterSource source,
            string statName,
            bool isBoundedStat,
            float fallbackValue
        )
        {
            var context = node.GetContextFromGraph(skillGraph);
            var character = GetCharacterFromContext(context, source);
            var stat = GetStatFromCharacter(character, statName, isBoundedStat);

            if (stat != null)
            {
                return stat.Current;
            }

            Debug.LogWarning(
                $"{node.GetType().Name}: Unable to retrieve runtime value for {statName}, returning fallback."
            );
            return fallbackValue;
        }

        /// <summary>
        /// Gets stat max value (only for bounded stats).
        /// </summary>
        public static float GetStatMaxValue(
            SkillGraph skillGraph,
            SkillNode node,
            CharacterSource source,
            string statName,
            float fallbackValue
        )
        {
            var context = node.GetContextFromGraph(skillGraph);
            var character = GetCharacterFromContext(context, source);
            var stat = GetStatFromCharacter(character, statName, isBoundedStat: true);

            if (stat is BoundedCharacterStat boundedStat)
            {
                return boundedStat.Max;
            }

            Debug.LogWarning(
                $"{node.GetType().Name}: Unable to retrieve max value for {statName}, returning fallback."
            );
            return fallbackValue;
        }

        /// <summary>
        /// Gets stat percentage (Current/Max * 100, only for bounded stats).
        /// </summary>
        public static float GetStatPercentage(
            SkillGraph skillGraph,
            SkillNode node,
            CharacterSource source,
            string statName,
            float fallbackValue = 100f
        )
        {
            var context = node.GetContextFromGraph(skillGraph);
            var character = GetCharacterFromContext(context, source);
            var stat = GetStatFromCharacter(character, statName, isBoundedStat: true);

            if (stat is BoundedCharacterStat boundedStat)
            {
                return boundedStat.Ratio * 100f;
            }

            Debug.LogWarning(
                $"{node.GetType().Name}: Unable to retrieve percentage for {statName}, returning fallback."
            );
            return fallbackValue;
        }

        /// <summary>
        /// Gets stat bonus value.
        /// </summary>
        public static float GetStatBonus(
            SkillGraph skillGraph,
            SkillNode node,
            CharacterSource source,
            string statName,
            bool isBoundedStat
        )
        {
            var context = node.GetContextFromGraph(skillGraph);
            var character = GetCharacterFromContext(context, source);
            var stat = GetStatFromCharacter(character, statName, isBoundedStat);

            return stat?.Bonus ?? 0f;
        }

        /// <summary>
        /// Checks if stat has an active bonus (Bonus != 0).
        /// </summary>
        public static bool GetStatBonusActive(
            SkillGraph skillGraph,
            SkillNode node,
            CharacterSource source,
            string statName,
            bool isBoundedStat
        )
        {
            var context = node.GetContextFromGraph(skillGraph);
            var character = GetCharacterFromContext(context, source);
            var stat = GetStatFromCharacter(character, statName, isBoundedStat);

            return stat != null && Mathf.Abs(stat.Bonus) > 1e-6f;
        }
    }
}
