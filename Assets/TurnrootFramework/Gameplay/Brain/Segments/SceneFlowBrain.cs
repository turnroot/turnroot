using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Utilities.SceneFlows
{
    /// <summary>
    /// Brain component that manages scene flow through a graph-based network system.
    /// Handles scene transitions, navigation history, and condition evaluation.
    /// Provides UnityEvent-compatible methods for inspector-based scene flow triggers.
    /// </summary>
    public class SceneFlowBrain : BrainComponent
    {
        [Header("Scene Flow Configuration")]
        [Tooltip("The scene flow graph defining available scenes and transitions.")]
        public SceneFlowGraph sceneFlowGraph;

        [Header("Runtime State")]
        [Tooltip("The current scene node in the flow.")]
        [SerializeField]
        private SceneNode _currentScene;

        [Tooltip("Navigation history for back/return functionality.")]
        private Stack<string> _sceneHistory = new();

        [Header("Custom Flags")]
        [Tooltip("Runtime flags for custom conditions (can be set by game events).")]
        [SerializeField]
        private Dictionary<string, bool> _customFlags = new();

        [SerializeField]
        private Dictionary<string, int> _customIntValues = new();

        [SerializeField]
        private Dictionary<string, string> _customStringValues = new();

        [Header("Loading Settings")]
        [Tooltip("Should scene transitions show a loading screen?")]
        public bool useLoadingScreen = true;

        [Tooltip("Minimum time (seconds) to show loading screen, even if scene loads faster.")]
        public float minimumLoadingTime = 0.5f;

        // Condition evaluator instance
        private SceneFlowConditionEvaluatorImpl _conditionEvaluator;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Normal;

        protected override void Awake()
        {
            base.Awake();
            _conditionEvaluator = new SceneFlowConditionEvaluatorImpl(this);
        }

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to any brain events relevant to scene flow
            // For example, battle completion, story progression, etc.
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // Unsubscribe from brain events
        }

        #region Current Scene & History

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
            Brain.PublishSceneChanged(scene.sceneName, scene.displayName);
        }

        #endregion

        #region Scene Transition Methods (UnityEvent Compatible)

        /// <summary>
        /// Transition to a scene by its ID in the graph.
        /// UnityEvent compatible (single string parameter).
        /// </summary>
        public void TransitionToScene(string targetSceneId)
        {
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            var targetScene = sceneFlowGraph.GetScene(targetSceneId);
            if (targetScene == null)
            {
                $"SceneFlowBrain: Target scene '{targetSceneId}' not found in graph!".LogError();
                return;
            }

            // Check if there's a valid transition
            var transition = FindTransition(_currentScene?.id, targetSceneId);
            if (transition == null && _currentScene != null)
            {
                $"SceneFlowBrain: No transition defined from '{_currentScene.id}' to '{targetSceneId}'!".LogWarning();
                // Allow transition anyway for flexibility, but log warning
            }

            // Check conditions if transition exists
            if (transition != null && !transition.AreConditionsMet(_conditionEvaluator))
            {
                $"SceneFlowBrain: Transition conditions not met for '{targetSceneId}'.".LogWarning();
                Brain.PublishSceneTransitionBlocked(targetSceneId, "Conditions not met");
                return;
            }

            // Add current scene to history (if not null and not already on top)
            if (
                _currentScene != null
                && (_sceneHistory.Count == 0 || _sceneHistory.Peek() != _currentScene.id)
            )
            {
                _sceneHistory.Push(_currentScene.id);
            }

            // Perform the transition
            StartCoroutine(LoadSceneAsync(targetScene, transition));
        }

        /// <summary>
        /// Transition to a scene by its Unity scene name (not graph ID).
        /// UnityEvent compatible (single string parameter).
        /// </summary>
        public void TransitionToSceneByName(string sceneName)
        {
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            var targetScene = sceneFlowGraph.GetSceneByName(sceneName);
            if (targetScene == null)
            {
                $"SceneFlowBrain: Scene with name '{sceneName}' not found in graph!".LogError();
                return;
            }

            TransitionToScene(targetScene.id);
        }

        /// <summary>
        /// Go back to the previous scene in navigation history.
        /// UnityEvent compatible (no parameters).
        /// </summary>
        public void GoBackToPreviousScene()
        {
            if (!CanGoBack)
            {
                "SceneFlowBrain: No previous scene in history to return to.".LogWarning();
                return;
            }

            var previousSceneId = _sceneHistory.Pop();
            var previousScene = sceneFlowGraph.GetScene(previousSceneId);

            if (previousScene == null)
            {
                $"SceneFlowBrain: Previous scene '{previousSceneId}' not found in graph!".LogError();
                return;
            }

            // Don't add to history when going back
            StartCoroutine(LoadSceneAsync(previousScene, null, addToHistory: false));
        }

        /// <summary>
        /// Clear the navigation history.
        /// UnityEvent compatible (no parameters).
        /// </summary>
        public void ClearHistory()
        {
            _sceneHistory.Clear();
            "SceneFlowBrain: Navigation history cleared.".LogInfo();
        }

        #endregion

        #region Specific Transition Methods (For Inspector UnityEvents)

        // These methods are designed to be easily called from Inspector UnityEvents
        // They follow the pattern: Go[From][To](optional parameter)

        /// <summary>
        /// Generic method to transition from current scene to a target.
        /// Can be bound to buttons/events with scene ID as parameter.
        /// </summary>
        public void GoToScene(string sceneId) => TransitionToScene(sceneId);

        /// <summary>
        /// Return to hub from current scene.
        /// Finds the first hub scene in the graph.
        /// </summary>
        public void ReturnToHub()
        {
            var hubScene = sceneFlowGraph?.scenes?.FirstOrDefault(s => s.isHub);
            if (hubScene != null)
            {
                TransitionToScene(hubScene.id);
            }
            else
            {
                "SceneFlowBrain: No hub scene found in graph!".LogError();
            }
        }

        #endregion

        #region Available Scene Options

        /// <summary>
        /// Get all currently available scene transitions from the current scene.
        /// Filters by conditions.
        /// </summary>
        public List<SceneOption> GetAvailableScenes()
        {
            if (_currentScene == null || sceneFlowGraph == null)
            {
                return new List<SceneOption>();
            }

            var transitions = sceneFlowGraph.GetTransitionsFrom(_currentScene.id);
            var available = new List<SceneOption>();

            foreach (var transition in transitions)
            {
                if (transition.AreConditionsMet(_conditionEvaluator))
                {
                    var targetScene = sceneFlowGraph.GetScene(transition.toSceneId);
                    if (targetScene != null)
                    {
                        available.Add(
                            new SceneOption
                            {
                                sceneId = targetScene.id,
                                sceneName = targetScene.sceneName,
                                displayName = targetScene.displayName,
                                label = transition.label,
                                transition = transition,
                            }
                        );
                    }
                }
            }

            return available;
        }

        /// <summary>
        /// Check if a specific scene is currently reachable from current scene.
        /// </summary>
        public bool IsSceneAvailable(string targetSceneId)
        {
            var transitions = sceneFlowGraph?.GetTransitionsFrom(_currentScene?.id);
            if (transitions == null)
            {
                return false;
            }

            var transition = transitions.Find(t => t.toSceneId == targetSceneId);
            return transition != null && transition.AreConditionsMet(_conditionEvaluator);
        }

        #endregion

        #region Condition Management

        /// <summary>
        /// Set a custom boolean flag for scene conditions.
        /// UnityEvent compatible.
        /// </summary>
        public void SetCustomFlag(string key, bool value)
        {
            _customFlags[key] = value;
            $"SceneFlowBrain: Set flag '{key}' = {value}".LogInfo();
        }

        /// <summary>
        /// Set a custom int value for scene conditions.
        /// </summary>
        public void SetCustomIntValue(string key, int value)
        {
            _customIntValues[key] = value;
            $"SceneFlowBrain: Set int '{key}' = {value}".LogInfo();
        }

        /// <summary>
        /// Set a custom string value for scene conditions.
        /// </summary>
        public void SetCustomStringValue(string key, string value)
        {
            _customStringValues[key] = value;
            $"SceneFlowBrain: Set string '{key}' = '{value}'".LogInfo();
        }

        /// <summary>
        /// Get a custom flag value.
        /// </summary>
        public bool GetCustomFlag(string key)
        {
            return _customFlags.TryGetValue(key, out bool value) && value;
        }

        /// <summary>
        /// Get a custom int value.
        /// </summary>
        public int GetCustomIntValue(string key)
        {
            return _customIntValues.TryGetValue(key, out int value) ? value : 0;
        }

        /// <summary>
        /// Get a custom string value.
        /// </summary>
        public string GetCustomStringValue(string key)
        {
            return _customStringValues.TryGetValue(key, out string value) ? value : "";
        }

        #endregion

        #region Helper Methods

        private SceneTransition FindTransition(string fromSceneId, string toSceneId)
        {
            if (string.IsNullOrEmpty(fromSceneId) || sceneFlowGraph == null)
            {
                return null;
            }

            var transitions = sceneFlowGraph.GetTransitionsFrom(fromSceneId);
            return transitions.Find(t => t.toSceneId == toSceneId);
        }

        private IEnumerator LoadSceneAsync(
            SceneNode targetScene,
            SceneTransition transition,
            bool addToHistory = true
        )
        {
            // Publish scene transition started event
            Brain.PublishSceneTransitionStarted(targetScene.sceneName, targetScene.displayName);

            float startTime = Time.time;

            // Start loading the scene
            var asyncLoad = SceneManager.LoadSceneAsync(
                targetScene.sceneName,
                LoadSceneMode.Single
            );

            if (asyncLoad == null)
            {
                $"SceneFlowBrain: Failed to start loading scene '{targetScene.sceneName}'!".LogError();
                yield break;
            }

            // Wait for scene to load
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Brain.PublishSceneLoadProgress(progress);
                yield return null;
            }

            // Ensure minimum loading time if configured
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minimumLoadingTime)
            {
                yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
            }

            // Update current scene
            _currentScene = targetScene;

            // Publish scene transition completed event
            Brain.PublishSceneTransitionCompleted(targetScene.sceneName, targetScene.displayName);
            Brain.PublishSceneChanged(targetScene.sceneName, targetScene.displayName);

            $"SceneFlowBrain: Loaded scene '{targetScene.displayName}' ({targetScene.sceneName})".LogInfo();
        }

        #endregion

        #region Condition Evaluator Implementation

        /// <summary>
        /// Internal implementation of condition evaluator that has access to Brain state.
        /// </summary>
        internal class SceneFlowConditionEvaluatorImpl : SceneFlowConditionEvaluator
        {
            private readonly SceneFlowBrain _brain;

            public SceneFlowConditionEvaluatorImpl(SceneFlowBrain brain)
            {
                _brain = brain;
            }

            public override bool EvaluateCondition(SceneCondition condition)
            {
                switch (condition.conditionType)
                {
                    case SceneConditionType.Always:
                        return true;

                    case SceneConditionType.BrainStateBool:
                        return EvaluateBrainStateBool(condition);

                    case SceneConditionType.BrainStateInt:

                        return EvaluateBrainStateInt(condition);

                    case SceneConditionType.BrainStateString:
                        return EvaluateBrainStateString(condition);

                    case SceneConditionType.CustomFlag:
                        return _brain.GetCustomFlag(condition.conditionKey)
                            == condition.expectedBoolValue;

                    default:
                        $"SceneFlowBrain: Unknown condition type {condition.conditionType}".LogWarning();
                        return false;
                }
            }

            private bool EvaluateBrainStateBool(SceneCondition condition)
            {
                // Check if a specific brain state is currently active
                // conditionKey should be a state name like "Hub", "Combat", "Paused", etc.
                // expectedBoolValue = true means "state should be active", false means "state should NOT be active"
                var stateBrain = _brain.Brain?.stateBrain;
                if (stateBrain == null)
                {
                    // Fall back to custom flags if StateBrain not available
                    return _brain.GetCustomFlag(condition.conditionKey)
                        == condition.expectedBoolValue;
                }

                var currentState = stateBrain.CurrentState;
                if (currentState == null)
                {
                    return !condition.expectedBoolValue; // No active state = false
                }

                // Check if the condition key matches either the current state name or its full path
                bool stateIsActive =
                    currentState.Name == condition.conditionKey
                    || currentState.GetFullPath() == condition.conditionKey;

                // If not current state, check if it matches the parent state
                if (!stateIsActive && currentState.Parent != null)
                {
                    stateIsActive = currentState.Parent.Name == condition.conditionKey;
                }

                return stateIsActive == condition.expectedBoolValue;
            }

            private bool EvaluateBrainStateInt(SceneCondition condition)
            {
                // StateBrain doesn't use integer values, so fall back to custom int values
                // This could be extended in the future for things like state depth, child count, etc.
                var actualValue = _brain.GetCustomIntValue(condition.conditionKey);
                return CompareInt(
                    actualValue,
                    condition.expectedIntValue,
                    condition.comparisonOperator
                );
            }

            private bool EvaluateBrainStateString(SceneCondition condition)
            {
                // Check if the current brain state matches a specific name/path
                // conditionKey options:
                //   - "CurrentStateName" - checks current state name
                //   - "CurrentStatePath" - checks current state full path
                //   - Any other key falls back to custom string values
                var stateBrain = _brain.Brain?.stateBrain;

                if (
                    stateBrain?.CurrentState == null
                    || (
                        condition.conditionKey != "CurrentStateName"
                        && condition.conditionKey != "CurrentStatePath"
                    )
                )
                {
                    // Fall back to custom string values
                    var actualValue = _brain.GetCustomStringValue(condition.conditionKey);
                    return actualValue == condition.expectedStringValue;
                }

                // Special case: check current state name or path
                var stateValue =
                    condition.conditionKey == "CurrentStateName"
                        ? stateBrain.CurrentState.Name
                        : stateBrain.CurrentState.GetFullPath();

                return stateValue == condition.expectedStringValue;
            }

            private bool CompareInt(int actual, int expected, ComparisonOperator op)
            {
                return op switch
                {
                    ComparisonOperator.Equal => actual == expected,
                    ComparisonOperator.NotEqual => actual != expected,
                    ComparisonOperator.GreaterThan => actual > expected,
                    ComparisonOperator.GreaterThanOrEqual => actual >= expected,
                    ComparisonOperator.LessThan => actual < expected,
                    ComparisonOperator.LessThanOrEqual => actual <= expected,
                    _ => false,
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents an available scene option for UI/dynamic selection.
    /// </summary>
    [Serializable]
    public class SceneOption
    {
        public string sceneId;
        public string sceneName;
        public string displayName;
        public string label;
        public SceneTransition transition;

        public override string ToString()
        {
            return $"{displayName} - {label}";
        }
    }
}
