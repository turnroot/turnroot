using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components
{
    public class UnitCellDataOnly : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI NameText;
        public TMPro.TextMeshProUGUI ClassText;
        public Image Portrait;

        public void ClearData()
        {
            NameText.text = "";
            ClassText.text = "";
            Portrait.sprite = GamewideUiSettings.Instance.NoPortraitSprite;
        }

        public void SetData(string unitName, string className, Sprite portraitSprite)
        {
            NameText.text = unitName;
            ClassText.text = className;
            Portrait.sprite =
                portraitSprite != null
                    ? portraitSprite
                    : GamewideUiSettings.Instance.NoPortraitSprite;
        }
    }
}
