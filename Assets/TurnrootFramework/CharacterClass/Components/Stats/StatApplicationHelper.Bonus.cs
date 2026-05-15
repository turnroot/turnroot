using System.Collections.Generic;
using Turnroot.Characters.Stats;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Bonus Application

        /// <summary>
        /// Apply stat modifiers to a character's bonus values. Handles both bounded (HP) and unbounded stats.
        /// </summary>
        public static void ApplyStatBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        ) => character?.ApplyStatBonuses(modifiers);

        /// <summary>
        /// Remove stat modifiers from a character's bonus values. Handles both bounded (HP) and unbounded stats.
        /// </summary>
        public static void RemoveStatBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        ) => character?.RemoveStatBonuses(modifiers);

        #endregion
    }
}
