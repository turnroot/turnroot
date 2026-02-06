using System;
using Turnroot.Graphics2D;
using UnityEngine;

namespace Turnroot.Skills.Components.Badges
{
    /// <summary>
    /// A visual badge for skills that uses stacked images with accent colors derived from the skill's properties.
    /// </summary>
    [Serializable]
    public class SkillBadge : StackedImage<Skill>
    {
        protected override string GetSaveSubdirectory() => "SkillBadges";

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
    }
}
