using System;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct HubManagerChapter
    {
        public HubManager hubManager;
        public int chapterNumber;
    }

    public class HubManagerSwitcher : MonoBehaviour
    {
        public HubManagerChapter[] hubManagerChapters;

        private void Awake()
        {
            var brain = Utilities.GetAndCacheBrain.GetBrain();
            brain.OnSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(string sceneName, string displayName)
        {
            var brain = Utilities.GetAndCacheBrain.GetBrain();
            for (int i = 0; i < hubManagerChapters.Length; i++)
            {
                if (
                    hubManagerChapters[i].chapterNumber
                    == brain.sceneFlowBrain.CurrentScene.ChapterNumber
                )
                {
                    hubManagerChapters[i].hubManager.ActivateHubManager();
                    break;
                }
            }
        }
    }
}
