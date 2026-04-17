using System;
using Turnroot.Characters;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using static Turnroot.Gameplay.NonCombatScenes.Hub.HubManager;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct UnitSpawnEntry
    {
        [Tooltip("Transform where the unit model is placed in the hub.")]
        public Transform UnitSpawnPoint;

        [Tooltip(
            "Transform used for the avatar model spawn and as the point the unit looks toward."
        )]
        public Transform AvatarPoint;
    }

    public class HubSubLocation : MonoBehaviour
    {
        public HubSublocationName LocationName;

        private bool HasBeenVisitedEver;

        private Brain.Brain brain;
        public GameObject tutorialPrefab;

        private bool acceptingInput = false;
        private bool _tutorialActive = false;
        public Transform cameraPoint;

        [HideInInspector]
        public CharacterInstance[] CharactersPresent;

        public UnitSpawnEntry[] UnitSpawnPoints;

        public Camera GeneralCamera;
        public AudioClip SublocationMusic;

        public UIFade FadeToBlack;

        public UIFade LocationsFade;
        public UIFade BackButtonFade;

        public UIFade NotificationFade;
        private string LtmKey => "HubSubLocation_Visited_" + LocationName.ToString();

        public bool CanBeVisitedToday() => true;

        public bool AcceptingInput => acceptingInput;

        public void PlayerVisit()
        {
            if (!HasBeenVisitedEver)
            {
                HasBeenVisitedEver = true;
                SaveVisitedFlag();

                if (tutorialPrefab != null)
                {
                    var tutorialInstance = Instantiate(tutorialPrefab);
                    tutorialInstance.SetActive(true);
                    acceptingInput = false;
                    _tutorialActive = true;
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
            brain.audioBrain.SetMusic(SublocationMusic);
        }

        public void HandleOnHubSublocationTutorialCompleted()
        {
            if (!_tutorialActive)
            {
                return;
            }

            _tutorialActive = false;
            acceptingInput = true;
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
            // set all the spawn points active
            if (UnitSpawnPoints != null)
            {
                foreach (var spawnPoint in UnitSpawnPoints)
                {
                    if (spawnPoint.UnitSpawnPoint != null)
                    {
                        spawnPoint.UnitSpawnPoint.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void SaveVisitedFlag() => brain.ltm.RememberBool(LtmKey, true);

        private void DoCameraTransition()
        {
            if (FadeToBlack != null)
            {
                FadeToBlack.OnVisible.RemoveListener(OnFadeVisible);
                FadeToBlack.OnHidden.RemoveListener(OnFadeHidden);
                FadeToBlack.OnVisible.AddListener(OnFadeVisible);
                FadeToBlack.OnHidden.AddListener(OnFadeHidden);

                FadeToBlack.Show();
            }
            else
            {
                $"No FadeToBlack component assigned, doing instant camera move.".LogWarning();
                ResetCameraToCameraPoint();
                acceptingInput = true;
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
        }

        private void OnFadeHidden()
        {
            FadeToBlack.OnHidden.RemoveListener(OnFadeHidden);
            if (!_tutorialActive)
            {
                acceptingInput = true;
            }
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
                HubSublocationName.ExploreMisc => HubInputMode.ExploreMisc,
                _ => HubInputMode.Chosen,
            };
        }

        public void ResetCameraToCameraPoint()
        {
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
