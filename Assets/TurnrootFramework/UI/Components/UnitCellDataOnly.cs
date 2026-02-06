using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Displays basic unit information including name, class, and portrait in a UI cell.
    /// </summary>
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
            Portrait.sprite = portraitSprite ?? GamewideUiSettings.Instance.NoPortraitSprite;
        }
    }
}
