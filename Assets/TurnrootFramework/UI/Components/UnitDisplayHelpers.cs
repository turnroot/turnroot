using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public static class UnitDisplayHelpers
    {
        // Build display data (name, className, portrait) for UI from a runtime instance
        public static (string name, string className, Sprite portrait) FromInstance(
            CharacterInstance inst
        )
        {
            if (inst == null)
                return ("", "n/a", GamewideUiSettings.Instance.NoPortraitSprite);

            var name = inst.CharacterTemplate?.DisplayName ?? "";
            var className =
                inst.GetCurrentClass()?.ClassData?.Identity?.ClassName
                ?? inst.CharacterTemplate?.StartingClass?.Identity?.ClassName
                ?? "n/a";
            var portrait =
                inst.CharacterTemplate?.DefaultPortrait?.RuntimeSprite
                ?? GamewideUiSettings.Instance.NoPortraitSprite;
            return (name, className, portrait);
        }

        // Build display data from CharacterData (no runtime instance available)
        public static (string name, string className, Sprite portrait) FromCharacterData(
            Characters.CharacterData data
        )
        {
            if (data == null)
                return ("", "n/a", GamewideUiSettings.Instance.NoPortraitSprite);

            var name = data.DisplayName ?? "";
            var className = data.StartingClass?.Identity?.ClassName ?? "n/a";
            var portrait =
                data.DefaultPortrait?.RuntimeSprite ?? GamewideUiSettings.Instance.NoPortraitSprite;
            return (name, className, portrait);
        }
    }
}
