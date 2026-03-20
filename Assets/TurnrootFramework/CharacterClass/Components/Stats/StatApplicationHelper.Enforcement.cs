using System.Collections.Generic;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Stat Enforcement

        /// <summary>
        /// Enforce bounded stat minimums - raise stats to minimum if below.
        /// </summary>
        /// <param name="minimums">List of minimum stat values</param>
        /// <param name="character">Target character instance</param>
        /// <param name="logChanges">Whether to log changes to console</param>
        public static void EnforceBoundedMinimums(
            List<StatModifier> minimums,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (character == null || minimums == null)
            {
                return;
            }

            var brain = Object.FindFirstObjectByType<Gameplay.Brain.Brain>();

            foreach (var minimum in minimums)
            {
                if (minimum.value > 0)
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
            }
        }

        /// <summary>
        /// Enforce unbounded stat minimums - raise stats to minimum if below.
        /// </summary>
        public static OperationResult EnforceUnboundedMinimums(
            List<UnboundedStatModifier> minimums,
            CharacterInstance character,
            bool logChanges = false
        )
        {
            if (character == null || minimums == null)
            {
                return OperationResult.Failure(
                    "EnforceUnboundedMinimums: character or minimums is null."
                );
            }

            var brain = Object.FindFirstObjectByType<Gameplay.Brain.Brain>();

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
        /// Apply bounded stat caps - set maximum values for bounded stats.
        /// </summary>
        public static void ApplyBoundedCaps(List<StatModifier> caps, CharacterInstance character)
        {
            if (character == null || caps == null)
            {
                return;
            }

            foreach (var cap in caps)
            {
                if (cap.value > 0)
                {
                    var stat = character.GetBoundedStat(cap.boundedStatType);
                    if (stat != null)
                    {
                        stat.SetMax(cap.value);
                    }
                }
            }
        }

        /// <summary>
        /// Check if any unbounded stat exceeds class caps.
        /// </summary>
        public static bool IsAboveUnboundedCaps(
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
                if (cap.value > 0)
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
