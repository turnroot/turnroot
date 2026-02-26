using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes
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
            Unit, // From context.Unit.UnitInstance
            Enemy, // From context.Targets[0]
            Ally, // From context.Allies[0]
        }

        public static CharacterInstance GetCharacterFromContext(
            BattleContext context,
            CharacterSource source
        )
        {
            return context == null
                ? null
                : source switch
                {
                    CharacterSource.Unit => context.Unit.UnitInstance,
                    CharacterSource.Enemy => context.Participants.Targets != null
                    && context.Participants.Targets.Count > 0
                        ? context.Participants.Targets[0]
                        : null,
                    CharacterSource.Ally => context.Participants.Allies != null
                    && context.Participants.Allies.Count > 0
                        ? context.Participants.Allies[0]
                        : null,
                    _ => null,
                };
        }

        public static BaseCharacterStat GetStatFromCharacter(
            CharacterInstance character,
            string statName,
            bool isBoundedStat
        )
        {
            if (character == null || string.IsNullOrEmpty(statName))
            {
                return null;
            }

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

            $"{node.GetType().Name}: Unable to retrieve runtime value for {statName}, returning fallback.".LogWarning();
            return fallbackValue;
        }

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

            $"{node.GetType().Name}: Unable to retrieve max value for {statName}, returning fallback.".LogWarning();
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

            $"{node.GetType().Name}: Unable to retrieve percentage for {statName}, returning fallback.".LogWarning();
            return fallbackValue;
        }

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
