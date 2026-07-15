using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Combat;
using Turnroot.UI;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Helpers

        private static bool ValidateRequired(
            string context,
            params (object Value, string Name)[] values
        ) => ValidationHelper.ValidateNotNull(context, values);

        private static bool ValidateRequired(object value, string valueName, string context) =>
            ValidationHelper.ValidateNotNull(value, valueName, context);

        private static bool ValidateRequired(object value, string valueNameOrMessage) =>
            ValidationHelper.ValidateNotNull(value, valueNameOrMessage);

        private static bool ValidateRequiredNotNullOrEmpty<T>(
            T[] values,
            string valueName,
            string context
        ) => ValidationHelper.ValidateNotNullOrEmpty(values, valueName, context);

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
            var list = new List<UiChoice>(5);

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
            if (
                !ValidateRequired(
                    nameof(IsAnyForcedBattleAtDayLimit),
                    (AllGameBattlesTable.Instance, "AllGameBattlesTable.Instance")
                )
            )
            {
                return false;
            }

            if (
                !TryBuildAvailableBattleNames(
                    nameof(IsAnyForcedBattleAtDayLimit),
                    out var availableBattleNames
                )
            )
            {
                return false;
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

                var battleSceneName = battle.BattleScene.SceneName;
                if (!availableBattleNames.Contains(battleSceneName))
                {
                    continue;
                }

                int spent = GetForcedBattleDaysSpent(battleSceneName);
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
                !ValidateRequired(
                    nameof(IncrementForcedBattleDaysSpent),
                    (AllGameBattlesTable.Instance, "AllGameBattlesTable.Instance"),
                    (_brain, nameof(_brain)),
                    (_brain?.sceneFlowBrain, "_brain.sceneFlowBrain"),
                    (_brain?.ltm, "_brain.ltm")
                )
            )
            {
                return;
            }

            if (
                !TryBuildAvailableBattleNames(
                    nameof(IncrementForcedBattleDaysSpent),
                    out var availableBattleNames
                )
            )
            {
                return;
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

                var battleSceneName = battle.BattleScene.SceneName;
                if (!availableBattleNames.Contains(battleSceneName))
                {
                    continue;
                }

                int current = GetForcedBattleDaysSpent(battleSceneName);
                _brain.ltm.Remember(
                    ForcedBattleDaysLtmKeyPrefix + battleSceneName,
                    (current + 1).ToString()
                );
            }
        }

        private bool TryBuildAvailableBattleNames(
            string context,
            out HashSet<string> availableBattleNames
        )
        {
            availableBattleNames = new HashSet<string>();

            if (!ValidateRequired(context, (_brain?.sceneFlowBrain, "_brain.sceneFlowBrain")))
            {
                return false;
            }

            var availableScenes = _brain.sceneFlowBrain.GetAvailableScenes();
            if (!ValidateRequired(availableScenes, nameof(availableScenes), context))
            {
                return false;
            }

            var graph = _brain.sceneFlowBrain.sceneFlowGraph;
            if (!ValidateRequired(graph, nameof(graph), context))
            {
                return false;
            }

            var battleSceneNames = new HashSet<string>(
                graph.GetBattleScenes().Select(n => n.sceneName)
            );

            foreach (var opt in availableScenes)
            {
                if (battleSceneNames.Contains(opt.sceneName))
                {
                    availableBattleNames.Add(opt.sceneName);
                }
            }

            return true;
        }

        public void TransitionBackToHub(UIFade fadeToBlack = null, Vector3? returnPosition = null)
        {
            void DoReturn()
            {
                var brainValidation = OperationResultGuards.RequireNotNull(_brain, nameof(_brain));
                if (!brainValidation.Success)
                {
                    $"HubManager: TransitionBackToHub aborted. {brainValidation.ErrorMessage}".LogError();
                    return;
                }

                var allPoi = FindObjectsByType<HubPoiUi>(FindObjectsSortMode.None);
                foreach (var poi in allPoi)
                {
                    poi.Hide();
                }

                GetHubCharacterManager()?.HandleHubOverviewEntered();

                _brain.audioBrain.SetMusic(HubBackgroundMusic, fadeDuration: 1f);
                GeneralCamera.fieldOfView = HubMainFov;
                BackButtonFade.Hide();

                if (returnPosition.HasValue && _avatarRoot != null)
                {
                    _avatarRoot.transform.position = returnPosition.Value;
                }
                else if (GeneralCamera != null && cameraPoints != null && cameraPoints.Length > 0)
                {
                    int idx = UnityEngine.Random.Range(0, cameraPoints.Length);
                    Transform dest = cameraPoints[idx];
                    GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
                }
                else
                {
                    $"Nothing can be done".LogError("HugManager");
                }

                CurrentLocationName = null;
                CurrentLocationPoint = null;
                CurrentTraversalAvatarPoint = null;

                SetInputMode(HubInputMode.Location);
                UpdateChoiceSelection();
                UpdateDateText();
                HubActionsFade.Show();
                _brain?.charactersBrain.CheckBirthdays();
            }

            if (fadeToBlack == null)
            {
                DoReturn();
                return;
            }

            UnityAction onVisible = null;
            UnityAction onHidden = null;

            onVisible = () =>
            {
                fadeToBlack.OnVisible.RemoveListener(onVisible);
                DoReturn();
                fadeToBlack.Hide();
            };

            onHidden = () =>
            {
                fadeToBlack.OnHidden.RemoveListener(onHidden);
            };

            fadeToBlack.OnVisible.AddListener(onVisible);
            fadeToBlack.OnHidden.AddListener(onHidden);
            fadeToBlack.Show();
        }

        private int currentIndex = 0;

        private const string birthdayNotificationTypeName = "birthday";
        private const string shipNotificationTypeName = "ship";
        private const string itemNotificationTypeName = "items";

        public void UpdateChapterNumberAndNameText(int chapterNumber, string chapterName)
        {
            if (ChapterNumberAndNameText != null)
            {
                ChapterNumberAndNameText.text = string.Format(
                    ChapterNumberAndNameFormat,
                    chapterNumber,
                    chapterName
                );
            }
        }

        public SpecificUiHandler SpecificUiInputHandler => GetComponent<SpecificUiHandler>();

        private Character.HubCharacterManager GetHubCharacterManager() =>
            _hubCharacterManager =
                _hubCharacterManager != null
                    ? _hubCharacterManager
                    : FindFirstObjectByType<Character.HubCharacterManager>();

        #endregion
    }
}
