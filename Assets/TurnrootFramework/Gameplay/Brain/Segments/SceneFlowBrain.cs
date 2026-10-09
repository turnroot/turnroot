using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Utilities.SceneFlows
{
    /// <summary>
    /// Brain component that drives the game loop defined by <see cref="GameFlowRegistry"/>:
    /// game start -> hub &lt;-&gt; end of hub day (with battles as needed) -> credits.
    /// Tracks the current scene, which battles are unlocked/completed, the current chapter,
    /// and custom flags. Provides UnityEvent-compatible navigation methods.
    /// </summary>
    [RequireComponent(typeof(Brain))]
    public partial class SceneFlowBrain : BrainComponent
    {
        private static WaitForSeconds _waitForSeconds0_3 = new(0.3f);

        // reference to storage system for dates/flags
        private LongTermMemory _ltm;

        private string _currentSceneName;
        private readonly Dictionary<string, bool> _customFlags = new();

        // Tracks which scene last had arrival side-effects applied, so that when both
        // LoadSceneAsync and a scene component call SetCurrentSceneByName for the same
        // transition the side effects only fire once.
        private string _lastSideEffectsSceneName;

        // Guards against concurrent scene transitions (e.g. player spam-clicking a button).
        // Set to true when a transition starts; cleared when LoadSceneAsync finishes or aborts.
        private bool _isTransitioning;

        // The Brain scene name (matches BrainLoader constant)
        private const string BrainSceneName = "TurnrootBrain";

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        private static GameFlowRegistry Registry => GameFlowRegistry.Instance;

        private Brain EnsureBrainReference()
        {
            if (_brain == null)
            {
                _brain = GetComponent<Brain>();
            }

            return _brain;
        }

        protected override void Awake()
        {
            base.Awake();

            _ltm = GetComponent<LongTermMemory>();
            if (_ltm != null && _ltm.Initialized)
            {
                EnsureStartingDate();
                LoadFlagsFromLtm();
                RefreshChapter(force: true);
            }

            if (_brain != null)
            {
                _brain.OnLongTermMemoryInitialized += OnLtmInitialized;
            }

            if (Registry == null)
            {
                "SceneFlowBrain: No GameFlowRegistry found in Resources!".LogError();
            }
        }

        protected override void SubscribeToBrainEvents()
        {
            _brain.OnBattleCompleted += HandleBattleCompleted;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            if (_brain != null)
            {
                _brain.OnLongTermMemoryInitialized -= OnLtmInitialized;
                _brain.OnBattleCompleted -= HandleBattleCompleted;
            }
        }

        #region Current Scene

        public string CurrentSceneName => _currentSceneName;

        public GameSceneKind CurrentSceneKind =>
            Registry != null ? Registry.GetSceneKind(_currentSceneName) : GameSceneKind.Other;

        /// <summary>
        /// Records <paramref name="sceneName"/> as the current scene (typically called by a scene's
        /// own component when it loads, e.g. the game start scene) and applies arrival side effects.
        /// </summary>
        public void SetCurrentSceneByName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            _currentSceneName = sceneName;
            ApplySceneArrivalSideEffects(sceneName);
            EnsureBrainReference()?.PublishSceneChanged(sceneName, GetDisplayName(sceneName));
        }

        private static string GetDisplayName(string sceneName)
        {
            if (Registry != null && Registry.TryGetBattle(sceneName, out var battle))
            {
                return string.IsNullOrEmpty(battle.BattleName) ? sceneName : battle.BattleName;
            }

            return sceneName;
        }

        /// <summary>
        /// Applies side effects of arriving at <paramref name="sceneName"/>: the
        /// <c>HubDayCompleted</c> event when the End Of Hub Day scene is entered, and a chapter
        /// refresh. Guarded by <see cref="_lastSideEffectsSceneName"/> so it fires once per arrival.
        /// </summary>
        private void ApplySceneArrivalSideEffects(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || _lastSideEffectsSceneName == sceneName)
            {
                return;
            }

            _lastSideEffectsSceneName = sceneName;

            if (Registry != null && Registry.GetSceneKind(sceneName) == GameSceneKind.EndOfHubDay)
            {
                EnsureBrainReference()?.PublishHubDayCompleted();
            }

            RefreshChapter(force: false);
        }

        #endregion

        #region LTM / Date

        private void OnLtmInitialized()
        {
            EnsureStartingDate();
            LoadFlagsFromLtm();

            // A save file is now active, so the chapter must be (re)published even if it
            // has not changed since it was last computed.
            RefreshChapter(force: true);
        }

        private void EnsureStartingDate()
        {
            if (_ltm == null)
            {
                return;
            }

            var date = _ltm.GetGameDate();
            if (date.year == 0)
            {
                // write user-configurable starting date
                var start = GameplayGeneralSettings.Instance?.StartingGameDate ?? GameDate.Default;
                _ltm.SetGameDate(start.year, (Month)(start.month - 1), start.day);
                date = _ltm.GetGameDate();
            }

            _brain?.PublishGameDateChanged(date.year, date.month, date.day);
        }

        private const string LtmFlagPrefix = "sceneflow.flag.";

        /// <summary>
        /// Restores any custom flags that were previously persisted to LTM into the
        /// in-memory <see cref="_customFlags"/> dictionary.
        /// </summary>
        private void LoadFlagsFromLtm()
        {
            if (_ltm == null || !_ltm.Initialized)
            {
                return;
            }

            var keys = _ltm.RecallKeysByPrefix(LtmFlagPrefix);
            foreach (var ltmKey in keys)
            {
                string flagKey = ltmKey.Substring(LtmFlagPrefix.Length);
                _customFlags[flagKey] = _ltm.RecallBool(ltmKey);
                $"SceneFlowBrain: Restored flag '{flagKey}' = {_customFlags[flagKey]} from LTM.".LogInfo();
            }
        }

        #endregion

        #region Flags

        public void SetCustomFlag(string key, bool value)
        {
            _customFlags[key] = value;
            if (_ltm != null && _ltm.Initialized)
            {
                _ltm.RememberBool(LtmFlagPrefix + key, value);
            }

            $"SceneFlowBrain: Set flag '{key}' = {value}".LogInfo();
        }

        public bool GetCustomFlag(string key) =>
            _customFlags.TryGetValue(key, out bool value) && value;

        #endregion
    }
}
