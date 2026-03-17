using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Input Handling

        private void HandleInput(string action)
        {
            switch (CurrentInputMode)
            {
                case HubInputMode.Location:
                    HandleLocationInput(action);
                    break;
                case HubInputMode.MarketChoice:
                case HubInputMode.CafeChoice:
                case HubInputMode.Docks:
                case HubInputMode.Training:
                case HubInputMode.Battlefields:
                    SublocationInput.HandleSubLocationInput(action);
                    break;
                case HubInputMode.Chosen:
                    SpecificUiInputHandler.HandleInput(action);
                    break;
            }
        }

        public void SetInputMode(HubInputMode mode)
        {
            if (mode != CurrentInputMode)
            {
                PreviousInputMode = CurrentInputMode;
            }

            CurrentInputMode = mode;
            currentIndex = 0;

            bool allowLook = mode switch
            {
                HubInputMode.Location => false,
                HubInputMode.MarketChoice => true,
                HubInputMode.CafeChoice => true,
                HubInputMode.Battlefields => false,
                HubInputMode.Docks => true,
                HubInputMode.Training => true,
                HubInputMode.Chosen => false,
                HubInputMode.None => false,
                _ => false,
            };

            SublocationInput.SetLookEnabled(allowLook);
        }

        public void RevertToPreviousInputMode()
        {
            if (PreviousInputMode == CurrentInputMode)
            {
                return;
            }

            SetInputMode(PreviousInputMode);
        }

        private void IncrementGameDateForHubLoad()
        {
            if (_brain?.ltm == null)
            {
                return;
            }

            GameDate current = _brain.ltm.GetGameDate();
            var dt = new System.DateTime(current.year, current.month, current.day);
            dt = dt.AddDays(1);

            _brain.ltm.SetGameDate(dt.Year, (Month)(dt.Month - 1), dt.Day);
            gameDate = new GameDate(dt.Year, dt.Month, dt.Day);
        }

        #endregion
    }
}
