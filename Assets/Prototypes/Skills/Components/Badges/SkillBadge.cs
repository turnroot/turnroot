using System;
using Assets.Prototypes.Graphics2D;
using UnityEngine;

namespace Assets.Prototypes.Skills.Components.Badges
{
    [Serializable]
    public class SkillBadge : StackedImage<Skill>
    {
        protected override string GetSaveSubdirectory()
        {
            return "SkillBadges";
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
    }
}
