using System;
using Turnroot.Graphics2D;
using UnityEngine;

namespace Turnroot.Characters.Subclasses
{
    [Serializable]
    public class Portrait : StackedImage<CharacterData>
    {
        protected override string GetSaveSubdirectory()
        {
            return "Portraits";
        }

        public override void UpdateTintColorsFromOwner()
        {
            // Ensure array is initialized
            if (_tintColors == null || _tintColors.Length < 3)
            {
                _tintColors = new Color[3] { Color.white, Color.white, Color.white };
            }

            if (_owner != null)
            {
                _tintColors[0] = _owner.AccentColor1;
                _tintColors[1] = _owner.AccentColor2;
                _tintColors[2] = _owner.AccentColor3;
            }
        }

        // These are instance helpers for portrait-specific default handling.
        // They are not overrides because the base class doesn't define them.
        public void SaveDefaults()
        {
            // Intentionally empty - portrait-specific default saving can be implemented here.
        }

        public void LoadDefaults()
        {
            // Intentionally empty - portrait-specific default loading can be implemented here.
        }
    }
}
