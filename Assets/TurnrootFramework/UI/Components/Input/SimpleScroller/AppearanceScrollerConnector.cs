using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI
{
    public enum ScrollerType
    {
        HairColor,
        EyeColor,
        SkinColor,
        HairStyle,
        Voice,
        FacialAccessory,
        Outfit,
    }

    [RequireComponent(typeof(UiScroller))]
    public class AppearanceScrollerConnector : MonoBehaviour
    {
        public ScrollerType scrollerType;

        public bool StartsSelected = false;

        [InfoBox("Only applies to hair color, eye color, and skin color")]
        public UnityEngine.UI.Image ColorPreviewImage;

        public UiScroller scroller { get; private set; }

        private Dictionary<string, Color> colorDict = new();

        private void Awake()
        {
            scroller = GetComponent<UiScroller>();
            switch (scrollerType)
            {
                case ScrollerType.HairColor:
                    ParseColorList(
                        GameplayGeneralSettings.Instance.AvatarHairColorChoices,
                        "Hair Color"
                    );
                    break;
                case ScrollerType.EyeColor:
                    ParseColorList(
                        GameplayGeneralSettings.Instance.AvatarEyeColorChoices,
                        "Eye Color"
                    );
                    break;

                case ScrollerType.SkinColor:
                    ParseColorList(
                        GameplayGeneralSettings.Instance.AvatarSkinColorChoices,
                        "Skin Color"
                    );
                    break;

                // TODO: add data for these
                case ScrollerType.HairStyle:
                    break;
                case ScrollerType.Voice:
                    break;
                case ScrollerType.FacialAccessory:
                    break;
                case ScrollerType.Outfit:
                    break;
            }
        }

        private void ParseColorList(Color[] colorList, string prefix)
        {
            var choices = new string[colorList.Length];
            for (int i = 0; i < colorList.Length; i++)
            {
                colorDict.Add(prefix + " " + i.ToString(), colorList[i]);
                choices[i] = prefix + " " + i.ToString();
            }
            scroller.SetChoices(choices, selected: StartsSelected);
            ColorPreviewImage.color = colorList[0];
        }

        public Color GetColorFromChoice(string choice)
        {
            if (colorDict.TryGetValue(choice, out Color color))
            {
                return color;
            }
            else
            {
                $"Color not found: {choice}".LogError();
                return Color.white;
            }
        }

        public void SetPreviewColor()
        {
            if (ColorPreviewImage == null)
            {
                return;
            }

            ColorPreviewImage.color = GetColorFromChoice(scroller.SelectedChoice);
        }
    }
}
