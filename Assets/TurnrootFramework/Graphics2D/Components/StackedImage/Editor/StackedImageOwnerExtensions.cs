using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Graphics2D.Editor
{
    /// <summary>
    /// Provide extension methods so editor code can call SaveDefaults/LoadDefaults
    /// on a generic TOwner constrained to UnityEngine.Object.
    /// These delegate to CharacterData when applicable.
    /// </summary>
    public static class StackedImageOwnerExtensions
    {
        public static void SaveDefaults(this UnityEngine.Object owner)
        {
            if (owner is CharacterData c)
            {
                c.SaveDefaults();
            }
        }

        public static void LoadDefaults(this UnityEngine.Object owner)
        {
            if (owner is CharacterData c)
            {
                c.LoadDefaults();
            }
        }
    }
}
