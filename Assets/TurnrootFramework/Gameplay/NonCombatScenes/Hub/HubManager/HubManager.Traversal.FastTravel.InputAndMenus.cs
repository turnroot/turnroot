using System;
using System.Collections;
using NaughtyAttributes;
using Turnroot.NonCombatScenes.Abstract;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        #region Fast Travel Input and Menus
        private bool TryHandleFastTravelInput(string action)
        {
            if (_isFastTravelInProgress)
            {
                return true;
            }

            if (!_fastTravelMenuOpen)
            {
                if (action == InputActionConstants.ToggleDetails)
                {
                    OpenFastTravelMenu();
                    return true;
                }

                return false;
            }

            if (action is InputActionConstants.Back or InputActionConstants.Cancel)
            {
                CloseFastTravelMenu();
                return true;
            }

            if (_fastTravelNavigableChoices == null || _fastTravelNavigableChoices.Length == 0)
            {
                return true;
            }

            UiChoiceHandler.HandleNavigation(
                action,
                _fastTravelNavigableChoices,
                ref _fastTravelChoiceIndex,
                _fastTravelNavigableChoices.Length,
                OnFastTravelChoiceSelected
            );

            UpdateFastTravelChoiceSelection();

            return true;
        }

        private void OpenFastTravelMenu()
        {
            if (_isFastTravelInProgress || _fastTravelMenuOpen)
            {
                return;
            }

            var optionsBuild = BuildFastTravelOptions();
            if (!optionsBuild.Success)
            {
                $"HubManager: Fast travel menu cannot open. {optionsBuild.ErrorMessage}".LogWarning();
                return;
            }

            _fastTravelMenuOpen = true;
            _fastTravelChoiceIndex = 0;

            FastTravelChoicesFade?.Show();
            UpdateFastTravelChoiceSelection();
        }

        private void CloseFastTravelMenu(bool force = false)
        {
            if (!_fastTravelMenuOpen && !force)
            {
                return;
            }

            _fastTravelMenuOpen = false;
            FastTravelChoicesFade?.Hide();
            DeselectAllFastTravelChoices();
        }

        #endregion
    }
}