using Turnroot.Characters;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using static Turnroot.Gameplay.NonCombatScenes.Hub.HubManager;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class HubSubLocation : MonoBehaviour
    {
        public HubSublocationName LocationName;

        private bool HasBeenVisitedEver;

        private Brain.Brain brain;
        public GameObject tutorialPrefab;

        public HubInputMode InputModeForThisLocation;

        private bool acceptingInput = false;
        public Transform[] cameraPoints;

        public CharacterInstance[] CharactersPresent;

        public Camera GeneralCamera;

        public UIFade FadeToBlack;

        public UIFade LocationsFade;
        public UIFade BackButtonFade;

        public UIFade NotificationFade;
        private string LtmKey => "HubSubLocation_Visited_" + LocationName.ToString();

        public bool CanBeVisitedToday()
        {
            return true;
        }

        public bool AcceptingInput => acceptingInput;

        public void PlayerVisit()
        {
            Debug.Log($"HubSubLocation.PlayerVisit called for {LocationName}");
            if (!HasBeenVisitedEver)
            {
                $"First time visiting {LocationName}, showing tutorial.".LogInfo();
                HasBeenVisitedEver = true;
                SaveVisitedFlag();

                if (tutorialPrefab != null)
                {
                    $"Instantiating tutorial for {LocationName}.".LogInfo();
                    Instantiate(tutorialPrefab);
                    acceptingInput = false;
                }
                else
                {
                    $"No tutorial prefab set for {LocationName}, skipping tutorial.".LogWarning();
                    // fall through to transition below
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
            $"Starting camera transition for visiting {LocationName}".LogInfo();
            if (FadeToBlack != null)
            {
                $"Using fade to black for camera transition.".LogInfo();
                FadeToBlack.OnVisible.AddListener(OnFadeVisible);
                FadeToBlack.OnHidden.AddListener(OnFadeHidden);

                FadeToBlack.Show();
                $"Camera transition initiated for {LocationName}".LogInfo();
            }
            else
            {
                $"No FadeToBlack component assigned, doing instant camera move.".LogWarning();
                MoveCameraRandom();
                acceptingInput = true;
                $"Camera transition complete for {LocationName}".LogInfo();
            }
        }

        private void OnFadeVisible()
        {
            FadeToBlack.OnVisible.RemoveListener(OnFadeVisible);

            if (LocationsFade != null)
            {
                LocationsFade.Hide();
            }
            if (BackButtonFade != null)
            {
                BackButtonFade.Show();
            }
            if (NotificationFade != null)
            {
                NotificationFade.Hide();
            }

            if (brain != null)
            {
                brain.PublishHubSublocationInputModeChange(InputModeForThisLocation);
            }

            MoveCameraRandom();
            FadeToBlack.Hide();
            $"Camera moved and fading back in for {LocationName}".LogInfo();
        }

        private void OnFadeHidden()
        {
            FadeToBlack.OnHidden.RemoveListener(OnFadeHidden);
            acceptingInput = true;
            $"Camera transition fully complete for {LocationName}, accepting input now.".LogInfo();
        }

        private void MoveCameraRandom()
        {
            $"Moving camera to a random point for {LocationName}".LogInfo();
            if (GeneralCamera == null || cameraPoints == null || cameraPoints.Length == 0)
            {
                $"Camera or camera points not set up for {LocationName}, cannot move camera.".LogError();
                return;
            }

            int idx = Random.Range(0, cameraPoints.Length);
            Transform dest = cameraPoints[idx];
            GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
            $"Camera moved to random position {idx} for {LocationName}".LogInfo();
        }
    }
}
