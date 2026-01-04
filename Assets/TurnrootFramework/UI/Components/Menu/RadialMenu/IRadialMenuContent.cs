using UnityEngine;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// Implement this on the root of your radial menu content prefab.
    /// Prefab should show an icon (Image) and/or a label (TMP or Text) and be able to hide them.
    /// </summary>
    public interface IRadialMenuContent
    {
        void SetLabel(string text);
        void SetIcon(Sprite icon);
        void ApplyVisibility(bool showIcon, bool showLabel);
    }
}
