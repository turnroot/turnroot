using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Permanent Bonuses

        /// <summary>
        /// Apply permanent bounded stat increases (class change bonuses).
        /// </summary>
        /// <param name="modifiers">List of stat modifiers to apply permanently</param>
        /// <param name="character">Target character instance</param>
        /// <param name="logChanges">Whether to log changes to console</param>
        public static void ApplyBoundedPermanentBonuses(
            List<StatModifier> modifiers,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (
                !ValidationHelper.ValidateNotNull(character, nameof(character))
                || !ValidationHelper.ValidateNotNull(modifiers, nameof(modifiers))
            )
            {
                return;
            }

            var brain = Object.FindFirstObjectByType<Gameplay.Brain.Brain>();

            foreach (var modifier in modifiers)
            {
                if (modifier.value != 0)
                {
                    var stat = character.GetBoundedStat(modifier.boundedStatType);
                    if (stat != null)
                    {
                        float oldVal = stat.Current;
                        stat.SetCurrent(stat.Current + modifier.value);
                        float newVal = stat.Current;

                        if (logChanges)
                        {
                            $"Class change bonus: {modifier.boundedStatType} +{modifier.value} (now {stat.Current})".LogInfo();
                        }

                        // Publish stat-changed event so UI/other systems can react
                        brain?.PublishCharacterBoundedStatChanged(
                            character,
                            modifier.boundedStatType,
                            oldVal,
                            newVal
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Apply permanent unbounded stat increases (class change bonuses).
        /// </summary>
        /// <param name="modifiers">List of stat modifiers to apply permanently</param>
        /// <param name="character">Target character instance</param>
        /// <param name="logChanges">Whether to log changes to console</param>
        public static void ApplyUnboundedPermanentBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (
                !ValidationHelper.ValidateNotNull(character, nameof(character))
                || !ValidationHelper.ValidateNotNull(modifiers, nameof(modifiers))
            )
            {
                return;
            }

            var brain = Object.FindFirstObjectByType<Gameplay.Brain.Brain>();

            foreach (var modifier in modifiers)
            {
                if (modifier.value != 0)
                {
                    var stat = character.GetUnboundedStat(modifier.unboundedStatType);
                    if (stat != null)
                    {
                        float oldVal = stat.Current;
                        stat.SetCurrent(stat.Current + modifier.value);
                        float newVal = stat.Current;

                        if (logChanges)
                        {
                            $"Class change bonus: {modifier.unboundedStatType} +{modifier.value} (now {stat.Current})".LogInfo();
                        }

                        // Publish stat-changed event so UI/other systems can react
                        brain?.PublishCharacterUnboundedStatChanged(
                            character,
                            modifier.unboundedStatType,
                            oldVal,
                            newVal
                        );
                    }
                }
            }
        }

        #endregion
    }
}
