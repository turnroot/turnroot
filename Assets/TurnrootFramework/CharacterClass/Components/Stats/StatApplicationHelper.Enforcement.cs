using System.Collections.Generic;
using Turnroot.Utilities;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Stat Enforcement

        /// <summary>
        /// Enforce stat minimums — raise any stat below its minimum up to the minimum value.
        /// Handles both bounded (HP, via isBounded) and unbounded stats.
        /// </summary>
        public static OperationResult EnforceStatMinimums(
            List<UnboundedStatModifier> minimums,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (character == null || minimums == null)
            {
                return OperationResult.Failure(
                    "EnforceStatMinimums: character or minimums is null."
                );
            }

            var brain = GetAndCacheBrain.GetBrain();

            foreach (var minimum in minimums)
            {
                if (minimum.value > 0)
                {
                    if (minimum.isBounded)
                    {
                        var stat = character.GetBoundedStat(minimum.boundedStatType);
                        if (stat != null && stat.Current < minimum.value)
                        {
                            float oldVal = stat.Current;
                            stat.SetCurrent(minimum.value);
                            float newVal = stat.Current;

                            if (logChanges)
                            {
                                $"Enforced minimum: {minimum.boundedStatType} raised to {minimum.value}".LogInfo();
                            }

                            brain?.PublishCharacterBoundedStatChanged(
                                character,
                                minimum.boundedStatType,
                                oldVal,
                                newVal
                            );
                        }
                    }
                    else
                    {
                        var stat = character.GetUnboundedStat(minimum.unboundedStatType);
                        if (stat != null && stat.Current < minimum.value)
                        {
                            float oldVal = stat.Current;
                            stat.SetCurrent(minimum.value);
                            float newVal = stat.Current;

                            if (logChanges)
                            {
                                $"Enforced minimum: {minimum.unboundedStatType} raised to {minimum.value}".LogInfo();
                            }

                            brain?.PublishCharacterUnboundedStatChanged(
                                character,
                                minimum.unboundedStatType,
                                oldVal,
                                newVal
                            );
                        }
                    }
                }
            }
            return OperationResult.Successful();
        }

        /// <summary>
        /// Apply stat caps — sets the maximum value for bounded (HP) stats and checks unbounded stat caps.
        /// </summary>
        public static void ApplyStatCaps(
            List<UnboundedStatModifier> caps,
            CharacterInstance character
        )
        {
            if (character == null || caps == null)
            {
                return;
            }

            foreach (var cap in caps)
            {
                if (cap.value > 0 && cap.isBounded)
                {
                    var stat = character.GetBoundedStat(cap.boundedStatType);
                    stat?.SetMax(cap.value);
                }
            }
        }

        /// <summary>
        /// Check if any unbounded stat exceeds class caps.
        /// </summary>
        public static bool IsAboveStatCaps(
            List<UnboundedStatModifier> caps,
            CharacterInstance character
        )
        {
            if (character == null || caps == null)
            {
                return false;
            }

            foreach (var cap in caps)
            {
                if (cap.value > 0 && !cap.isBounded)
                {
                    var stat = character.GetUnboundedStat(cap.unboundedStatType);
                    if (stat != null && stat.Current > cap.value)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
