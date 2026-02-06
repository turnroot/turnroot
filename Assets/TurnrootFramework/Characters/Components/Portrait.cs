using System;
using System.Collections.Generic;
using Turnroot.Graphics2D;
using Turnroot.Graphics2D.Tags;
using UnityEngine;

namespace Turnroot.Characters.Subclasses
{
    /// <summary>
    /// Portrait implementation for characters using layered images.
    /// </summary>
    [Serializable]
    public class Portrait : StackedImage<CharacterData>
    {
        protected override string GetSaveSubdirectory() => "Portraits";

        // Ensure portrait-specific mandatory tags are applied at the object level
        protected override IEnumerable<ILayerTag> MandatoryTags() =>
            PortraitLayerTags.MandatoryTags();

        public override void UpdateTintColorsFromOwner()
        {
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
    }
}
