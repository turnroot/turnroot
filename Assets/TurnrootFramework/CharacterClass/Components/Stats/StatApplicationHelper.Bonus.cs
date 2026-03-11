using System.Collections.Generic;
using Turnroot.Characters.Stats;

namespace Turnroot.Characters.CharacterClass
{
    public static partial class StatApplicationHelper
    {
        #region Bonus Application

        /// <summary>
        /// Wrapper: apply bounded stat modifiers to a character's bonus values.
        /// </summary>
        public static void ApplyBoundedBonuses(
            List<StatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        ) => character?.ApplyBoundedBonuses(modifiers);

        /// <summary>
        /// Wrapper: apply unbounded stat modifiers to a character's bonus values.
        /// </summary>
        public static void ApplyUnboundedBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        ) => character?.ApplyUnboundedBonuses(modifiers);

        /// <summary>
        /// Wrapper: remove bounded stat modifiers from a character's bonus values.
        /// </summary>
        public static void RemoveBoundedBonuses(
            List<StatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        ) => character?.RemoveBoundedBonuses(modifiers);

        /// <summary>
        /// Wrapper: remove unbounded stat modifiers from a character's bonus values.
        /// </summary>
        public static void RemoveUnboundedBonuses(
            List<UnboundedStatModifier> modifiers,
            CharacterInstance character,
            string context = ""
        ) => character?.RemoveUnboundedBonuses(modifiers);

        #endregion
    }
}
