using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Utilities.SceneFlows
{
    /// <summary>
    /// Brain component that manages scene flow through a graph-based network system.
    /// Handles scene transitions, navigation history, and condition evaluation.
    /// Provides UnityEvent-compatible methods for inspector-based scene flow triggers.
    /// </summary>
    public partial class SceneFlowBrain : BrainComponent
    {
        private static WaitForSeconds _waitForSeconds0_3 = new WaitForSeconds(0.3f);

        // reference to storage system for dates/etc.
        private LongTermMemory _ltm;

        [HideInInspector]
        public SceneFlowGraph sceneFlowGraph; // this is auto-set

        [SerializeField, HideInInspector]
        private SceneNode _currentScene;
        private Stack<string> _sceneHistory = new();

        [SerializeField]
        private Dictionary<string, bool> _customFlags = new();

        [SerializeField]
        private Dictionary<string, int> _customIntValues = new();

        [SerializeField]
        private Dictionary<string, string> _customStringValues = new();

        // Condition evaluator instance
        private SceneFlowConditionEvaluatorImpl _conditionEvaluator;

        // The Brain scene name (matches BrainLoader constant)
        private const string BrainSceneName = "TurnrootBrain";

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        protected override void Awake()
        {
            base.Awake();
            _conditionEvaluator = new SceneFlowConditionEvaluatorImpl(this);

            _ltm = GetComponent<LongTermMemory>();
            if (_ltm != null && _ltm.Initialized)
            {
                // LTM already ready, replicate logic from OnLtmInitialized
                var existing = _ltm.GetGameDate();
                if (existing.year == 0)
                {
                    // write user-configurable starting date
                    var start =
                        GameplayGeneralSettings.Instance?.StartingGameDate ?? GameDate.Default;
                    _ltm.SetGameDate(start.year, (Month)(start.month - 1), start.day);
                    existing = _ltm.GetGameDate();
                }
                _brain?.PublishGameDateChanged(existing.year, existing.month, existing.day);
            }

            if (_brain != null)
            {
                _brain.OnLongTermMemoryInitialized += OnLtmInitialized;
            }

            if (sceneFlowGraph == null)
            {
                // scan resources, it's a singleton SO so there can only be one
                sceneFlowGraph = Resources.Load<SceneFlowGraph>("SceneFlowGraph");
                if (sceneFlowGraph == null)
                {
                    "SceneFlowBrain: No SceneFlowGraph assigned or found in Resources!".LogError();
                }
            }
        }

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to any brain events relevant to scene flow
            // For example, battle completion, story progression, etc.
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // Unsubscribe from brain events
            if (_brain != null)
            {
                _brain.OnLongTermMemoryInitialized -= OnLtmInitialized;
            }
        }

        #region Current Scene & History

        private void OnLtmInitialized()
        {
            if (_ltm != null)
            {
                var date = _ltm.GetGameDate();
                if (date.year == 0)
                {
                    var start =
                        GameplayGeneralSettings.Instance?.StartingGameDate ?? GameDate.Default;
                    _ltm.SetGameDate(start.year, (Month)(start.month - 1), start.day);
                    date = _ltm.GetGameDate();
                }
                _brain.PublishGameDateChanged(date.year, date.month, date.day);
            }
        }

        public SceneNode CurrentScene => _currentScene;

        public string CurrentSceneId => _currentScene?.id;

        public string CurrentSceneName => _currentScene?.sceneName;

        public bool CanGoBack => _sceneHistory.Count > 0;

        /// <summary>
        /// Set the current scene (typically called after a scene loads).
        /// </summary>
        public void SetCurrentScene(string sceneId)
        {
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            var scene = sceneFlowGraph.GetScene(sceneId);
            if (scene == null)
            {
                $"SceneFlowBrain: Scene '{sceneId}' not found in graph!".LogError();
                return;
            }

            _currentScene = scene;

            // update stored game date based on scene metadata
            if (scene.TimePasses && _ltm != null && _ltm.Initialized)
            {
                ApplySceneDateToLtm(scene);
            }

            // Reset the end-of-day transition flags when we arrive at a hub scene
            if (scene.isHub)
            {
                SetCustomFlag(SceneFlowConditionKeys.ReturnToHub, false);
                SetCustomFlag(SceneFlowConditionKeys.EndHubDay, false);
            }

            if (scene.SpecificChapter)
            {
                Brain.PublishSetSaveFileChapter(scene.ChapterName, scene.ChapterNumber);
            }

            Brain.PublishSceneChanged(scene.sceneName, scene.displayName);
        }

        /// <summary>
        /// Set current scene by scene name instead of ID.
        /// </summary>
        public void SetCurrentSceneByName(string sceneName)
        {
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            var scene = sceneFlowGraph.GetSceneByName(sceneName);
            if (scene == null)
            {
                $"SceneFlowBrain: Scene with name '{sceneName}' not found in graph!".LogError();
                return;
            }

            _currentScene = scene;

            if (scene.TimePasses && _ltm != null)
            {
                ApplySceneDateToLtm(scene);
            }

            if (scene.SpecificChapter)
            {
                Brain.PublishSetSaveFileChapter(scene.ChapterName, scene.ChapterNumber);
            }

            Brain.PublishSceneChanged(scene.sceneName, scene.displayName);
        }

        #endregion

        #region Date Helpers

        /// <summary>
        /// Applies the date metadata from <paramref name="scene"/> to long-term memory.
        /// When <see cref="SceneNode.IncrementDate"/> is true the current date is advanced by
        /// <see cref="SceneNode.IncrementDays"/> days; otherwise the absolute month/day (and
        /// optionally year) values on the node are written directly.
        /// Must only be called when <c>scene.TimePasses</c> is true.
        /// </summary>
        private void ApplySceneDateToLtm(SceneNode scene)
        {
            var oldDate = _ltm.GetGameDate();

            int newYear;
            Month newMonth;
            int newDay;

            if (scene.IncrementDate)
            {
                // Advance the current date by the configured number of days.
                var dt = new DateTime(oldDate.year, oldDate.month, oldDate.day).AddDays(
                    scene.IncrementDays
                );
                newYear = dt.Year;
                newMonth = (Month)(dt.Month - 1);
                newDay = dt.Day;
            }
            else
            {
                // Set to the absolute date recorded on the node.
                newMonth = scene.MonthForThisScene;
                newDay = scene.DayForThisScene;

                if (scene.HasYear)
                {
                    newYear = scene.YearForThisScene;
                }
                else
                {
                    // No year pinned — advance to the next *occurrence* of this month/day.
                    // If the target falls later in the same calendar year, keep the current year.
                    // If it has already passed (or is the same day and the intent is to
                    // stay in place), roll forward to the next year.
                    int targetMonthInt = (int)newMonth + 1; // Month enum is 0-based; LTM is 1-based
                    bool alreadyPassedThisYear =
                        targetMonthInt < oldDate.month
                        || (targetMonthInt == oldDate.month && newDay < oldDate.day);
                    newYear = alreadyPassedThisYear ? oldDate.year + 1 : oldDate.year;
                }
            }

            int newMonthInt = (int)newMonth + 1;
            if (newYear != oldDate.year || newMonthInt != oldDate.month || newDay != oldDate.day)
            {
                _ltm.SetGameDate(newYear, newMonth, newDay);
            }
        }

        #endregion
    }
}
