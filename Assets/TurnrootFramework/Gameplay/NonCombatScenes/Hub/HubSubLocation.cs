using Turnroot.Characters;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubSubLocation : MonoBehaviour
    {
        public HubSublocationName LocationName;

        private bool HasBeenVisitedEver;

        private Brain.Brain brain;
        public GameObject tutorialPrefab;

        private bool acceptingInput = false;
        public Transform[] cameraPoints;

        public CharacterInstance[] CharactersPresent;

        public Camera GeneralCamera;

        public UIFade FadeToBlack;
        private string LtmKey => "HubSubLocation_Visited_" + LocationName.ToString();

        public bool CanBeVisitedToday()
        {
            return true;
        }

        public bool AcceptingInput => acceptingInput;

        public void PlayerVisit()
        {
            if (!HasBeenVisitedEver)
            {
                HasBeenVisitedEver = true;
                SaveVisitedFlag();

                if (tutorialPrefab != null)
                {
                    Instantiate(tutorialPrefab);
                    acceptingInput = false;
                }
            }

            brain.PublishHubSublocationVisited(LocationName);
            DoCameraTransition();
        }

        public void HandleOnHubSublocationTutorialCompleted()
        {
            acceptingInput = true;
            HasBeenVisitedEver = true;
        }

        public void Initialize(Brain.Brain brain)
        {
            this.brain = brain;
            brain.OnHubSublocationTutorialCompleted += HandleOnHubSublocationTutorialCompleted;
            HasBeenVisitedEver = brain.ltm.RecallBool(LtmKey);
        }

        public void OnDestroy()
        {
            if (brain != null)
            {
                brain.OnHubSublocationTutorialCompleted -= HandleOnHubSublocationTutorialCompleted;
            }
        }

        private void SaveVisitedFlag() => brain.ltm.RememberBool(LtmKey, true);

        private void DoCameraTransition()
        {
            if (FadeToBlack != null)
            {
                FadeToBlack.OnHidden.AddListener(OnFadeHidden);
                FadeToBlack.OnVisible.AddListener(OnFadeVisible);
                FadeToBlack.Hide();
            }
            else
            {
                MoveCameraRandom();
                acceptingInput = true;
            }
        }

        private void OnFadeHidden()
        {
            FadeToBlack.OnHidden.RemoveListener(OnFadeHidden);
            MoveCameraRandom();
            FadeToBlack.Show();
        }

        private void OnFadeVisible()
        {
            FadeToBlack.OnVisible.RemoveListener(OnFadeVisible);
            acceptingInput = true;
        }

        private void MoveCameraRandom()
        {
            if (GeneralCamera == null || cameraPoints == null || cameraPoints.Length == 0)
            {
                return;
            }

            int idx = Random.Range(0, cameraPoints.Length);
            Transform dest = cameraPoints[idx];
            GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
        }
    }
}
