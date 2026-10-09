using System.Collections.Generic;
using System.Linq;
using TMPro;
using Turnroot.Gameplay.Combat;
using Turnroot.UI;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class BattleChoiceUI
    {
        #region List Building

        private void BuildChoiceList()
        {
            ClearChoiceList();

            if (_brain?.sceneFlowBrain == null)
            {
                "BattleChoiceUI: No SceneFlowBrain found in Brain.".LogError();
                return;
            }

            if (BattleUiChoicePrefab == null)
            {
                "BattleChoiceUI: BattleUiChoicePrefab is not assigned.".LogWarning();
                return;
            }

            if (ChoiceContainer == null)
            {
                "BattleChoiceUI: ChoiceContainer is not assigned.".LogWarning();
                return;
            }

            foreach (var battle in _brain.sceneFlowBrain.GetAvailableBattles())
            {
                _availableBattles.Add(battle);

                var instance = Instantiate(BattleUiChoicePrefab, ChoiceContainer.transform);
                var choice = instance.GetComponent<UiChoice>();

                var label = instance.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = battle.BattleName;
                }

                _battleChoices.Add(choice);
            }

            if (_battleChoices.Count == 0)
            {
                "BattleChoiceUI: No available battles to display. Check the Game Flow Registry's unlocked/required battles.".LogError();
                return;
            }

            _currentIndex = 0;
            UpdateChoiceSelection();
        }

        private void ClearChoiceList()
        {
            foreach (var choice in _battleChoices)
            {
                if (choice != null)
                {
                    Destroy(choice.gameObject);
                }
            }

            _battleChoices.Clear();
            _availableBattles.Clear();
            _currentIndex = 0;
            ClearRewardItems();
        }

        #endregion
    }
}
