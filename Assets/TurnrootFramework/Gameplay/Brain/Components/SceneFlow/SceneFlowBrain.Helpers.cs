using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Utilities.SceneFlows
{
    public partial class SceneFlowBrain
    {
        #region Helper Methods

        private SceneTransition FindTransition(string fromSceneId, string toSceneId)
        {
            return string.IsNullOrEmpty(fromSceneId) || sceneFlowGraph == null
                ? null
                : sceneFlowGraph.transitions.Find(t =>
                    t.fromSceneId == fromSceneId && t.toSceneId == toSceneId
                    || (
                        t.isBidirectional
                        && t.toSceneId == fromSceneId
                        && t.fromSceneId == toSceneId
                    )
                );
        }

        private IEnumerator LoadSceneAsync(SceneNode targetScene, SceneTransition transition)
        {
            // Publish scene transition started event
            Brain.PublishSceneTransitionStarted(targetScene.sceneName, targetScene.displayName);

            float startTime = Time.time;

            // Wait for loading screen fade-in before starting scene load
            // This ensures the loading UI is visible and ready before the actual loading begins
            yield return new WaitForSeconds(GamewideUiSettings.Instance.LoadingFadeInTime);

            // Store the previous scene for unloading checks (preserve Brain scene)
            SceneNode previousSceneNode = _currentScene;
            string previousSceneName = _currentScene?.sceneName;

            // Snapshot the hash of every currently loaded scene BEFORE the additive load.
            // This lets us identify the new scene instance even when old and new share the
            // same Unity scene file (e.g. Hub Period 1 and Hub Period 2 both load "hub.unity").
            // Scene.GetHashCode() returns the internal scene handle, which is unique per instance.
            var sceneHandlesBeforeLoad = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                sceneHandlesBeforeLoad.Add(SceneManager.GetSceneAt(i).GetHashCode());

            // Start loading the scene additively to preserve Brain scene
            var asyncLoad = SceneManager.LoadSceneAsync(
                targetScene.sceneName,
                LoadSceneMode.Additive
            );

            if (asyncLoad == null)
            {
                $"SceneFlowBrain: Failed to start loading scene '{targetScene.sceneName}'!".LogError();
                _isTransitioning = false;
                yield break;
            }

            // Wait for scene to load and report progress
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Brain.PublishSceneLoadProgress(progress);
                yield return null;
            }

            // Identify the new and old scene instances using the pre-load handle snapshot.
            // SceneManager.GetSceneByName always returns the FIRST match, which would be the
            // OLD scene when the old and new scenes share the same Unity file name.
            Scene newScene = default;
            Scene oldScene = default;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                int hash = s.GetHashCode();
                bool isNew = !sceneHandlesBeforeLoad.Contains(hash);

                if (isNew && !newScene.IsValid() && s.name == targetScene.sceneName)
                    newScene = s;
                else if (!isNew && !oldScene.IsValid() && s.name == previousSceneName)
                    oldScene = s;
            }
            // Fallback for the edge case where the scene was already unloaded or renamed.
            if (!newScene.IsValid())
                newScene = SceneManager.GetSceneByName(targetScene.sceneName);
            if (!oldScene.IsValid() && !string.IsNullOrEmpty(previousSceneName))
                oldScene = SceneManager.GetSceneByName(previousSceneName);

            if (newScene.IsValid())
                SceneManager.SetActiveScene(newScene);

            // Disable duplicate singleton components (EventSystem, AudioListener) in the old
            // scene before it is unloaded, to suppress Unity warnings while both are loaded.
            if (oldScene.IsValid() && previousSceneName != BrainSceneName)
                DisableDuplicateComponents(oldScene);

            // Fake progress steps up to 95% - DON'T report 100% yet
            float[] fakeProgressSteps =
            {
                0.10f,
                0.25f,
                0.80f,
                0.85f,
                0.90f,
                0.91f,
                0.92f,
                0.93f,
                0.94f,
                0.95f,
            };
            float timePerStep = 0.2f;

            foreach (float step in fakeProgressSteps)
            {
                Brain.PublishSceneLoadProgress(step);
                yield return new WaitForSeconds(timePerStep);
            }

            // Ensure minimum loading time if configured
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < GamewideUiSettings.Instance.MinimumLoadingTime)
            {
                yield return new WaitForSeconds(
                    GamewideUiSettings.Instance.MinimumLoadingTime - elapsedTime
                );
            }

            // Report 100% completion so loading UI can show it
            Brain.PublishSceneLoadProgress(1.0f);

            // Give the loading UI a moment to visually display 100%
            yield return _waitForSeconds0_3;

            // Signal that the scene is ready to display - loading UIs should hide now
            Brain.PublishSceneReadyToDisplay(targetScene.sceneName, targetScene.displayName);

            // Determine whether to unload the previous scene.
            // Priority: persistWhenLeaving on the node overrides everything; otherwise the
            // transition's unloadPreviousScene flag controls it (defaults to true when no
            // transition is provided, e.g. GoBackToPreviousScene).
            bool shouldKeepPrevious =
                (previousSceneNode?.persistWhenLeaving ?? false)
                || !(transition?.unloadPreviousScene ?? true);

            if (oldScene.IsValid() && previousSceneName != BrainSceneName && !shouldKeepPrevious)
            {
                $"SceneFlowBrain: Unloading previous scene '{previousSceneName}'".LogInfo();
                SceneManager.UnloadSceneAsync(oldScene);
            }
            else if (!oldScene.IsValid() || previousSceneName == BrainSceneName)
            {
                $"SceneFlowBrain: Skipping unload — no previous scene or it is the Brain scene.".LogInfo();
            }
            else
            {
                $"SceneFlowBrain: Keeping '{previousSceneName}' loaded (persist={previousSceneNode?.persistWhenLeaving}, transition.unload={transition?.unloadPreviousScene ?? true}).".LogInfo();
            }

            // Update current scene and apply all arrival side effects (date, hub flags,
            // HubDayCompleted event, chapter).  ApplySceneArrivalSideEffects is guarded by
            // _lastSideEffectsSceneId, so if the scene component also calls SetCurrentScene
            // for this same transition the effects will not double-fire.
            _currentScene = targetScene;
            ApplySceneArrivalSideEffects(targetScene);

            // Publish scene transition completed event
            Brain.PublishSceneTransitionCompleted(targetScene.sceneName, targetScene.displayName);
            Brain.PublishSceneChanged(targetScene.sceneName, targetScene.displayName);

            _isTransitioning = false;
            $"SceneFlowBrain: Loaded scene '{targetScene.displayName}' ({targetScene.sceneName})".LogInfo();
        }

        /// <summary>
        /// Disables duplicate singleton components in the specified scene to avoid Unity warnings.
        /// This is called on the old scene after the new scene becomes active, but before unloading.
        /// </summary>
        private void DisableDuplicateComponents(Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObject in rootObjects)
            {
                // Disable EventSystem components
                UnityEngine.EventSystems.EventSystem[] eventSystems =
                    rootObject.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
                foreach (var eventSystem in eventSystems)
                {
                    if (eventSystem != null && eventSystem.enabled)
                    {
                        eventSystem.enabled = false;
                    }
                }

                // Disable AudioListener components
                AudioListener[] audioListeners = rootObject.GetComponentsInChildren<AudioListener>(
                    true
                );
                foreach (var audioListener in audioListeners)
                {
                    if (audioListener != null && audioListener.enabled)
                    {
                        audioListener.enabled = false;
                    }
                }
            }
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

                if (stateIsActive)
                {
                    return stateIsActive == condition.expectedBoolValue;
                }

                // If the key isn't a real brain state, fall back to custom flags so designers can use
                // BrainStateBool condition type with a custom flag key.
                return _brain.GetCustomFlag(condition.conditionKey) == condition.expectedBoolValue;
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

        public override string ToString() => $"{displayName} - {label}";
    }
}
