using System.Collections.Generic;
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
            var list = new List<UiChoice>();

            if (ExploreChoice != null)
            {
                AddChoiceIfMissing(list, ExploreChoice);
            }

            if (BattlefieldsChoice != null)
            {
                AddChoiceIfMissing(list, BattlefieldsChoice);
            }

            if (EndDay != null)
            {
                list.Add(EndDay);
            }

            if (Settings != null)
            {
                list.Add(Settings);
            }

            if (Exit != null)
            {
                list.Add(Exit);
            }

            _navigableChoices = list.ToArray();
        }

        private static void AddChoiceIfMissing(List<UiChoice> list, UiChoice choice)
        {
            var validation = ValidateChoiceForNavigation(list, choice);
            if (!validation.Success)
            {
                if (list == null || choice == null)
                {
                    $"HubManager: Failed to add navigation choice. {validation.ErrorMessage}".LogError();
                }

                return;
            }

            list.Add(choice);
        }

        private static OperationResult ValidateChoiceForNavigation(
            List<UiChoice> list,
            UiChoice choice
        )
        {
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(list, nameof(list)),
                OperationResultGuards.RequireNotNull(choice, nameof(choice))
            );
            return !validation.Success ? validation
                : list.Contains(choice)
                    ? OperationResult.Failure("Choice already exists in navigable list.")
                : OperationResult.Successful();
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

            var battleSceneNames = new HashSet<string>(
                graph.GetBattleScenes().Select(n => n.sceneName)
            );

            var availableBattleNames = new HashSet<string>();
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
            var validation = OperationResultGuards.All(
                OperationResultGuards.RequireNotNull(
                    AllGameBattlesTable.Instance,
                    "AllGameBattlesTable.Instance"
                ),
                OperationResultGuards.RequireNotNull(_brain, nameof(_brain)),
                OperationResultGuards.RequireNotNull(
                    _brain?.sceneFlowBrain,
                    "_brain.sceneFlowBrain"
                ),
                OperationResultGuards.RequireNotNull(_brain?.ltm, "_brain.ltm")
            );
            if (!validation.Success)
            {
                $"HubManager: Failed to increment forced-battle day counters. {validation.ErrorMessage}".LogError();
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

            var battleSceneNames = new HashSet<string>(
                graph.GetBattleScenes().Select(n => n.sceneName)
            );

            var availableBattleNames = new HashSet<string>();
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
