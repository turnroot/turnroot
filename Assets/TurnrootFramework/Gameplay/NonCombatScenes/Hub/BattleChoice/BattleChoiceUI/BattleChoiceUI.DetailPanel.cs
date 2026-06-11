using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Combat;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using static Turnroot.Gameplay.Brain.GamewideContextBrainHelpers;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class BattleChoiceUI
    {
        #region Selection & Detail Panel

        private void UpdateChoiceSelection()
        {
            for (int i = 0; i < _battleChoices.Count; i++)
            {
                if (_battleChoices[i] == null)
                {
                    continue;
                }

                if (i == _currentIndex)
                {
                    _battleChoices[i].Select();
                }
                else
                {
                    _battleChoices[i].Deselect();
                }
            }

            if (_currentIndex >= 0 && _currentIndex < _availableBattles.Count)
            {
                UpdateDetailPanel(_availableBattles[_currentIndex]);
            }
        }

        private void UpdateDetailPanel(AllGameBattlesTable.BattleEntry battle)
        {
            if (BattleName != null)
            {
                BattleName.text = battle.BattleName;
            }

            if (BattleDescription != null)
            {
                BattleDescription.text = battle.BattleDescription;
            }

            if (DifficultyImages != null)
            {
                for (int i = 0; i < DifficultyImages.Length; i++)
                {
                    if (DifficultyImages[i] == null)
                    {
                        continue;
                    }

                    DifficultyImages[i].sprite =
                        i < battle.BattleDifficulty
                            ? DifficultyActiveSprite
                            : DifficultyInactiveSprite;
                }
            }

            if (_currentIndex >= 0 && _currentIndex < _battleChoices.Count)
            {
                if (
                    _battleChoices[_currentIndex]
                        .TryGetComponent<BattleChoiceTypeDisplay>(out var typeDisplay)
                )
                {
                    typeDisplay.SetRequiredActive(battle.RequiredStoryBattle);
                    typeDisplay.SetParalogueActive(battle.ParalogueBattle);
                }
            }

            var bgColor =
                battle.RequiredStoryBattle ? RequiredBackgroundColor
                : battle.ParalogueBattle ? ParalogueBackgroundColor
                : NormalBackgroundColor;
            if (BackgroundImages != null)
            {
                foreach (var img in BackgroundImages)
                {
                    if (img != null)
                    {
                        img.color = bgColor;
                    }
                }
            }

            UpdateMapImages(battle);
            UpdateRewardItems(battle);
        }

        private void UpdateMapImages(AllGameBattlesTable.BattleEntry battle)
        {
            bool useUnexplored =
                GameplayGeneralSettings.Instance != null
                && GameplayGeneralSettings.Instance.UnexploredMaps;

            if (!useUnexplored)
            {
                if (MapImage != null)
                {
                    MapImage.gameObject.SetActive(true);
                    MapImage.sprite = battle.MapSprite;
                }

                if (MapQuadrantDisplay != null)
                {
                    MapQuadrantDisplay.gameObject.SetActive(false);
                }

                return;
            }

            // Unexplored maps: hide flat images, show quadrant blend display.

            if (MapImage != null)
            {
                MapImage.gameObject.SetActive(false);
            }

            if (MapQuadrantDisplay == null)
            {
                return;
            }

            MapQuadrantDisplay.gameObject.SetActive(true);

            var explorationStatus = GetExplorationStatus(battle);
            MapQuadrantDisplay.SetFromExplorationStatus(
                battle.MapExplorationSprites,
                explorationStatus
            );
        }

        private ExploredStatus GetExplorationStatus(AllGameBattlesTable.BattleEntry battle)
        {
            if (AllGameBattlesTable.Instance == null)
            {
                "BattleChoiceUI: AllGameBattlesTable not found. Create one in a Resources folder.".LogWarning();
                return default;
            }

            var ltm = _brain?.GetComponent<LongTermMemory>();
            return AllGameBattlesTable.Instance.Initialize(battle.BattleScene?.SceneName, ltm);
        }

        #endregion
    }
}
