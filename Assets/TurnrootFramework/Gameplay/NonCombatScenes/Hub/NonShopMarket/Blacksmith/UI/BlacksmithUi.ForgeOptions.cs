using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Blacksmith
{
    public partial class BlacksmithUi : MonoBehaviour
    {
        private bool _inForgeOptionSelection;
        private BlacksmithForgeableItem _activeForgeEntry;
        private List<UiChoice> _forgeOptionChoices;
        private List<GameObject> _forgeOptionObjects;
        private int _forgeOptionCurrentIndex;
        private ForgeOption[] _activeForgeOptions;

        public void EnterForgeOptionSelection(BlacksmithForgeableItem entry)
        {
            _activeForgeEntry = entry;
            _inForgeOptionSelection = true;
            _forgeOptionCurrentIndex = 0;

            BuildForgeOptionList(entry);

            ForgeOptionsPanel?.SetActive(true);
        }

        public void ExitForgeOptionSelection()
        {
            _inForgeOptionSelection = false;
            ClearForgeOptionObjects();
            ForgeOptionsPanel?.SetActive(false);
            UpdateCurrentItemUiWithSelectionCount();
        }

        private void BuildForgeOptionList(BlacksmithForgeableItem entry)
        {
            ClearForgeOptionObjects();
            _forgeOptionChoices = new List<UiChoice>();
            _forgeOptionObjects = new List<GameObject>();

            var itemInstance = entry.ItemToForge;
            if (itemInstance == null)
            {
                "BlacksmithUi.BuildForgeOptionList: ItemToForge is null".LogWarning("BlacksmithUi");
                return;
            }

            var template = itemInstance.Template;
            _activeForgeOptions = template?.ForgeOptions;

            if (_activeForgeOptions == null || _activeForgeOptions.Length == 0)
            {
                "BlacksmithUi.BuildForgeOptionList: No forge options on item".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var storehouse = brain?.storehouseBrain;
            var forger = itemInstance.Forger;
            forger?.GetForgeOptions();

            for (var i = 0; i < _activeForgeOptions.Length; i++)
            {
                if (ForgeOptionPrefab == null || ForgeOptionsListContainer == null)
                {
                    "BlacksmithUi.BuildForgeOptionList: ForgeOptionPrefab or ForgeOptionsListContainer is null".LogWarning(
                        "BlacksmithUi"
                    );
                    break;
                }

                var optionObject = Instantiate(
                    ForgeOptionPrefab,
                    ForgeOptionsListContainer.transform
                );
                if (optionObject == null)
                {
                    continue;
                }

                _forgeOptionObjects.Add(optionObject);

                if (!optionObject.TryGetComponent<UiChoice>(out var uiChoice))
                {
                    uiChoice = optionObject.AddComponent<UiChoice>();
                }

                optionObject.TryGetComponent<BlacksmithForgeOptionRefs>(out var refs);

                var option = _activeForgeOptions[i];
                bool canAfford =
                    forger != null
                    && storehouse != null
                    && forger.CanForge(storehouse, option).Success;

                ConfigureForgeOptionRowUi(option, refs, canAfford);

                uiChoice.CanBeSelected = canAfford;
                _forgeOptionChoices.Add(uiChoice);
            }

            _forgeOptionCurrentIndex = 0;
            UpdateForgeOptionHighlight();
        }

        private void ConfigureForgeOptionRowUi(
            ForgeOption option,
            BlacksmithForgeOptionRefs refs,
            bool canAfford
        )
        {
            if (refs == null)
            {
                return;
            }

            var forgeInto = option.ForgeInto;

            if (refs.ForgeIntoIcon != null)
            {
                refs.ForgeIntoIcon.sprite = forgeInto?.InventoryIcon;
            }

            if (refs.ForgeIntoNameText != null)
            {
                refs.ForgeIntoNameText.text = forgeInto?.Name ?? "Unknown";
                refs.ForgeIntoNameText.color = canAfford ? Color.white : Color.grey;
            }

            if (refs.DescriptionText != null)
            {
                refs.DescriptionText.text = forgeInto?.FlavorText ?? string.Empty;
            }

            if (refs.UsesText != null)
            {
                refs.UsesText.text =
                    forgeInto != null && forgeInto.Durability
                        ? $"Uses: {forgeInto.MaxUses}"
                        : string.Empty;
            }

            if (refs.GoldCostText != null)
            {
                refs.GoldCostText.text = $"{option.Price}G";
            }

            if (option.Item != null)
            {
                if (refs.MaterialIcon != null)
                {
                    refs.MaterialIcon.sprite = option.Item.InventoryIcon;
                }

                if (refs.MaterialNameText != null)
                {
                    refs.MaterialNameText.text = option.Item.Name;
                }

                if (refs.MaterialAmountText != null)
                {
                    refs.MaterialAmountText.text = $"x{option.ItemAmount}";
                }
            }
            else
            {
                if (refs.MaterialIcon != null)
                {
                    refs.MaterialIcon.sprite = null;
                }

                if (refs.MaterialNameText != null)
                {
                    refs.MaterialNameText.text = "--";
                }

                if (refs.MaterialAmountText != null)
                {
                    refs.MaterialAmountText.text = "--";
                }
            }
        }

        public void NavigateForgeOptions(string action)
        {
            if (_forgeOptionChoices == null || _forgeOptionChoices.Count == 0)
            {
                return;
            }

            int delta = 0;
            if (action == InputActionConstants.NavigateUp)
            {
                delta = -1;
            }
            else if (action == InputActionConstants.NavigateDown)
            {
                delta = 1;
            }

            if (delta == 0)
            {
                return;
            }

            _forgeOptionCurrentIndex = Mathf.Clamp(
                _forgeOptionCurrentIndex + delta,
                0,
                _forgeOptionChoices.Count - 1
            );

            UpdateForgeOptionHighlight();
            AudioPlayer?.PlayOneShot(NavigateAudioClip);
        }

        private void UpdateForgeOptionHighlight()
        {
            if (_forgeOptionChoices == null)
            {
                return;
            }

            for (var i = 0; i < _forgeOptionChoices.Count; i++)
            {
                var choice = _forgeOptionChoices[i];
                if (choice == null)
                {
                    continue;
                }

                if (i == _forgeOptionCurrentIndex)
                {
                    choice.Select();
                }
                else
                {
                    choice.Deselect();
                }
            }
        }

        public void ExecuteSelectedForgeOption()
        {
            if (
                _activeForgeOptions == null
                || _forgeOptionCurrentIndex < 0
                || _forgeOptionCurrentIndex >= _activeForgeOptions.Length
            )
            {
                "BlacksmithUi.ExecuteSelectedForgeOption: Invalid option index".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var option = _activeForgeOptions[_forgeOptionCurrentIndex];
            var itemInstance = _activeForgeEntry.ItemToForge;
            if (itemInstance == null)
            {
                "BlacksmithUi.ExecuteSelectedForgeOption: ItemToForge is null".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var storehouse = brain?.storehouseBrain;
            if (storehouse == null)
            {
                "BlacksmithUi.ExecuteSelectedForgeOption: Missing StorehouseBrain".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            var forger = itemInstance.Forger;
            if (forger == null)
            {
                "BlacksmithUi.ExecuteSelectedForgeOption: Item has no Forger".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            forger.GetForgeOptions();

            var canForgeResult = forger.CanForge(storehouse, option);
            if (!canForgeResult.Success)
            {
                $"BlacksmithUi.ExecuteSelectedForgeOption: Cannot forge: {canForgeResult.ErrorMessage}".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            BeginGoldScroll(option.Price);

            var forgeResult = forger.ForgeItem(storehouse, option);
            if (!forgeResult.Success)
            {
                $"BlacksmithUi.ExecuteSelectedForgeOption: Forge failed: {forgeResult.ErrorMessage}".LogWarning(
                    "BlacksmithUi"
                );
                return;
            }

            FinalizeTransaction(storehouse);
            ExitForgeOptionSelection();
            RefreshBlacksmithDisplay();
        }

        private void ClearForgeOptionObjects()
        {
            if (_forgeOptionObjects != null)
            {
                foreach (var obj in _forgeOptionObjects)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }

                _forgeOptionObjects = null;
            }

            _forgeOptionChoices = null;
            _activeForgeOptions = null;
        }

        /// <summary>
        /// Called by Blacksmith.HandleBackInput. Returns true if back was consumed
        /// (i.e., exiting the forge option sub-panel), false if it should propagate.
        /// </summary>
        public bool TryHandleBack(string action)
        {
            if (action is not "Back" and not InputActionConstants.Cancel)
            {
                return false;
            }

            if (_inForgeOptionSelection)
            {
                ExitForgeOptionSelection();
                return true;
            }

            return false;
        }
    }
}
