using System.Collections.Generic;
using System.Linq;
using TMPro;
using Turnroot.Gameplay.Combat;
using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class BattleChoiceUI
    {
        #region List Building

        private void BuildChoiceList()
        {
            ClearChoiceList();

            if (AllGameBattlesTable.Instance == null)
            {
                "BattleChoiceUI: AllGameBattlesTable not found. Create one in a Resources folder.".LogWarning();
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

            var availableSceneNames = GetAvailableBattleSceneNames();

            foreach (var battle in AllGameBattlesTable.Instance.Battles)
            {
                if (battle.BattleScene == null || battle.BattleScene.IsEmpty)
                {
                    continue;
                }

                if (!availableSceneNames.Contains(battle.BattleScene.SceneName))
                {
                    continue;
                }

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
                "BattleChoiceUI: No available battles to display. Something is wrong with the Scene Flow Graph".LogError();
                return;
            }

            _currentIndex = 0;
            UpdateChoiceSelection();
        }

        private HashSet<string> GetAvailableBattleSceneNames()
        {
            var result = new HashSet<string>();

            if (_brain?.sceneFlowBrain == null)
            {
                "BattleChoiceUI: No SceneFlowBrain found in Brain.".LogError();
                return result;
            }

            var available = _brain.sceneFlowBrain.GetAvailableScenes();
            if (available == null)
            {
                "BattleChoiceUI: SceneFlowBrain returned null for available scenes. Something is wrong with the Scene Flow Graph.".LogError();
                return result;
            }

            var graph = _brain.sceneFlowBrain.sceneFlowGraph;
            if (graph == null)
            {
                "BattleChoiceUI: No scene flow graph found in Brain.".LogError();
                return result;
            }

            var battleSceneNames = new HashSet<string>(
                graph.GetBattleScenes().Select(n => n.sceneName)
            );

            if (battleSceneNames.Count == 0)
            {
                "BattleChoiceUI: No battle scenes found in the Scene Flow Graph.".LogError();
                return result;
            }

            foreach (var opt in available)
            {
                if (battleSceneNames.Contains(opt.sceneName))
                {
                    result.Add(opt.sceneName);
                }
            }

            return result;
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
