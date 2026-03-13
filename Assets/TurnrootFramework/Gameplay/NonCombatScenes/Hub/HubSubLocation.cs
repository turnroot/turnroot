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

        private bool acceptingInput = false;
        public Transform cameraPoint;

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

            // Track the current sublocation for proper back behavior.
            var hubManager = FindFirstObjectByType<HubManager>();
            if (hubManager != null)
            {
                hubManager.SetCurrentSubLocation(this);
                hubManager.GeneralCamera.fieldOfView = hubManager.SublocationInput.normalFov;
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
                ResetCameraToCameraPoint();
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
                brain.PublishHubSublocationInputModeChange(GetSublocationChoiceMode());
            }

            ResetCameraToCameraPoint();
            FadeToBlack.Hide();
            $"Camera moved and fading back in for {LocationName}".LogInfo();
        }

        private void OnFadeHidden()
        {
            FadeToBlack.OnHidden.RemoveListener(OnFadeHidden);
            acceptingInput = true;
            $"Camera transition fully complete for {LocationName}, accepting input now.".LogInfo();
        }

        private HubInputMode GetSublocationChoiceMode()
        {
            return LocationName switch
            {
                HubSublocationName.Market => HubInputMode.MarketChoice,
                HubSublocationName.Cafe => HubInputMode.CafeChoice,
                HubSublocationName.Battlefields => HubInputMode.Battlefields,
                HubSublocationName.Docks => HubInputMode.Docks,
                HubSublocationName.Training => HubInputMode.Training,
                _ => HubInputMode.Chosen,
            };
        }

        public void ResetCameraToCameraPoint()
        {
            $"Resetting camera to a random point for {LocationName}".LogInfo();
            if (GeneralCamera == null || cameraPoint == null)
            {
                $"Camera or camera points not set up for {LocationName}, cannot move camera.".LogError();
                return;
            }

            Transform dest = cameraPoint;
            GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
        }
    }
}
