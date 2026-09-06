using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// Default implementation of radial menu content displaying an icon and label with visibility controls.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RadialMenuContent : MonoBehaviour, IRadialMenuContent
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TMP_Text labelText;

        public void SetLabel(string text)
        {
            if (labelText != null)
            {
                labelText.text = text ?? "Segment";
            }
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
        }

        public void ApplyVisibility(bool showIcon, bool showLabel)
        {
            iconImage?.gameObject.SetActive(showIcon);

            labelText?.gameObject.SetActive(showLabel);
        }
    }
}
