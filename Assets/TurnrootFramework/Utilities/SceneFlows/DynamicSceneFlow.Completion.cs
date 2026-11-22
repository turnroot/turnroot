using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities.SceneFlows;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    public partial class DynamicSceneFlow : MonoBehaviour
    {
        #region Scene Flow Completion
        public void MarkSceneCompleteAndAdvance(bool showLoadingScreen = true)
        {
            var loadingScreen = FindFirstObjectByType<LoadingScreenController>();

            if (!showLoadingScreen)
            {
                if (loadingScreen != null)
                {
                    loadingScreen.showOnSceneTransitionStart = false;
                    _pendingLoadingScreenRestore = loadingScreen;
                }
            }
            else
            {
                loadingScreen?.Show();
            }

            FlowBrain.AdvanceToNextScene();
        }

        public void MarkSceneCompleteAndAdvanceLoadingScreen() =>
            MarkSceneCompleteAndAdvance(showLoadingScreen: true);

        public void GoToCredits()
        {
            FindFirstObjectByType<LoadingScreenController>()?.Show();
            FlowBrain.GoToCredits();
        }

        #endregion
    }
}