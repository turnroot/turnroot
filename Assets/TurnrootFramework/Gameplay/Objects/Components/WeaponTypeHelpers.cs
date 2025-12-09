using System.Linq;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Components
{
    public static class WeaponTypeHelpers
    {
        /// <summary>
        /// Return the list of WeaponType assets configured in GameplayGeneralSettings.
        /// </summary>
        public static WeaponType[] GetConfiguredWeaponTypes()
        {
            var settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>("GameSettings");
            return settings != null && settings.WeaponTypes != null ? settings.WeaponTypes : System.Array.Empty<WeaponType>();
        }

        /// <summary>
        /// Compares two WeaponType instances for equivalence. Prefers reference equality
        /// but falls back to matching on Id if both are present.
        /// </summary>
        public static bool Equals(WeaponType a, WeaponType b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(a.Id) && !string.IsNullOrEmpty(b.Id) ? a.Id == b.Id : a.name == b.name;
        }
    }
}
