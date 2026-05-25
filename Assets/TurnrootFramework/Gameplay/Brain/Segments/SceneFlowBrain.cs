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
        private Dictionary<string, bool> _customFlags = new();
        private Dictionary<string, int> _customIntValues = new();
        private Dictionary<string, string> _customStringValues = new();

        // Condition evaluator instance
        private SceneFlowConditionEvaluatorImpl _conditionEvaluator;

        // Tracks which scene last had arrival side-effects applied, so that when both
        // LoadSceneAsync and a scene component call SetCurrentScene for the same transition
        // the side effects (hub flag reset, HubDayCompleted, date advance, chapter) only
        // fire once.
        private string _lastSideEffectsSceneId;

        // Guards against concurrent scene transitions (e.g. player spam-clicking a button).
        // Set to true when a transition starts; cleared when LoadSceneAsync finishes or aborts.
        private bool _isTransitioning;

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
                LoadFlagsFromLtm();
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

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents()
        {
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
                _brain?.PublishGameDateChanged(date.year, date.month, date.day);
            }

            // Re-publish the chapter for the current scene now that a save file is active.
            // ApplySceneArrivalSideEffects already ran for the current scene (e.g. game_start)
            // but at that point no save file existed yet, so PublishSetSaveFileChapter was a
            // no-op. Publishing directly here (bypassing the dedup guard) ensures the chapter
            // is correctly written as soon as the player's save slot becomes active.
            if (_currentScene != null && _currentScene.SpecificChapter)
            {
                _brain?.PublishSetSaveFileChapter(
                    _currentScene.ChapterName,
                    _currentScene.ChapterNumber
                );
            }

            LoadFlagsFromLtm();
        }

        private const string LtmFlagPrefix = "sceneflow.flag.";

        /// <summary>
        /// Restores any custom flags that were previously persisted to LTM into the
        /// in-memory <see cref="_customFlags"/> dictionary. Called both on Awake (when LTM
        /// is already initialised) and from <see cref="OnLtmInitialized"/>.
        /// </summary>
        private void LoadFlagsFromLtm()
        {
            if (_ltm == null || !_ltm.Initialized)
                return;

            var keys = _ltm.RecallKeysByPrefix(LtmFlagPrefix);
            foreach (var ltmKey in keys)
            {
                string flagKey = ltmKey.Substring(LtmFlagPrefix.Length);
                _customFlags[flagKey] = _ltm.RecallBool(ltmKey);
                $"SceneFlowBrain: Restored flag '{flagKey}' = {_customFlags[flagKey]} from LTM.".LogInfo();
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
            ApplySceneArrivalSideEffects(scene);
            Brain.PublishSceneChanged(scene.sceneName, scene.displayName);
        }

        /// <summary>
        /// Set current scene by scene name instead of ID.
        /// When multiple graph nodes share the same Unity scene name (e.g. multiple hub day
        /// instances), prefers the node already resolved by LoadSceneAsync, then the one
        /// reachable via a transition from the current scene.
        /// </summary>
        public void SetCurrentSceneByName(string sceneName)
        {
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            SceneNode scene;
            if (_currentScene != null && _currentScene.sceneName == sceneName)
            {
                // LoadSceneAsync already resolved the correct node — reuse it.
                scene = _currentScene;
            }
            else
            {
                var matches = sceneFlowGraph.scenes.FindAll(s => s.sceneName == sceneName);
                if (matches.Count == 0)
                {
                    $"SceneFlowBrain: Scene with name '{sceneName}' not found in graph!".LogError();
                    return;
                }

                // With multiple nodes, prefer the one directly reachable from the current scene.
                scene =
                    matches.Count == 1
                        ? matches[0]
                        : matches.Find(s =>
                            sceneFlowGraph.transitions.Exists(t =>
                                (t.toSceneId == s.id && t.fromSceneId == _currentScene?.id)
                                || (
                                    t.isBidirectional
                                    && t.fromSceneId == s.id
                                    && t.toSceneId == _currentScene?.id
                                )
                            )
                        ) ?? matches[0];
            }

            _currentScene = scene;
            ApplySceneArrivalSideEffects(scene);
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
                Brain.PublishGameDateChanged(newYear, newMonthInt, newDay);
            }
        }

        /// <summary>
        /// Applies all side effects triggered by arriving at <paramref name="scene"/>:
        /// date advancement, hub flag resets, <c>HubDayCompleted</c> event, and chapter changes.
        /// Uses <see cref="_lastSideEffectsSceneId"/> as a dedup guard so that when both
        /// <c>LoadSceneAsync</c> and a scene component call <c>SetCurrentScene</c> for the
        /// same transition the side effects only fire once.
        /// </summary>
        private void ApplySceneArrivalSideEffects(SceneNode scene)
        {
            if (scene == null || _lastSideEffectsSceneId == scene.id)
                return;
            _lastSideEffectsSceneId = scene.id;

            // Advance or set the game date when the scene metadata requires it.
            if (scene.TimePasses && _ltm != null && _ltm.Initialized)
            {
                ApplySceneDateToLtm(scene);
            }

            // Arriving at a hub resets the end-of-day flags so they can be re-triggered.
            if (scene.isHub)
            {
                SetCustomFlag(SceneFlowConditionKeys.ReturnToHub, false);
                SetCustomFlag(SceneFlowConditionKeys.EndHubDay, false);
            }

            // Entering the End Of Hub Day scene signals that the current hub day is done.
            if (scene.isEndOfHubDay)
            {
                Brain.PublishHubDayCompleted();
            }

            if (scene.SpecificChapter)
            {
                Brain.PublishSetSaveFileChapter(scene.ChapterName, scene.ChapterNumber);
            }
        }

        #endregion
    }
}
