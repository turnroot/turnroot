using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Input Handling

        private void HandleInput(string action)
        {
            if (SpecificUiInputHandler.ActiveTutorialHandler != null)
            {
                SpecificUiInputHandler.HandleInput(action);
                return;
            }

            switch (CurrentInputMode)
            {
                case HubInputMode.Location:
                    HandleLocationInput(action);
                    break;
                case HubInputMode.MarketChoice:
                case HubInputMode.Docks:
                case HubInputMode.Training:
                case HubInputMode.Battlefields:
                case HubInputMode.ExploreMisc:
                    if (CurrentSubLocation == null || CurrentSubLocation.AcceptingInput)
                    {
                        SublocationInput.HandleSubLocationInput(action);
                    }
                    break;
                case HubInputMode.ExploreMenu:
                    HandleExploreMenuInput(action);
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
                HubInputMode.Battlefields => true,
                HubInputMode.Docks => true,
                HubInputMode.Training => true,
                HubInputMode.ExploreMisc => true,
                HubInputMode.ExploreMenu => false,
                HubInputMode.Chosen => false,
                HubInputMode.None => false,
                _ => false,
            };

            SublocationInput.SetLookEnabled(allowLook);
        }

        private void HandleExploreMenuInput(string action)
        {
            // All input is forwarded exclusively to the carousel while the explore menu is open.
            // The carousel handles navigation, confirm, and back — nothing else receives input.
            if (ExploreCarousel != null)
            {
                ExploreCarousel.HandleInput(action);
                return;
            }

            // Fallback (no carousel assigned): at least handle back.
            if (action is InputActionConstants.Cancel or "Back")
            {
                BackFromExploreMenu();
            }
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
