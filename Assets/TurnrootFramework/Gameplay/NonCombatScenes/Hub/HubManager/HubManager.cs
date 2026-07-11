using System;
using NaughtyAttributes;
using TMPro;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.NonCombatScenes.Abstract;
using Turnroot.UI;
using Turnroot.UI.Components.Notifications;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Weather;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct HubTeleportPoint
    {
        public HubSublocationName Name;
        public Transform Point;
    }

    [Serializable]
    public struct DoThingAtChapter
    {
        public int Chapter;
        public UnityEvent Event;
    }

    [RequireComponent(typeof(UiInputProvider))]
    [RequireComponent(typeof(SpecificUiHandler))]
    [RequireComponent(typeof(FastTravelManager))]
    /// <remarks>
    /// This may need editing for your project, but if you aren't making major logic changes, you should
    /// be able to wrangle it to work for you just with UI changes and inspector stuff
    /// </remarks>
    public partial class HubManager : MonoBehaviour
    {
        #region Fields

        [Foldout("Core")]
        [HideInInspector]
        public Brain.Brain _brain;

        [Foldout("Core")]
        [InfoBox("Events that occur in the hub at specific chapters.")]
        public DoThingAtChapter[] DoThingsAtChapters;

        [Foldout("Core")]
        [InfoBox("Input provider used for navigating hub choices.")]
        public UiInputProvider InputProvider;

        [Foldout("Core")]
        [InfoBox("Loading screen controller used during scene transitions.")]
        public LoadingScreenController LoadingScreen;

        [Foldout("Core")]
        [InfoBox("Prefab containing the menu canvas used while settings is open.")]
        public GameObject MenuCanvasPrefab;

        [Foldout("Core")]
        public AudioClip HubBackgroundMusic;

        [Foldout("Core")]
        [InfoBox("Text element used to display the current hub date (day/month/year).")]
        public TextMeshProUGUI dateText;

        [HorizontalLine(color: EColor.Red)]
        [Foldout("Navigation Choices")]
        public UiChoice EndDay;

        [Foldout("Navigation Choices")]
        public UiChoice Exit;

        [Foldout("Battles")]
        [InfoBox(
            "Activated when a required battle's day limit has been reached and End Day is disabled."
        )]
        public GameObject ForcedBattleIndicator;

        [Foldout("Navigation Choices")]
        public UiChoice Settings;

        [Foldout("Navigation Choices")]
        [InfoBox("UiChoice for the Explore entry in the main hub menu.")]
        public UiChoice ExploreChoice;

        [Foldout("Navigation Choices")]
        [InfoBox("UiChoice for the Battlefields entry in the main hub menu.")]
        public UiChoice BattlefieldsChoice;

        [HorizontalLine(color: EColor.Orange)]
        [Foldout("Battles")]
        [InfoBox("The BattleChoiceUI component used to display and navigate available battles.")]
        public BattleChoiceUI BattleChoiceUi;

        private GameObject _menuCanvasInstance;
        private bool _settingsMenuOpen;
        private Action _menuDepthChangedHandler;
        private Character.HubCharacterManager _hubCharacterManager;
        private SceneSkyboxSetter _sceneSkyboxSetter;

        [HorizontalLine(color: EColor.Yellow)]
        [Foldout("Locations")]
        [InfoBox("Teleport destinations used by hub traversal flows.")]
        public HubTeleportPoint[] TeleportPoints;

        [Foldout("Locations")]
        [InfoBox("Tutorial shown the first time Explore is entered.")]
        public GameObject ExploreTutorialPrefab;

        [Foldout("Locations")]
        public ShopsManager shopsManager;

        [Foldout("Locations")]
        public Dock dock;

        [HorizontalLine(color: EColor.Green)]
        [Foldout("Chapter Display")]
        [InfoBox("UI text used to show the current chapter number and name.")]
        public TextMeshProUGUI ChapterNumberAndNameText;

        [Foldout("Chapter Display")]
        [InfoBox("Format string for chapter display. {0} = chapter number, {1} = chapter name.")]
        public string ChapterNumberAndNameFormat = "Chapter {0}: {1}";

        [Foldout("Runtime")]
        [HideInInspector]
        public GameDate gameDate;

        [HorizontalLine(color: EColor.Blue)]
        [Foldout("Camera & Fades")]
        [InfoBox("Fade used when returning from traversal/POI interaction back to the hub.")]
        public UIFade HubFadeToBlack;

        [Foldout("Camera & Fades")]
        [InfoBox(
            "The main overlay/HUD to hide when opening any vendor UI and restore when closing it."
        )]
        public UIFade MainOverlayUiFade;

        [Foldout("Camera & Fades")]
        [InfoBox("Fade used to show/hide the hub action UI.")]
        public UIFade HubActionsFade;

        [Foldout("Camera & Fades")]
        [InfoBox("Fade used to show/hide the back button UI.")]
        public UIFade BackButtonFade;

        [Foldout("Camera & Fades")]
        [InfoBox("Field of view used for the hub camera when not in traversal zoom/POI focus.")]
        public float HubMainFov;

        [Foldout("Camera & Fades")]
        [InfoBox("Possible camera positions for randomising the hub camera on load.")]
        public Transform[] cameraPoints;

        [Foldout("Camera & Fades")]
        public Camera GeneralCamera;

        [HorizontalLine(color: EColor.Indigo)]
        [Foldout("Spawn Points")]
        public Transform TraversalStartAvatarPoint;

        [Foldout("Spawn Points")]
        [InfoBox("Collider used to sample terrain height for unit spawn points.")]
        public MeshCollider SpawnGroundCollider;

        [Foldout("Spawn Points")]
        [InfoBox("Raycast distance used when sampling spawn-point height.")]
        public float SpawnPointRaycastDistance = 20f;

        [HorizontalLine(color: EColor.Violet)]
        [Foldout("Notifications")]
        public NotificationsHelper notifications;

        private DockShipStatus[] pastShipDockedStatuses;

        private const string dockShipStatusLtmKey = "Hub_DockedShipStatuses";

        [Serializable]
        private class DockShipStatusContainer
        {
            public DockShipStatus[] statuses;
        }

        [Foldout("Runtime")]
        [HideInInspector]
        public HubInputMode CurrentInputMode = HubInputMode.None;

        private UiChoice[] _navigableChoices;

        public HubInputMode PreviousInputMode { get; private set; } = HubInputMode.None;

        public HubSublocationName? CurrentLocationName { get; private set; }
        public Transform CurrentLocationPoint { get; private set; }
        public Transform CurrentTraversalAvatarPoint { get; private set; }

        public enum HubInputMode
        {
            None,
            Location,
            Chosen,
            MarketChoice,
            Battlefields,
            Docks,
            Training,
            Traversal,
        }

        private readonly System.Collections.Generic.Dictionary<
            Transform,
            float
        > _spawnPointHeights = new();

        public void SetCurrentLocation(HubTeleportPoint teleportPoint)
        {
            var validation = OperationResultGuards.RequireNotNull(
                teleportPoint.Point,
                "teleportPoint.Point"
            );
            if (!validation.Success)
            {
                $"HubManager: Cannot set current location '{teleportPoint.Name}'. {validation.ErrorMessage}".LogError();
                return;
            }

            CurrentLocationName = teleportPoint.Name;
            CurrentLocationPoint = teleportPoint.Point;
            CurrentTraversalAvatarPoint = teleportPoint.Point;

            if (GeneralCamera != null)
            {
                GeneralCamera.transform.SetPositionAndRotation(
                    teleportPoint.Point.position,
                    teleportPoint.Point.rotation
                );
            }
        }

        public void TransitionBackToHub(UIFade fadeToBlack = null, Vector3? returnPosition = null)
        {
            void DoReturn()
            {
                var brainValidation = OperationResultGuards.RequireNotNull(_brain, nameof(_brain));
                if (!brainValidation.Success)
                {
                    $"HubManager: TransitionBackToHub aborted. {brainValidation.ErrorMessage}".LogError();
                    return;
                }

                var allPoi = FindObjectsByType<HubPoiUi>(FindObjectsSortMode.None);
                foreach (var poi in allPoi)
                {
                    poi.Hide();
                }

                GetHubCharacterManager()?.HandleHubOverviewEntered();

                _brain.audioBrain.SetMusic(HubBackgroundMusic, fadeDuration: 1f);
                GeneralCamera.fieldOfView = HubMainFov;
                BackButtonFade.Hide();

                if (returnPosition.HasValue && _avatarRoot != null)
                {
                    _avatarRoot.transform.position = returnPosition.Value;
                }
                else if (GeneralCamera != null && cameraPoints != null && cameraPoints.Length > 0)
                {
                    int idx = UnityEngine.Random.Range(0, cameraPoints.Length);
                    Transform dest = cameraPoints[idx];
                    GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
                }
                else
                {
                    $"Nothing can be done".LogError("HugManager");
                }

                CurrentLocationName = null;
                CurrentLocationPoint = null;
                CurrentTraversalAvatarPoint = null;

                SetInputMode(HubInputMode.Location);
                UpdateChoiceSelection();
                UpdateDateText();
                HubActionsFade.Show();
                _brain?.charactersBrain.CheckBirthdays();
            }

            if (fadeToBlack == null)
            {
                DoReturn();
                return;
            }

            UnityAction onVisible = null;
            UnityAction onHidden = null;

            onVisible = () =>
            {
                fadeToBlack.OnVisible.RemoveListener(onVisible);
                DoReturn();
                fadeToBlack.Hide();
            };

            onHidden = () =>
            {
                fadeToBlack.OnHidden.RemoveListener(onHidden);
            };

            fadeToBlack.OnVisible.AddListener(onVisible);
            fadeToBlack.OnHidden.AddListener(onHidden);
            fadeToBlack.Show();
        }

        private int currentIndex = 0;

        private const string birthdayNotificationTypeName = "birthday";
        private const string shipNotificationTypeName = "ship";
        private const string itemNotificationTypeName = "items";

        public void UpdateChapterNumberAndNameText(int chapterNumber, string chapterName)
        {
            if (ChapterNumberAndNameText != null)
            {
                ChapterNumberAndNameText.text = string.Format(
                    ChapterNumberAndNameFormat,
                    chapterNumber,
                    chapterName
                );
            }
        }

        public SpecificUiHandler SpecificUiInputHandler => GetComponent<SpecificUiHandler>();

        private Character.HubCharacterManager GetHubCharacterManager() =>
            _hubCharacterManager =
                _hubCharacterManager != null
                    ? _hubCharacterManager
                    : FindFirstObjectByType<Character.HubCharacterManager>();

        private SceneSkyboxSetter GetSceneSkyboxSetter() =>
            _sceneSkyboxSetter ??= FindFirstObjectByType<SceneSkyboxSetter>();

        #endregion
    }
}
