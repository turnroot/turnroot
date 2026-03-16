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
        private readonly System.Collections.Generic.HashSet<string> _spawnedCharacterIds = new();
        public GameObject tutorialPrefab;

        private bool acceptingInput = false;
        public Transform cameraPoint;

        [HideInInspector]
        public CharacterInstance[] CharactersPresent;

        public Transform[] UnitSpawnPoints;

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
            if (!HasBeenVisitedEver)
            {
                $"First time visiting {LocationName}, showing tutorial.".LogInfo();
                HasBeenVisitedEver = true;
                SaveVisitedFlag();

                if (tutorialPrefab != null)
                {
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

            SpawnAllCharacters();
        }

        private void SpawnAllCharacters()
        {
            if (brain == null)
            {
                $"HubSubLocation {LocationName}: Cannot spawn characters because Brain is null".LogWarning();
                return;
            }

            if (CharactersPresent == null || CharactersPresent.Length == 0)
            {
                $"HubSubLocation {LocationName}: No characters set to be present in this sublocation".LogInfo();
                return;
            }

            if (UnitSpawnPoints == null || UnitSpawnPoints.Length == 0)
            {
                $"HubSubLocation {LocationName}: No spawn points set for this sublocation".LogWarning();
                return;
            }

            // Ensure all spawn points start enabled (in case they were disabled on a previous visit).
            if (UnitSpawnPoints != null)
            {
                foreach (var p in UnitSpawnPoints)
                {
                    if (p != null)
                    {
                        p.gameObject.SetActive(true);
                    }
                }
            }

            // Randomize spawn point order so that runs are different each visit.
            var spawnPointIndices = new int[UnitSpawnPoints.Length];
            for (int i = 0; i < spawnPointIndices.Length; i++)
            {
                spawnPointIndices[i] = i;
            }
            for (int i = 0; i < spawnPointIndices.Length; i++)
            {
                int j = Random.Range(i, spawnPointIndices.Length);
                (spawnPointIndices[i], spawnPointIndices[j]) = (
                    spawnPointIndices[j],
                    spawnPointIndices[i]
                );
            }

            var hubManager = FindFirstObjectByType<HubManager>();
            TryGetComponent<HubPoiUi>(out var poiUi);

            var usedSpawnPoints = new System.Collections.Generic.HashSet<Transform>();

            for (int i = 0; i < CharactersPresent.Length; i++)
            {
                var character = CharactersPresent[i];
                if (character == null)
                {
                    $"HubSubLocation {LocationName}: CharactersPresent contains a null entry".LogWarning();
                    continue;
                }

                if (_spawnedCharacterIds.Contains(character.Id))
                {
                    // Already spawned for this sublocation.
                    continue;
                }

                var spawnPoint = UnitSpawnPoints[spawnPointIndices[i % spawnPointIndices.Length]];
                if (spawnPoint == null)
                {
                    $"HubSubLocation {LocationName}: UnitSpawnPoints contains a null entry".LogWarning();
                    continue;
                }

                usedSpawnPoints.Add(spawnPoint);

                float spawnY =
                    hubManager?.GetSpawnPointHeight(spawnPoint, spawnPoint.position.y)
                    ?? spawnPoint.position.y;
                var spawnPosition = new Vector3(
                    spawnPoint.position.x,
                    spawnY,
                    spawnPoint.position.z
                );

                // If the model already exists, just reposition it
                var existingModel = brain.unitAppearanceBrain?.GetModelForUnit(character.Id);
                if (existingModel != null)
                {
                    existingModel.transform.SetPositionAndRotation(
                        spawnPosition,
                        spawnPoint.rotation
                    );
                    existingModel.transform.SetParent(transform, worldPositionStays: true);
                    _spawnedCharacterIds.Add(character.Id);
                    continue;
                }

                var model = brain.unitAppearanceBrain?.CreateModelForUnit(character);
                if (model == null)
                {
                    $"HubSubLocation {LocationName}: Failed to create model for character".LogWarning();
                    continue;
                }

                model.transform.SetPositionAndRotation(spawnPosition, spawnPoint.rotation);
                model.transform.SetParent(transform, worldPositionStays: true);
                _spawnedCharacterIds.Add(character.Id);
                if (poiUi != null)
                {
                    poiUi.LabelText = character.CharacterTemplate.DisplayName;
                }
            }

            // Disable any unused spawn points so only active characters show POI markers
            if (UnitSpawnPoints != null)
            {
                foreach (var spawnPoint in UnitSpawnPoints)
                {
                    if (spawnPoint != null && !usedSpawnPoints.Contains(spawnPoint))
                    {
                        spawnPoint.gameObject.SetActive(false);
                    }
                }
            }
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
                    if (spawnPoint != null)
                    {
                        spawnPoint.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void SaveVisitedFlag() => brain.ltm.RememberBool(LtmKey, true);

        private void DoCameraTransition()
        {
            if (FadeToBlack != null)
            {
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
            acceptingInput = true;
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
