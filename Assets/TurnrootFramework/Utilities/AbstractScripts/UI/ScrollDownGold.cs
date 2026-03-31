using Turnroot.GameSettings;

namespace Turnroot.Utilities.Ui
{
    public class ScrollDownGold : ScrollDownNumber
    {
        public override string Suffix =>
            GameplayGeneralSettings.Instance.GoldDisplayNames.OneLetter;
    }
}
