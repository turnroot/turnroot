using System.Linq;
using Turnroot.Utilities;

namespace Turnroot.Characters
{
    /// <summary>
    /// Runtime helpers for SpeciesType operations.
    /// </summary>
    public static class SpeciesTypeHelpers
    {
        /// <summary>
        /// Return the list of SpeciesType assets configured in GameplayGeneralSettings.
        /// </summary>
        public static SpeciesType[] GetConfiguredSpeciesTypes()
        {
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            if (settings != null && settings.SpeciesTypes != null)
            {
                return settings.SpeciesTypes;
            }

            return System.Array.Empty<SpeciesType>();
        }

        /// <summary>
        /// Compares two SpeciesType instances for equivalence. Prefers reference equality
        /// but falls back to matching on Id if both are present.
        /// </summary>
        public static bool Equals(SpeciesType a, SpeciesType b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(a.Id) && !string.IsNullOrEmpty(b.Id))
            {
                return a.Id == b.Id;
            }

            return a.name == b.name;
        }
    }
}
