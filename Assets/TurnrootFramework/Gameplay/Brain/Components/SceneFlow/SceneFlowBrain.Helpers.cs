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

        private IEnumerator LoadSceneAsync(string targetSceneName, string targetDisplayName)
        {
            // Publish scene transition started event
            Brain.PublishSceneTransitionStarted(targetSceneName, targetDisplayName);

            float startTime = Time.time;

            // Wait for loading screen fade-in before starting scene load
            // This ensures the loading UI is visible and ready before the actual loading begins
            yield return new WaitForSeconds(GamewideUiSettings.Instance.LoadingFadeInTime);

            // Store the previous scene for unloading checks (preserve Brain scene)
            string previousSceneName = _currentSceneName;

            // Snapshot the hash of every currently loaded scene BEFORE the additive load.
            // This lets us identify the new scene instance even when old and new share the
            // same Unity scene file (e.g. Hub Period 1 and Hub Period 2 both load "hub.unity").
            // Scene.GetHashCode() returns the internal scene handle, which is unique per instance.
            var sceneHandlesBeforeLoad = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                sceneHandlesBeforeLoad.Add(SceneManager.GetSceneAt(i).GetHashCode());
            }

            // Start loading the scene additively to preserve Brain scene
            var asyncLoad = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);

            if (asyncLoad == null)
            {
                $"SceneFlowBrain: Failed to start loading scene '{targetSceneName}'!".LogError();
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

                if (isNew && !newScene.IsValid() && s.name == targetSceneName)
                {
                    newScene = s;
                }
                else if (!isNew && !oldScene.IsValid() && s.name == previousSceneName)
                {
                    oldScene = s;
                }
            }
            // Fallback for the edge case where the scene was already unloaded or renamed.
            if (!newScene.IsValid())
            {
                newScene = SceneManager.GetSceneByName(targetSceneName);
            }

            if (!oldScene.IsValid() && !string.IsNullOrEmpty(previousSceneName))
            {
                oldScene = SceneManager.GetSceneByName(previousSceneName);
            }

            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
            }

            // Disable duplicate singleton components (EventSystem, AudioListener) in the old
            // scene before it is unloaded, to suppress Unity warnings while both are loaded.
            if (oldScene.IsValid() && previousSceneName != BrainSceneName)
            {
                DisableDuplicateComponents(oldScene);
            }

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
            Brain.PublishSceneReadyToDisplay(targetSceneName, targetDisplayName);

            // The previous scene is always unloaded, except for the Brain scene.
            AsyncOperation unloadOperation = null;
            if (oldScene.IsValid() && previousSceneName != BrainSceneName)
            {
                $"SceneFlowBrain: Unloading previous scene '{previousSceneName}'".LogInfo();
                unloadOperation = SceneManager.UnloadSceneAsync(oldScene);
            }
            else
            {
                "SceneFlowBrain: Skipping unload â€” no previous scene or it is the Brain scene.".LogInfo();
            }

            // Ensure transition completion fires only after the previous scene has finished
            // unloading (when an unload was requested).
            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                {
                    yield return null;
                }
            }

            // Update current scene and apply arrival side effects (hub flags, HubDayCompleted
            // event, chapter). ApplySceneArrivalSideEffects is guarded by
            // _lastSideEffectsSceneName, so if the scene component also calls
            // SetCurrentSceneByName for this same transition the effects will not double-fire.
            _currentSceneName = targetSceneName;
            ApplySceneArrivalSideEffects(targetSceneName);

            // Publish scene transition completed event
            Brain.PublishSceneTransitionCompleted(targetSceneName, targetDisplayName);
            Brain.PublishSceneChanged(targetSceneName, targetDisplayName);

            _isTransitioning = false;
            $"SceneFlowBrain: Loaded scene '{targetDisplayName}' ({targetSceneName})".LogInfo();
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
    }
}
