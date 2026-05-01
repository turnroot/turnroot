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
                _brain?.PublishGameDateChanged(
                    existing.year,
                    (int)existing.month + 1,
                    existing.day
                );
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
                _brain.PublishGameDateChanged(date.year, (int)date.month + 1, date.day);
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
            if (_ltm != null && _ltm.Initialized)
            {
                var oldDate = _ltm.GetGameDate();
                int newYear = oldDate.year;
                if (scene.HasYear)
                {
                    newYear = scene.YearForThisScene;
                }

                var monthEnum = scene.MonthForThisScene;
                int monthInt = (int)monthEnum + 1;
                int newDay = scene.DayForThisScene;

                if (newYear != oldDate.year || monthInt != oldDate.month || newDay != oldDate.day)
                {
                    _ltm.SetGameDate(newYear, monthEnum, newDay);
                }
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

            if (_ltm != null)
            {
                var oldDate = _ltm.GetGameDate();
                int newYear = oldDate.year;
                if (scene.HasYear)
                {
                    newYear = scene.YearForThisScene;
                }

                var monthEnum = scene.MonthForThisScene;
                int monthInt = (int)monthEnum + 1;
                int newDay = scene.DayForThisScene;

                if (newYear != oldDate.year || monthInt != oldDate.month || newDay != oldDate.day)
                {
                    _ltm.SetGameDate(newYear, monthEnum, newDay);
                }
            }

            if (scene.SpecificChapter)
            {
                Brain.PublishSetSaveFileChapter(scene.ChapterName, scene.ChapterNumber);
            }

            Brain.PublishSceneChanged(scene.sceneName, scene.displayName);
        }

        #endregion
    }
}
