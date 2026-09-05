using Turnroot.UI;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class BattleChoiceUI
    {
        #region Input

        private void HandleInput(string action)
        {
            if (_confirmPopupActive)
            {
                HandleConfirmPopupInput(action);
                return;
            }

            if (action is InputActionConstants.Cancel or InputActionConstants.Back)
            {
                _hubManager.BackFromBattleChoice();
                return;
            }

            if (_battleChoices.Count == 0)
            {
                return;
            }

            UiChoiceHandler.HandleNavigation(
                action,
                _battleChoices.ToArray(),
                ref _currentIndex,
                _battleChoices.Count,
                ShowConfirmPopup
            );

            if (action is InputActionConstants.NavigateUp or InputActionConstants.NavigateDown)
            {
                SfxAudio?.PlayOneShot(NavigateClip);
            }

            UpdateChoiceSelection();
        }

        private void ShowConfirmPopup()
        {
            if (_currentIndex < 0 || _currentIndex >= _availableBattles.Count)
            {
                return;
            }

            SfxAudio?.PlayOneShot(SelectClip);
            _confirmPopupActive = true;
            _confirmPopupIndex = 0;
            ConfirmPopupFade?.Show();
            UpdateConfirmPopupSelection();
        }

        private void HandleConfirmPopupInput(string action)
        {
            if (action == InputActionConstants.Cancel)
            {
                CloseConfirmPopup();
                return;
            }

            var choices = new[] { ConfirmChoice, CancelChoice };

            UiChoiceHandler.HandleNavigation(
                action,
                choices,
                ref _confirmPopupIndex,
                choices.Length,
                OnConfirmPopupSelect
            );

            UpdateConfirmPopupSelection();
        }

        private void OnConfirmPopupSelect()
        {
            if (_confirmPopupIndex == 0)
            {
                SfxAudio?.PlayOneShot(SelectClip);
                StartBattle(_availableBattles[_currentIndex]);
            }
            else
            {
                CloseConfirmPopup();
            }
        }

        private void CloseConfirmPopup(bool silent = false)
        {
            if (!_confirmPopupActive && !silent)
            {
                return;
            }

            _confirmPopupActive = false;
            ConfirmPopupFade?.Hide();
            UpdateChoiceSelection();
        }

        private void UpdateConfirmPopupSelection()
        {
            if (_confirmPopupIndex == 0)
            {
                ConfirmChoice?.Select();
                CancelChoice?.Deselect();
            }
            else
            {
                ConfirmChoice?.Deselect();
                CancelChoice?.Select();
            }
        }

        #endregion
    }
}
