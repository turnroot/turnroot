using UnityEngine;
using Turnroot.Gameplay.Combat;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubManagerSwitcher : MonoBehaviour
    {
        public HubManagerChapter[] hubManagerChapters;
        private HubManager _activeHubManager;
        private Brain.Brain _brain;
        private bool _subscribedToSceneChanged;
        private string _lastActivatedSceneName;

        private void Awake()
        {
            TrySubscribeToBrainSceneChanged();
        }

        private void OnEnable()
        {
            TrySubscribeToBrainSceneChanged();
        }

        private void Start()
        {
            // Fallback for scenes that arrive before the switcher's subscription.
            TryActivateHubManagerForCurrentScene();
        }

        private void OnDisable() => UnsubscribeFromBrainSceneChanged();

        private void OnDestroy() => UnsubscribeFromBrainSceneChanged();

        private void TrySubscribeToBrainSceneChanged()
        {
            if (_subscribedToSceneChanged)
            {
                return;
            }

            _brain ??= Brain.Brain.Instance;
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
            _brain ??= Brain.Brain.Instance;
            if (_brain?.sceneFlowBrain == null)
            {
                return;
            }

            if (_brain.sceneFlowBrain.CurrentSceneKind != GameSceneKind.Hub)
            {
                if (_activeHubManager != null)
                {
                    _activeHubManager.gameObject.SetActive(false);
                }
                return;
            }

            var currentChapterNumber = _brain.sceneFlowBrain.CurrentChapterNumber;
            var selectedHubManager = GetHubManagerForChapter(currentChapterNumber);
            if (selectedHubManager == null)
            {
                return;
            }

            if (
                _activeHubManager == selectedHubManager
                && selectedHubManager.gameObject.activeInHierarchy
                && _lastActivatedSceneName == _brain.sceneFlowBrain.CurrentSceneName
            )
            {
                HubManager.SetCurrent(selectedHubManager);
                return;
            }

            if (_activeHubManager != null && _activeHubManager != selectedHubManager)
            {
                _activeHubManager.gameObject.SetActive(false);
            }

            _activeHubManager = selectedHubManager;
            HubManager.SetCurrent(_activeHubManager);
            if (!_activeHubManager.gameObject.activeSelf)
            {
                _activeHubManager.gameObject.SetActive(true);
            }

            _lastActivatedSceneName = _brain.sceneFlowBrain.CurrentSceneName;
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
