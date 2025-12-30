using TMPro;
using Turnroot.UI.Components.RadialMenu;
using UnityEngine;
using UnityEngine.UI;

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
            labelText.text = text ?? "Segment";
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
        if (iconImage != null)
            iconImage.gameObject.SetActive(showIcon);
        if (labelText != null)
            labelText.gameObject.SetActive(showLabel);
    }
}
