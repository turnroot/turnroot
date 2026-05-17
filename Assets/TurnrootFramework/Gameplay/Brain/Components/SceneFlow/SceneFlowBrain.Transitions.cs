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
            if (transition != null)
            {
                bool isReverse =
                    transition.isBidirectional && transition.toSceneId == _currentScene?.id;
                bool conditionsMet = isReverse
                    ? transition.AreReverseConditionsMet(_conditionEvaluator)
                    : transition.AreConditionsMet(_conditionEvaluator);

                if (!conditionsMet)
                {
                    $"SceneFlowBrain: Transition conditions not met for '{targetSceneId}'.".LogWarning();
                    Brain.PublishSceneTransitionBlocked(targetSceneId, "Conditions not met");
                    return;
                }
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

        public void ClearHistory()
        {
            _sceneHistory.Clear();
            "SceneFlowBrain: Navigation history cleared.".LogInfo();
        }

        #endregion

        #region Specific Transition Methods
        public void GoToScene(string sceneId) => TransitionToScene(sceneId);

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

        public List<SceneOption> GetAvailableScenes()
        {
            if (_currentScene == null || sceneFlowGraph == null)
            {
                "SceneFlowBrain: GetAvailableScenes called but current scene or graph is null".LogWarning();
                return new List<SceneOption>();
            }

            var transitions = sceneFlowGraph.transitions;
            $"SceneFlowBrain: Current scene '{_currentScene.id}', transitions: {transitions?.Count ?? 0}".LogInfo();

            var available = new List<SceneOption>();

            foreach (var transition in transitions)
            {
                // When a transition is bidirectional, allow it to be used from either side.
                bool isForward = transition.fromSceneId == _currentScene.id;
                bool isReverse =
                    transition.isBidirectional && transition.toSceneId == _currentScene.id;

                if (!isForward && !isReverse)
                {
                    continue;
                }

                bool conditionsMet = isForward
                    ? transition.AreConditionsMet(_conditionEvaluator)
                    : transition.AreReverseConditionsMet(_conditionEvaluator);

                if (!conditionsMet)
                {
                    $"SceneFlowBrain: Transition not met: {transition.fromSceneId} -> {transition.toSceneId}".LogInfo();
                    continue;
                }

                // Determine which scene is the target based on direction.
                string targetSceneId = isForward ? transition.toSceneId : transition.fromSceneId;
                var targetScene = sceneFlowGraph.GetScene(targetSceneId);
                if (targetScene == null)
                {
                    continue;
                }

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

            return available;
        }

        public bool IsSceneAvailable(string targetSceneId)
        {
            var transitions = sceneFlowGraph?.transitions;
            if (transitions == null)
            {
                return false;
            }

            var transition = transitions.Find(t =>
                t.fromSceneId == _currentScene?.id && t.toSceneId == targetSceneId
                || (
                    t.isBidirectional
                    && t.toSceneId == _currentScene?.id
                    && t.fromSceneId == targetSceneId
                )
            );

            if (transition == null)
            {
                return false;
            }

            bool isReverse =
                transition.isBidirectional && transition.toSceneId == _currentScene?.id;
            return isReverse
                ? transition.AreReverseConditionsMet(_conditionEvaluator)
                : transition.AreConditionsMet(_conditionEvaluator);
        }

        #endregion

        #region Condition Management

        public void SetCustomFlag(string key, bool value)
        {
            _customFlags[key] = value;
            $"SceneFlowBrain: Set flag '{key}' = {value}".LogInfo();
        }

        public void SetCustomIntValue(string key, int value)
        {
            _customIntValues[key] = value;
            $"SceneFlowBrain: Set int '{key}' = {value}".LogInfo();
        }

        public void SetCustomStringValue(string key, string value)
        {
            _customStringValues[key] = value;
            $"SceneFlowBrain: Set string '{key}' = '{value}'".LogInfo();
        }

        public bool GetCustomFlag(string key) =>
            _customFlags.TryGetValue(key, out bool value) && value;

        public int GetCustomIntValue(string key) =>
            _customIntValues.TryGetValue(key, out int value) ? value : 0;

        public string GetCustomStringValue(string key) =>
            _customStringValues.TryGetValue(key, out string value) ? value : "";

        #endregion
    }
}
