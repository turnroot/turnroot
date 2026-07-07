using System.Collections.Generic;
using Turnroot.Utilities;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Permanent Bonuses

        /// <summary>
        /// Apply permanent stat increases (class change bonuses).
        /// Handles both bounded (HP, via isBounded) and unbounded stats.
        /// </summary>
        public static void ApplyPermanentBonuses(
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

            var brain = GetAndCacheBrain.GetBrain();

            foreach (var modifier in modifiers)
            {
                if (modifier.value != 0)
                {
                    if (modifier.isBounded)
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

                            brain?.PublishCharacterBoundedStatChanged(
                                character,
                                modifier.boundedStatType,
                                oldVal,
                                newVal
                            );
                        }
                    }
                    else
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
        }

        #endregion
    }
}
