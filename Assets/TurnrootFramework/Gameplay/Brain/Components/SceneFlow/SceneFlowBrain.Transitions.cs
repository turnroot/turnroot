using System.Collections.Generic;
using System.Linq;

namespace Turnroot.Utilities.SceneFlows
{
    public partial class SceneFlowBrain
    {
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
        public bool GetCustomFlag(string key) =>
            _customFlags.TryGetValue(key, out bool value) && value;

        /// <summary>
        /// Get a custom int value.
        /// </summary>
        public int GetCustomIntValue(string key) =>
            _customIntValues.TryGetValue(key, out int value) ? value : 0;

        /// <summary>
        /// Get a custom string value.
        /// </summary>
        public string GetCustomStringValue(string key) =>
            _customStringValues.TryGetValue(key, out string value) ? value : "";

        #endregion
    }
}
