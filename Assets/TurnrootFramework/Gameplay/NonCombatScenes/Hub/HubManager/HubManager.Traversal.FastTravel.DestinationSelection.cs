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
        #region Destination Selection

        private OperationResult BuildFastTravelOptions()
        {
            if (FastTravelLocations == null || FastTravelLocations.Length == 0)
            {
                _fastTravelNavigableChoices = Array.Empty<UiChoice>();
                _fastTravelNavigableOptions = Array.Empty<UnlockableWorldLocationOption>();
                return OperationResult.Failure("No FastTravelLocations are configured.");
            }

            _fastTravelManager ??= GetComponent<FastTravelManager>();

            int validCount = 0;
            for (int i = 0; i < FastTravelLocations.Length; i++)
            {
                var option = FastTravelLocations[i];
                option.available = IsFastTravelOptionAvailable(option);
                FastTravelLocations[i] = option;

                option.Choice?.gameObject.SetActive(option.available);

                if (option.Choice != null && option.available)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                _fastTravelNavigableChoices = Array.Empty<UiChoice>();
                _fastTravelNavigableOptions = Array.Empty<UnlockableWorldLocationOption>();
            }
            else
            {
                _fastTravelNavigableChoices = new UiChoice[validCount];
                _fastTravelNavigableOptions = new UnlockableWorldLocationOption[validCount];

                int writeIndex = 0;
                for (int i = 0; i < FastTravelLocations.Length; i++)
                {
                    var option = FastTravelLocations[i];
                    if (option.Choice == null || !option.available)
                    {
                        continue;
                    }

                    option.Choice.gameObject.SetActive(true);

                    _fastTravelNavigableChoices[writeIndex] = option.Choice;
                    _fastTravelNavigableOptions[writeIndex] = option;
                    writeIndex++;
                }
            }

            if (_fastTravelNavigableChoices.Length == 0)
            {
                return OperationResult.Failure(
                    "FastTravelOptions are configured, but none are both unlocked and mapped to a valid UiChoice."
                );
            }

            if (
                _fastTravelChoiceIndex < 0
                || _fastTravelChoiceIndex >= _fastTravelNavigableChoices.Length
            )
            {
                _fastTravelChoiceIndex = 0;
            }

            return OperationResult.Successful();
        }

        private bool IsFastTravelOptionAvailable(UnlockableWorldLocationOption option)
        {
            return option.UnlockableLocation != null
                && _fastTravelManager != null
                && _fastTravelManager.IsLocationAvailable(option.UnlockableLocation);
        }

        private void UpdateFastTravelChoiceSelection()
        {
            if (_fastTravelNavigableChoices == null)
            {
                return;
            }

            for (int i = 0; i < _fastTravelNavigableChoices.Length; i++)
            {
                var choice = _fastTravelNavigableChoices[i];
                if (choice == null)
                {
                    continue;
                }

                if (i == _fastTravelChoiceIndex)
                {
                    choice.Select();
                }
                else
                {
                    choice.Deselect();
                }
            }
        }

        private void DeselectAllFastTravelChoices()
        {
            if (_fastTravelNavigableChoices == null)
            {
                return;
            }

            for (int i = 0; i < _fastTravelNavigableChoices.Length; i++)
            {
                _fastTravelNavigableChoices[i]?.Deselect();
            }
        }

        private void OnFastTravelChoiceSelected()
        {
            if (
                _fastTravelNavigableOptions == null
                || _fastTravelChoiceIndex < 0
                || _fastTravelChoiceIndex >= _fastTravelNavigableOptions.Length
            )
            {
                return;
            }

            CloseFastTravelMenu();
            StartFastTravel(_fastTravelNavigableOptions[_fastTravelChoiceIndex]);
        }

        #endregion
    }
}