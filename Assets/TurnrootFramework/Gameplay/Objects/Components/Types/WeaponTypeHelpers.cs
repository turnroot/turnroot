using Turnroot.GameSettings;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Objects.Components
{
    public static class WeaponTypeHelpers
    {
        /// <summary>
        /// Return the list of WeaponType assets configured in GameplayGeneralSettings.
        /// </summary>
        public static WeaponType[] GetConfiguredWeaponTypes()
        {
            var settings = GameplayGeneralSettings.Instance;
            return settings != null && settings.WeaponTypes != null
                ? settings.WeaponTypes
                : System.Array.Empty<WeaponType>();
        }

        /// <summary>
        /// Compares two WeaponType instances for equivalence. Prefers reference equality
        /// but falls back to matching on Id if both are present.
        /// </summary>
        public static bool Equals(WeaponType a, WeaponType b) =>
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
