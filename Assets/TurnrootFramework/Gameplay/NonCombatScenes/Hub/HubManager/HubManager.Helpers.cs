using System.Linq;
using Turnroot.Gameplay.Combat;
using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Helpers

        private void CacheSpawnPointHeights()
        {
            _spawnPointHeights.Clear();
            if (SpawnGroundCollider == null)
            {
                return;
            }

            var discoveredLocations = FindObjectsByType<HubCharacterSpawnArea>(
                FindObjectsSortMode.None
            );
            if (discoveredLocations == null || discoveredLocations.Length == 0)
            {
                return;
            }

            foreach (var sub in discoveredLocations)
            {
                if (sub == null || sub.UnitSpawnPoints == null)
                {
                    continue;
                }

                foreach (var entry in sub.UnitSpawnPoints)
                {
                    var spawnPoint = entry.UnitSpawnPoint;
                    if (spawnPoint == null)
                    {
                        continue;
                    }

                    var origin = spawnPoint.position + Vector3.up * SpawnPointRaycastDistance;
                    var ray = new Ray(origin, Vector3.down);
                    if (
                        SpawnGroundCollider.Raycast(
                            ray,
                            out var hit,
                            SpawnPointRaycastDistance * 2f
                        )
                    )
                    {
                        _spawnPointHeights[spawnPoint] = hit.point.y;
                    }
                }
            }
        }

        public float GetSpawnPointHeight(Transform spawnPoint, float defaultHeight) =>
            spawnPoint == null ? defaultHeight
            : _spawnPointHeights.TryGetValue(spawnPoint, out var h) ? h
            : defaultHeight;

        public void UpdateDateText()
        {
            if (dateText != null)
            {
                Month month = (Month)Mathf.Clamp(gameDate.month - 1, 0, 11);
                string daySuffix = GameDate.GetDaySuffix(gameDate.day);
                string monthName = month.ToString();
                dateText.text = $"{monthName} {gameDate.day}{daySuffix}";
            }
        }

        private void BuildNavigableChoices()
        {
            var list = new System.Collections.Generic.List<UiChoice>();

            if (LocationChoices != null)
            {
                list.AddRange(LocationChoices);
            }

            if (ExploreChoice != null)
            {
                list.Add(ExploreChoice);
            }

            if (BattlefieldsChoice != null)
            {
                list.Add(BattlefieldsChoice);
            }

            if (EndDay != null)
            {
                list.Add(EndDay);
            }

            if (Settings != null)
            {
                list.Add(Settings);
            }

            _navigableChoices = list.ToArray();
        }

        private void UpdateChoiceSelection()
        {
            if (_navigableChoices == null || _navigableChoices.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _navigableChoices.Length; i++)
            {
                if (_navigableChoices[i] == null)
                {
                    continue;
                }

                if (i == currentIndex)
                {
                    _navigableChoices[i].Select();
                }
                else
                {
                    _navigableChoices[i].Deselect();
                }
            }
        }

        #endregion

        #region Forced Battle Day Limit

        private const string ForcedBattleDaysLtmKeyPrefix = "Hub_ForcedBattleDays_";

        /// <summary>
        /// Updates EndDay.CanBeSelected based on whether any available required-story battle
        /// has a MaxHubDaysBeforeBattle limit that has been reached.
        /// </summary>
        public void RefreshEndDayAvailability()
        {
            if (EndDay == null)
            {
                return;
            }

            bool forced = IsAnyForcedBattleAtDayLimit();
            EndDay.CanBeSelected = !forced;
            ForcedBattleIndicator?.SetActive(forced);
        }

        /// <summary>
        /// Returns true if any currently-available required battle has a day limit set and
        /// the player has already spent that many hub days without entering it.
        /// </summary>
        private bool IsAnyForcedBattleAtDayLimit()
        {
            if (AllGameBattlesTable.Instance == null || _brain?.sceneFlowBrain == null)
            {
                return false;
            }

            var availableScenes = _brain.sceneFlowBrain.GetAvailableScenes();
            if (availableScenes == null)
            {
                return false;
            }

            var graph = _brain.sceneFlowBrain.sceneFlowGraph;
            if (graph == null)
            {
                return false;
            }

            var battleSceneNames = new System.Collections.Generic.HashSet<string>(
                graph.GetBattleScenes().Select(n => n.sceneName)
            );

            var availableBattleNames = new System.Collections.Generic.HashSet<string>();
            foreach (var opt in availableScenes)
            {
                if (battleSceneNames.Contains(opt.sceneName))
                {
                    availableBattleNames.Add(opt.sceneName);
                }
            }

            foreach (var battle in AllGameBattlesTable.Instance.Battles)
            {
                if (!battle.RequiredStoryBattle || battle.MaxHubDaysBeforeBattle <= 0)
                {
                    continue;
                }

                if (battle.BattleScene == null || battle.BattleScene.IsEmpty)
                {
                    continue;
                }

                if (!availableBattleNames.Contains(battle.BattleScene.SceneName))
                {
                    continue;
                }

                int spent = GetForcedBattleDaysSpent(battle.BattleScene.SceneName);
                if (spent >= battle.MaxHubDaysBeforeBattle)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetForcedBattleDaysSpent(string sceneName)
        {
            var raw = _brain?.ltm?.Recall(ForcedBattleDaysLtmKeyPrefix + sceneName);
            return int.TryParse(raw, out var val) ? val : 0;
        }

        /// <summary>
        /// Increments the hub-days-spent counter in LTM for every currently-available
        /// required battle that has a MaxHubDaysBeforeBattle limit set.
        /// Call this just before transitioning away via End Day.
        /// </summary>
        public void IncrementForcedBattleDaysSpent()
        {
            if (
                AllGameBattlesTable.Instance == null || _brain != null ? _brain.sceneFlowBrain
                : null == null || _brain != null ? _brain.ltm
                : null == null
            )
            {
                return;
            }

            var availableScenes = _brain.sceneFlowBrain.GetAvailableScenes();
            if (availableScenes == null)
            {
                return;
            }

            var graph = _brain.sceneFlowBrain.sceneFlowGraph;
            if (graph == null)
            {
                return;
            }

            var battleSceneNames = new System.Collections.Generic.HashSet<string>(
                graph.GetBattleScenes().Select(n => n.sceneName)
            );

            var availableBattleNames = new System.Collections.Generic.HashSet<string>();
            foreach (var opt in availableScenes)
            {
                if (battleSceneNames.Contains(opt.sceneName))
                {
                    availableBattleNames.Add(opt.sceneName);
                }
            }

            foreach (var battle in AllGameBattlesTable.Instance.Battles)
            {
                if (!battle.RequiredStoryBattle || battle.MaxHubDaysBeforeBattle <= 0)
                {
                    continue;
                }

                if (battle.BattleScene == null || battle.BattleScene.IsEmpty)
                {
                    continue;
                }

                if (!availableBattleNames.Contains(battle.BattleScene.SceneName))
                {
                    continue;
                }

                int current = GetForcedBattleDaysSpent(battle.BattleScene.SceneName);
                _brain.ltm.Remember(
                    ForcedBattleDaysLtmKeyPrefix + battle.BattleScene.SceneName,
                    (current + 1).ToString()
                );
            }
        }

        #endregion
    }
}
