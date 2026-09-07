using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        /// <summary>
        /// Try to instantiate a prefab under <paramref name="parent"/> with a consistent name and logging.
        /// Returns the created GameObject or null on failure.
        /// </summary>
        public static GameObject TryInstantiatePrefab(
            GameObject prefab,
            Transform parent,
            string name = null,
            string context = null
        )
        {
            if (prefab == null)
            {
                if (!string.IsNullOrEmpty(context))
                {
                    // We cannot call instance LogWarning from static context; use the logger directly.
                    $"{context}: prefab is null".LogWarning();
                }
                return null;
            }

            var instance = Instantiate(prefab, parent);
            if (instance == null)
            {
                $"{context ?? "UnitAppearanceBrain"}: Failed to instantiate prefab '{prefab.name}'".LogWarning();
                return null;
            }

            if (!string.IsNullOrEmpty(name))
            {
                instance.name = name;
            }

            return instance;
        }
    }
}
