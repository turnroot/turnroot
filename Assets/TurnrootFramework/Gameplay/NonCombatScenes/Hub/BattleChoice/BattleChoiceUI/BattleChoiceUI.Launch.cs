using Turnroot.Gameplay.Combat;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class BattleChoiceUI
    {
        #region Battle Launch

        private void StartBattle(GameFlowRegistry.BattleEntry battle)
        {
            if (_brain?.sceneFlowBrain == null)
            {
                $"BattleChoiceUI: No SceneFlowBrain available to start battle '{battle.BattleName}'.".LogError();
                return;
            }

            _hubManager.LoadingScreen?.Show();
            _brain.sceneFlowBrain.GoToBattle(battle.BattleScene.SceneName);
        }

        #endregion

        #region Helpers

        private void ClearRewardItems()
        {
            foreach (var label in _rewardItemLabels)
            {
                if (label != null)
                {
                    Destroy(label);
                }
            }

            _rewardItemLabels.Clear();
        }

        private void UpdateRewardItems(GameFlowRegistry.BattleEntry battle)
        {
            ClearRewardItems();

            if (
                ItemsRewardContainer == null
                || ItemRewardLabelPrefab == null
                || battle.Rewards == null
            )
            {
                return;
            }

            foreach (var item in battle.Rewards)
            {
                if (item == null)
                {
                    continue;
                }

                var label = Instantiate(ItemRewardLabelPrefab, ItemsRewardContainer.transform);
                label.text = item.Name;

                _rewardItemLabels.Add(label.gameObject);
            }
        }

        #endregion
    }
}
