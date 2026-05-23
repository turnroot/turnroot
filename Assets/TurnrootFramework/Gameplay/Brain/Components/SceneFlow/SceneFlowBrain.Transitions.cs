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
        public void TransitionToScene(string targetSceneId) =>
            TransitionToScene(targetSceneId, bypassConditions: false);

        /// <param name="bypassConditions">
        /// When <c>true</c> transition conditions are not evaluated.
        /// Use for forced/scripted transitions such as returning to hub after a battle.
        /// </param>
        private void TransitionToScene(string targetSceneId, bool bypassConditions)
        {
            if (_isTransitioning)
            {
                $"SceneFlowBrain: Transition to '{targetSceneId}' ignored — a transition is already in progress.".LogWarning();
                return;
            }

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

            var transition = FindTransition(_currentScene?.id, targetSceneId);
            if (transition == null && _currentScene != null)
            {
                $"SceneFlowBrain: No transition defined from '{_currentScene.id}' to '{targetSceneId}' — proceeding anyway.".LogWarning();
            }

            // Determine direction for bidirectional transitions.
            bool isReverse =
                transition != null
                && transition.isBidirectional
                && transition.toSceneId == _currentScene?.id;

            // Evaluate conditions unless the caller explicitly bypasses them.
            if (transition != null && !bypassConditions)
            {
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

            // Hub <-> EOHD transitions form a day-loop; exclude from history to prevent
            // the stack growing unboundedly with each hub day.
            bool isHubDayCycle =
                (_currentScene?.isHub == true && targetScene.isEndOfHubDay)
                || (_currentScene?.isEndOfHubDay == true && targetScene.isHub);

            if (
                !isHubDayCycle
                && _currentScene != null
                && (_sceneHistory.Count == 0 || _sceneHistory.Peek() != _currentScene.id)
            )
            {
                _sceneHistory.Push(_currentScene.id);
            }

            // Apply the brain state defined on the transition (e.g. "Combat" when going to
            // a battle scene, "Hub" when returning to the monastery).
            ApplyTransitionBrainState(transition, isReverse);

            _isTransitioning = true;
            StartCoroutine(LoadSceneAsync(targetScene, transition));
        }

        public void TransitionToSceneByName(string sceneName)
        {
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            // Use the same disambiguation as SetCurrentSceneByName so that calling
            // TransitionToSceneByName("hub") from a battle scene navigates to the hub of
            // the current game period, not always the first hub node in the graph.
            var matches = sceneFlowGraph.scenes.FindAll(s => s.sceneName == sceneName);
            if (matches.Count == 0)
            {
                $"SceneFlowBrain: Scene with name '{sceneName}' not found in graph!".LogError();
                return;
            }

            var targetScene =
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

            TransitionToScene(targetScene.id);
        }

        public void GoBackToPreviousScene()
        {
            if (_isTransitioning)
            {
                "SceneFlowBrain: GoBack ignored — a transition is already in progress.".LogWarning();
                return;
            }

            if (!CanGoBack)
            {
                "SceneFlowBrain: No previous scene in history to return to.".LogWarning();
                return;
            }

            // Skip any stale history entries whose scenes have been removed from the graph.
            SceneNode previousScene = null;
            while (_sceneHistory.Count > 0 && previousScene == null)
            {
                var id = _sceneHistory.Pop();
                previousScene = sceneFlowGraph?.GetScene(id);
                if (previousScene == null)
                    $"SceneFlowBrain: History entry '{id}' no longer in graph — skipping.".LogWarning();
            }

            if (previousScene == null)
            {
                "SceneFlowBrain: No valid previous scene found in history.".LogWarning();
                return;
            }

            // Bypass condition checking and history push for back navigation.
            _isTransitioning = true;
            StartCoroutine(LoadSceneAsync(previousScene, null));
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
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            // Already at a hub — nothing to do.
            if (_currentScene?.isHub == true)
                return;

            // With multiple hub instances, the correct hub for the current game period is the
            // most recently visited one in navigation history.
            foreach (var historyId in _sceneHistory.ToArray())
            {
                var historyScene = sceneFlowGraph.GetScene(historyId);
                if (historyScene != null && historyScene.isHub)
                {
                    // Bypass conditions — returning to hub after a battle must always succeed.
                    TransitionToScene(historyId, bypassConditions: true);
                    return;
                }
            }

            // Fall back to the first hub in the graph.
            var hubScene = sceneFlowGraph.scenes?.FirstOrDefault(s => s.isHub);
            if (hubScene != null)
                TransitionToScene(hubScene.id, bypassConditions: true);
            else
                "SceneFlowBrain: No hub scene found in graph!".LogError();
        }

        /// <summary>
        /// Transitions to the End Of Hub Day scene connected from the current hub.
        /// If the current scene is not a hub, falls back to any EOHD scene in the graph.
        /// </summary>
        public void EndHubDay()
        {
            if (sceneFlowGraph == null)
            {
                "SceneFlowBrain: No scene flow graph assigned!".LogError();
                return;
            }

            // Find the EOHD node connected directly from the current hub scene.
            // EndHubDay() is a direct programmatic API call — bypass transition conditions
            // so it always succeeds regardless of any designer-placed condition flags.
            if (_currentScene?.isHub == true)
            {
                var eohdTransition = sceneFlowGraph.transitions?.Find(t =>
                    t.fromSceneId == _currentScene.id
                    && (sceneFlowGraph.GetScene(t.toSceneId)?.isEndOfHubDay ?? false)
                );
                if (eohdTransition != null)
                {
                    TransitionToScene(eohdTransition.toSceneId, bypassConditions: true);
                    return;
                }
            }

            // Fall back to any EOHD scene in the graph
            var eohdScene = sceneFlowGraph.scenes?.FirstOrDefault(s => s.isEndOfHubDay);
            if (eohdScene != null)
                TransitionToScene(eohdScene.id, bypassConditions: true);
            else
                "SceneFlowBrain: No End Of Hub Day scene found in graph!".LogError();
        }

        #endregion

        #region Brain State Helpers

        /// <summary>
        /// Activates the brain state specified on <paramref name="transition"/> (or its reverse
        /// if <paramref name="isReverse"/> is true). Does nothing when the state string is empty
        /// or StateBrain is unavailable.
        /// State format: "StateName" for a top-level state, "Parent.Child" for a child state.
        /// </summary>
        private void ApplyTransitionBrainState(SceneTransition transition, bool isReverse)
        {
            if (transition == null)
                return;
            var targetState = isReverse
                ? transition.targetBrainStateReverse
                : transition.targetBrainState;
            if (string.IsNullOrEmpty(targetState))
                return;

            var stateBrain = Brain?.stateBrain;
            if (stateBrain == null)
            {
                $"SceneFlowBrain: Cannot apply brain state '{targetState}' — stateBrain is null.".LogWarning();
                return;
            }

            if (targetState.Contains("."))
            {
                var parts = targetState.Split('.');
                if (parts.Length == 2)
                {
                    stateBrain.ActivateChildStateByFullPath(parts[0], parts[1]);
                    return;
                }
            }

            stateBrain.ActivateHighLevelState(targetState);
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
