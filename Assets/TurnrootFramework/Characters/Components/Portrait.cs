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
        [SerializeField]
        private bool _spriteOverride;

        [SerializeField]
        private Sprite _overrideRuntimeSprite;

        [SerializeField]
        private Sprite _overrideSavedSprite;

        public bool SpriteOverride => _spriteOverride;
        public Sprite OverrideRuntimeSprite => _overrideRuntimeSprite;
        public Sprite OverrideSavedSprite => _overrideSavedSprite;

        public void SetOverrideSprites(
            bool enabled,
            Sprite runtimeSprite = null,
            Sprite savedSprite = null
        )
        {
            _spriteOverride = enabled;
            _overrideRuntimeSprite = runtimeSprite;
            _overrideSavedSprite = savedSprite;
        }

        public override void Render()
        {
            if (_spriteOverride)
            {
                SetRuntimeSprite(_overrideRuntimeSprite);
                SetSavedSprite(_overrideSavedSprite);
                return;
            }

            base.Render();
        }

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
