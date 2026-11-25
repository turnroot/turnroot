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
        private bool _subscribedToSceneReady;
        private string _lastActivatedSceneName;

        private void Awake()
        {
            TrySubscribeToBrainSceneChanged();
            TrySubscribeToBrainSceneReady();
        }

        private void OnEnable()
        {
            TrySubscribeToBrainSceneChanged();
            TrySubscribeToBrainSceneReady();
        }

        private void Start()
        {
            // Fallback for scenes that arrive before the switcher's subscription.
            TryActivateHubManagerForCurrentScene();
        }

        private void OnDisable()
        {
            UnsubscribeFromBrainSceneChanged();
            UnsubscribeFromBrainSceneReady();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBrainSceneChanged();
            UnsubscribeFromBrainSceneReady();
        }

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

        private void OnSceneReadyToDisplay(string sceneName, string displayName) =>
            TryActivateHubManagerForScene(sceneName);

        private void OnSceneChanged(string sceneName, string displayName) =>
            TryActivateHubManagerForScene(sceneName);

        private void TrySubscribeToBrainSceneReady()
        {
            if (_subscribedToSceneReady)
            {
                return;
            }

            _brain ??= Brain.Brain.Instance;
            if (_brain == null)
            {
                return;
            }

            _brain.OnSceneReadyToDisplay += OnSceneReadyToDisplay;
            _subscribedToSceneReady = true;
        }

        private void UnsubscribeFromBrainSceneReady()
        {
            if (!_subscribedToSceneReady)
            {
                return;
            }

            if (_brain != null)
            {
                _brain.OnSceneReadyToDisplay -= OnSceneReadyToDisplay;
            }
            _subscribedToSceneReady = false;
        }

        private void TryActivateHubManagerForCurrentScene() =>
            TryActivateHubManagerForScene(
                _brain?.sceneFlowBrain?.CurrentSceneName
            );

        private void TryActivateHubManagerForScene(string sceneName)
        {
            _brain ??= Brain.Brain.Instance;
            if (_brain?.sceneFlowBrain == null)
            {
                return;
            }

            var registry = GameFlowRegistry.Instance;
            if (
                string.IsNullOrEmpty(sceneName)
                || registry == null
                || registry.GetSceneKind(sceneName) != GameSceneKind.Hub
            )
            {
                if (_activeHubManager != null)
                {
                    if (HubManager.Current == _activeHubManager)
                    {
                        HubManager.SetCurrent(null);
                    }
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
                && _lastActivatedSceneName == sceneName
            )
            {
                HubManager.SetCurrent(selectedHubManager);
                return;
            }

            if (_activeHubManager != null && _activeHubManager != selectedHubManager)
            {
                if (HubManager.Current == _activeHubManager)
                {
                    HubManager.SetCurrent(null);
                }
                _activeHubManager.gameObject.SetActive(false);
            }

            _activeHubManager = selectedHubManager;
            HubManager.SetCurrent(_activeHubManager);
            if (!_activeHubManager.gameObject.activeSelf)
            {
                _activeHubManager.gameObject.SetActive(true);
            }

            _lastActivatedSceneName = sceneName;
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
