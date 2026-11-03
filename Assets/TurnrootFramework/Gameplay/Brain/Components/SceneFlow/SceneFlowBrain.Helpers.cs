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
            Brain.PublishSceneTransitionStarted(targetSceneName, targetDisplayName);
            float startTime = Time.time;
            yield return new WaitForSeconds(GamewideUiSettings.Instance.LoadingFadeInTime);

            string previousSceneName = _currentSceneName;
            var sceneHandlesBeforeLoad = GetLoadedSceneHandles();
            var asyncLoad = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (asyncLoad == null)
            {
                $"SceneFlowBrain: Failed to start loading scene '{targetSceneName}'!".LogError();
                _isTransitioning = false;
                yield break;
            }

            yield return ReportSceneLoadProgress(asyncLoad);
            var (newScene, oldScene) = FindTransitionScenes(
                sceneHandlesBeforeLoad,
                targetSceneName,
                previousSceneName
            );

            ActivateLoadedScene(newScene);
            DisableOldSceneDuplicates(oldScene, previousSceneName);
            yield return ReportSceneTransitionCompletion(startTime);
            Brain.PublishSceneReadyToDisplay(targetSceneName, targetDisplayName);

            var unloadOperation = UnloadPreviousScene(oldScene, previousSceneName);
            yield return WaitForUnload(unloadOperation);
            _currentSceneName = targetSceneName;
            ApplySceneArrivalSideEffects(targetSceneName);
            Brain.PublishSceneTransitionCompleted(targetSceneName, targetDisplayName);
            Brain.PublishSceneChanged(targetSceneName, targetDisplayName);
            _isTransitioning = false;
            $"SceneFlowBrain: Loaded scene '{targetDisplayName}' ({targetSceneName})".LogInfo();
        }

        private HashSet<int> GetLoadedSceneHandles()
        {
            var sceneHandles = new HashSet<int>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                sceneHandles.Add(SceneManager.GetSceneAt(i).GetHashCode());
            }

            return sceneHandles;
        }

        private IEnumerator ReportSceneLoadProgress(AsyncOperation asyncLoad)
        {
            while (!asyncLoad.isDone)
            {
                Brain.PublishSceneLoadProgress(Mathf.Clamp01(asyncLoad.progress / 0.9f));
                yield return null;
            }
        }

        private (Scene newScene, Scene oldScene) FindTransitionScenes(
            HashSet<int> sceneHandlesBeforeLoad,
            string targetSceneName,
            string previousSceneName
        )
        {
            Scene newScene = default;
            Scene oldScene = default;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                bool isNew = !sceneHandlesBeforeLoad.Contains(scene.GetHashCode());
                if (isNew && !newScene.IsValid() && scene.name == targetSceneName)
                {
                    newScene = scene;
                }
                else if (!isNew && !oldScene.IsValid() && scene.name == previousSceneName)
                {
                    oldScene = scene;
                }
            }

            if (!newScene.IsValid())
            {
                newScene = SceneManager.GetSceneByName(targetSceneName);
            }

            if (!oldScene.IsValid() && !string.IsNullOrEmpty(previousSceneName))
            {
                oldScene = SceneManager.GetSceneByName(previousSceneName);
            }

            return (newScene, oldScene);
        }

        private static void ActivateLoadedScene(Scene newScene)
        {
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
            }
        }

        private void DisableOldSceneDuplicates(Scene oldScene, string previousSceneName)
        {
            if (oldScene.IsValid() && previousSceneName != BrainSceneName)
            {
                DisableDuplicateComponents(oldScene);
            }
        }

        private IEnumerator ReportSceneTransitionCompletion(float startTime)
        {
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
            foreach (float step in fakeProgressSteps)
            {
                Brain.PublishSceneLoadProgress(step);
                yield return new WaitForSeconds(0.2f);
            }

            float elapsedTime = Time.time - startTime;
            float minimumLoadTime = GamewideUiSettings.Instance.MinimumLoadingTime;
            if (elapsedTime < minimumLoadTime)
            {
                yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
            }

            Brain.PublishSceneLoadProgress(1f);
            yield return _waitForSeconds0_3;
        }

        private AsyncOperation UnloadPreviousScene(Scene oldScene, string previousSceneName)
        {
            if (oldScene.IsValid() && previousSceneName != BrainSceneName)
            {
                $"SceneFlowBrain: Unloading previous scene '{previousSceneName}'".LogInfo();
                return SceneManager.UnloadSceneAsync(oldScene);
            }

            "SceneFlowBrain: Skipping unload â€” no previous scene or it is the Brain scene.".LogInfo();
            return null;
        }

        private static IEnumerator WaitForUnload(AsyncOperation unloadOperation)
        {
            while (unloadOperation != null && !unloadOperation.isDone)
            {
                yield return null;
            }
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
