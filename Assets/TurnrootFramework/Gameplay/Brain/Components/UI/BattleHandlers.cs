using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Partial class containing battle UI event handlers and setup logic.
    /// </summary>
    public partial class UiBrain : BrainComponent
    {
        public void HandleBattleUi() => TurnrootLogger.Log("UiBrain: Handling battle UI setup");
    }
}
