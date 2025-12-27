using Turnroot.GameSettings;
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
            return settings != null && settings.SpeciesTypes != null
                ? settings.SpeciesTypes
                : System.Array.Empty<SpeciesType>();
        }

        public static bool Equals(SpeciesType a, SpeciesType b) =>
            ReferenceEquals(a, b)
            || (
                a != null
                && b != null
                && (
                    !string.IsNullOrEmpty(a.Id) && !string.IsNullOrEmpty(b.Id)
                        ? a.Id == b.Id
                        : a.name == b.name
                )
            );
    }
}
