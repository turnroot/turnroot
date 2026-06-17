using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI
{
    public class AppearanceScrollerHandler : MonoBehaviour
    {
        public int AppearanceIndex { get; private set; } = 0;
        public AppearanceScrollerConnector[] AppearanceScrollers;
        public string SelectedHairColor { get; private set; }
        public string SelectedEyeColor { get; private set; }
        public string SelectedSkinColor { get; private set; }
        public string SelectedHairStyle { get; private set; }
        public string SelectedVoice { get; private set; }
        public string SelectedFacialAccessory { get; private set; }
        public string SelectedOutfit { get; private set; }

        public void Initialize()
        {
            foreach (var scroller in AppearanceScrollers)
            {
                switch (scroller.scrollerType)
                {
                    case ScrollerType.EyeColor:
                        scroller.scroller.OnChange += color =>
                        {
                            var colorValue = scroller.GetColorFromChoice(color);
                            SelectedEyeColor = color;
                        };
                        break;
                    case ScrollerType.HairColor:
                        scroller.scroller.OnChange += color =>
                        {
                            var colorValue = scroller.GetColorFromChoice(color);
                            SelectedHairColor = color;
                        };
                        break;
                    case ScrollerType.SkinColor:
                        scroller.scroller.OnChange += color =>
                        {
                            var colorValue = scroller.GetColorFromChoice(color);
                            SelectedSkinColor = color;
                        };
                        break;
                    case ScrollerType.HairStyle:
                        scroller.scroller.OnChange += style =>
                        {
                            SelectedHairStyle = style;
                        };
                        break;
                    case ScrollerType.Voice:
                        scroller.scroller.OnChange += voice =>
                        {
                            SelectedVoice = voice;
                        };
                        break;
                    case ScrollerType.FacialAccessory:
                        scroller.scroller.OnChange += accessory =>
                        {
                            SelectedFacialAccessory = accessory;
                        };
                        break;
                    case ScrollerType.Outfit:
                        scroller.scroller.OnChange += outfit =>
                        {
                            SelectedOutfit = outfit;
                        };
                        break;
                }
            }
        }

        public bool HandleAppearanceInput(string action)
        {
            switch (action)
            {
                case InputActionConstants.Cancel:
                case InputActionConstants.NavigateUp:
                case InputActionConstants.Back:
                    AppearanceIndex =
                        (AppearanceIndex - 1 + AppearanceScrollers.Length)
                        % AppearanceScrollers.Length;
                    for (int i = 0; i < AppearanceScrollers.Length; i++)
                    {
                        AppearanceScrollers[i].scroller.Deselect();
                    }
                    AppearanceScrollers[AppearanceIndex].SetPreviewColor();
                    AppearanceScrollers[AppearanceIndex].scroller.Select();
                    break;
                case InputActionConstants.NavigateDown:
                    AppearanceIndex = (AppearanceIndex + 1) % AppearanceScrollers.Length;
                    for (int i = 0; i < AppearanceScrollers.Length; i++)
                    {
                        AppearanceScrollers[i].scroller.Deselect();
                    }
                    AppearanceScrollers[AppearanceIndex].SetPreviewColor();
                    AppearanceScrollers[AppearanceIndex].scroller.Select();
                    break;
                case InputActionConstants.NavigateLeft:
                    AppearanceScrollers[AppearanceIndex].scroller.ScrollLeft();
                    AppearanceScrollers[AppearanceIndex].SetPreviewColor();
                    break;
                case InputActionConstants.NavigateRight:
                    AppearanceScrollers[AppearanceIndex].scroller.ScrollRight();
                    AppearanceScrollers[AppearanceIndex].SetPreviewColor();
                    break;
                case InputActionConstants.Submit:
                case InputActionConstants.Confirm:
                case InputActionConstants.Select:
                    if (AppearanceIndex == AppearanceScrollers.Length - 1)
                    {
                        return true;
                    }
                    else
                    {
                        AppearanceIndex = (AppearanceIndex + 1) % AppearanceScrollers.Length;
                        for (int i = 0; i < AppearanceScrollers.Length; i++)
                        {
                            AppearanceScrollers[i].scroller.Deselect();
                        }
                        AppearanceScrollers[AppearanceIndex].SetPreviewColor();
                        AppearanceScrollers[AppearanceIndex].scroller.Select();
                    }
                    break;
            }
            return false;
        }
    }
}
