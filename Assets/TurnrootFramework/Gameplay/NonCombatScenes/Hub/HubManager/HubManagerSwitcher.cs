using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubManagerSwitcher : MonoBehaviour
    {
        public HubManagerChapter[] hubManagerChapters;
        private HubManager _activeHubManager;
        private Brain.Brain _brain;
        private bool _subscribedToSceneChanged;

        private void Awake() => TrySubscribeToBrainSceneChanged();

        private void Start() => TryActivateHubManagerForCurrentScene();

        private void OnDisable() => UnsubscribeFromBrainSceneChanged();

        private void OnDestroy() => UnsubscribeFromBrainSceneChanged();

        private void TrySubscribeToBrainSceneChanged()
        {
            if (_subscribedToSceneChanged)
            {
                return;
            }

            _brain ??= Utilities.GetAndCacheBrain.GetBrain();
            if (_brain == null)
            {
                return;
            }

            _brain.OnSceneChanged += OnSceneChanged;
            _subscribedToSceneChanged = true;
        }

        private void UnsubscribeFromBrainSceneChanged()
        {
            if (!_subscribedToSceneChanged)
            {
                return;
            }

            if (_brain != null)
            {
                _brain.OnSceneChanged -= OnSceneChanged;
            }

            _subscribedToSceneChanged = false;
        }

        private void OnSceneChanged(string sceneName, string displayName) =>
            TryActivateHubManagerForCurrentScene();

        private void TryActivateHubManagerForCurrentScene()
        {
            _brain ??= Utilities.GetAndCacheBrain.GetBrain();
            if (_brain?.sceneFlowBrain?.CurrentScene == null)
            {
                return;
            }

            var currentChapterNumber = _brain.sceneFlowBrain.CurrentScene.ChapterNumber;
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
                HubManager.SetCurrent(selectedHubManager);
                return;
            }

            _activeHubManager = selectedHubManager;
            HubManager.SetCurrent(_activeHubManager);
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

            return hasSelectedHubManagerChapter ? selectedHubManagerChapter.hubManager
                : hubManagerChapters.Length > 0 ? hubManagerChapters[0].hubManager
                : null;
        }
    }
}
