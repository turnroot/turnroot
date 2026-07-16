using System;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubManagerSwitcher : MonoBehaviour
    {
        public HubManagerChapter[] hubManagerChapters;
        private HubManager _activeHubManager;

        private void Awake()
        {
            var brain = Utilities.GetAndCacheBrain.GetBrain();
            brain.OnSceneChanged += OnSceneChanged;
        }

        private void Start()
        {
            TryActivateHubManagerForCurrentScene();
        }

        private void OnDisable()
        {
            var brain = Utilities.GetAndCacheBrain.GetBrain();
            brain.OnSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(string sceneName, string displayName)
        {
            TryActivateHubManagerForCurrentScene();
        }

        private void TryActivateHubManagerForCurrentScene()
        {
            var brain = Utilities.GetAndCacheBrain.GetBrain();
            if (brain?.sceneFlowBrain?.CurrentScene == null)
            {
                return;
            }

            var currentChapterNumber = brain.sceneFlowBrain.CurrentScene.ChapterNumber;
            var selectedHubManager = GetHubManagerForChapter(currentChapterNumber);
            if (selectedHubManager == null)
            {
                return;
            }

            if (
                _activeHubManager == selectedHubManager
                && selectedHubManager.gameObject.activeInHierarchy
            )
            {
                return;
            }

            _activeHubManager = selectedHubManager;
            if (!_activeHubManager.gameObject.activeSelf)
            {
                _activeHubManager.gameObject.SetActive(true);
            }

            _activeHubManager.ActivateHubManager();
        }

        private HubManager GetHubManagerForChapter(int chapterNumber)
        {
            HubManagerChapter selectedHubManagerChapter = default;
            bool hasSelectedHubManagerChapter = false;

            for (int i = 0; i < hubManagerChapters.Length; i++)
            {
                var hubManagerChapter = hubManagerChapters[i];
                if (hubManagerChapter.chapterNumber > chapterNumber)
                {
                    continue;
                }

                if (
                    !hasSelectedHubManagerChapter
                    || hubManagerChapter.chapterNumber > selectedHubManagerChapter.chapterNumber
                )
                {
                    selectedHubManagerChapter = hubManagerChapter;
                    hasSelectedHubManagerChapter = true;
                }
            }

            if (hasSelectedHubManagerChapter)
            {
                return selectedHubManagerChapter.hubManager;
            }

            if (hubManagerChapters.Length > 0)
            {
                return hubManagerChapters[0].hubManager;
            }

            return null;
        }
    }
}
