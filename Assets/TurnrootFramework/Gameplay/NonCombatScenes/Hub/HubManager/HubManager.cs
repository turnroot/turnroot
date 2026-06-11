using System;
using NaughtyAttributes;
using TMPro;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
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

    [RequireComponent(typeof(UiInputProvider))]
    [RequireComponent(typeof(HubTeamLocations))]
    [RequireComponent(typeof(HubSubInput))]
    [RequireComponent(typeof(SpecificUiHandler))]
    /// <remarks>
    /// This may need editing for your project, but if you aren't making major logic changes, you should
    /// be able to wrangle it to work for you just with UI changes and inspector stuff
    /// </remarks>
    public partial class HubManager : MonoBehaviour
    {
        #region Fields

        [BoxGroup("Core")]
        [HideInInspector]
        public Brain.Brain _brain;

        [BoxGroup("Core")]
        [InfoBox("Input provider used for navigating hub choices.")]
        public UiInputProvider InputProvider;

        [BoxGroup("Core")]
        [InfoBox("Loading screen controller used during scene transitions.")]
        public LoadingScreenController LoadingScreen;

        [BoxGroup("Core")]
        [InfoBox("Prefab containing the menu canvas used while settings is open.")]
        public GameObject MenuCanvasPrefab;

        [BoxGroup("Core")]
        public AudioClip HubBackgroundMusic;

        [BoxGroup("Core")]
        [InfoBox("Text element used to display the current hub date (day/month/year).")]
        public TextMeshProUGUI dateText;

        [HorizontalLine(color: EColor.Red)]
        [BoxGroup("Navigation Choices")]
        public UiChoice EndDay;

        [BoxGroup("Navigation Choices")]
        [InfoBox(
            "Activated when a required battle's day limit has been reached and End Day is disabled."
        )]
        public GameObject ForcedBattleIndicator;

        [BoxGroup("Navigation Choices")]
        public UiChoice Settings;

        [BoxGroup("Navigation Choices")]
        [InfoBox("UiChoice for the Explore entry in the main hub menu.")]
        public UiChoice ExploreChoice;

        [BoxGroup("Navigation Choices")]
        [InfoBox("UiChoice for the Battlefields entry in the main hub menu.")]
        public UiChoice BattlefieldsChoice;

        [HorizontalLine(color: EColor.Orange)]
        [BoxGroup("Battles")]
        [InfoBox("The BattleChoiceUI component used to display and navigate available battles.")]
        public BattleChoiceUI BattleChoiceUi;

        private GameObject _menuCanvasInstance;
        private bool _settingsMenuOpen;
        private Action _menuDepthChangedHandler;
        private Character.HubCharacterManager _hubCharacterManager;
        private SceneSkyboxSetter _sceneSkyboxSetter;

        [HorizontalLine(color: EColor.Yellow)]
        [BoxGroup("Locations")]
        [InfoBox("Teleport destinations used by hub traversal flows.")]
        public HubTeleportPoint[] TeleportPoints;

        [BoxGroup("Locations")]
        [InfoBox("Tutorial shown the first time Explore is entered.")]
        public GameObject ExploreTutorialPrefab;

        [BoxGroup("Locations")]
        public ShopsManager shopsManager;

        [BoxGroup("Locations")]
        public Dock dock;

        [HorizontalLine(color: EColor.Green)]
        [BoxGroup("Chapter Display")]
        [InfoBox("UI text used to show the current chapter number and name.")]
        public TextMeshProUGUI ChapterNumberAndNameText;

        [BoxGroup("Chapter Display")]
        [InfoBox("Format string for chapter display. {0} = chapter number, {1} = chapter name.")]
        public string ChapterNumberAndNameFormat = "Chapter {0}: {1}";

        [HideInInspector]
        public GameDate gameDate;

        [HorizontalLine(color: EColor.Blue)]
        [BoxGroup("Camera & Fades")]
        public UnityEvent OnHubStartInitialize;

        [BoxGroup("Camera & Fades")]
        public UnityEvent OnHubInitialize;

        [BoxGroup("Camera & Fades")]
        [InfoBox("Fade used when returning from traversal/POI interaction back to the hub.")]
        public UIFade HubFadeToBlack;

        [BoxGroup("Camera & Fades")]
        [InfoBox(
            "The main overlay/HUD to hide when opening any vendor UI and restore when closing it."
        )]
        public UIFade MainOverlayUiFade;

        [BoxGroup("Camera & Fades")]
        [InfoBox("Fade used to show/hide the hub action UI.")]
        public UIFade HubActionsFade;

        [BoxGroup("Camera & Fades")]
        [InfoBox("Fade used to show/hide the back button UI.")]
        public UIFade BackButtonFade;

        [BoxGroup("Camera & Fades")]
        [InfoBox("Field of view used for the hub camera when not in traversal zoom/POI focus.")]
        public float HubMainFov;

        [BoxGroup("Camera & Fades")]
        [InfoBox(
            "If an explore location is Indoors, these effects will be disabled when visiting it and re-enabled when returning to the hub."
        )]
        public GameObject[] OutdoorEffects;

        [BoxGroup("Camera & Fades")]
        [InfoBox("Possible camera positions for randomising the hub camera on load.")]
        public Transform[] cameraPoints;

        [BoxGroup("Camera & Fades")]
        public Camera GeneralCamera;

        [HorizontalLine(color: EColor.Indigo)]
        [BoxGroup("Spawn Points")]
        [InfoBox(
            "Fallback avatar spawn point used when entering traversal without an active location traversal point."
        )]
        public Transform TraversalStartAvatarPoint;

        [BoxGroup("Spawn Points")]
        [InfoBox("Collider used to sample terrain height for unit spawn points.")]
        public MeshCollider SpawnGroundCollider;

        [BoxGroup("Spawn Points")]
        [InfoBox("Raycast distance used when sampling spawn-point height.")]
        public float SpawnPointRaycastDistance = 20f;

        [HorizontalLine(color: EColor.Violet)]
        [BoxGroup("Notifications")]
        public NotificationsHelper notifications;

        private DockShipStatus[] pastShipDockedStatuses;

        private const string dockShipStatusLtmKey = "Hub_DockedShipStatuses";

        [Serializable]
        private class DockShipStatusContainer
        {
            public DockShipStatus[] statuses;
        }

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
            CurrentLocationName = teleportPoint.Name;
            CurrentLocationPoint = teleportPoint.Point;
            CurrentTraversalAvatarPoint = teleportPoint.Point;

            if (GeneralCamera != null && teleportPoint.Point != null)
            {
                GeneralCamera.transform.SetPositionAndRotation(
                    teleportPoint.Point.position,
                    teleportPoint.Point.rotation
                );
            }
        }

        public void TransitionBackToHub(UIFade fadeToBlack = null)
        {
            void DoReturn()
            {
                var allPoi = FindObjectsByType<HubPoiUi>(FindObjectsSortMode.None);
                foreach (var poi in allPoi)
                {
                    poi.Hide();
                }

                GetHubCharacterManager()?.HandleHubOverviewEntered();

                _brain.audioBrain.SetMusic(HubBackgroundMusic);
                GeneralCamera.fieldOfView = HubMainFov;
                BackButtonFade.Hide();

                if (GeneralCamera != null && cameraPoints != null && cameraPoints.Length > 0)
                {
                    int idx = UnityEngine.Random.Range(0, cameraPoints.Length);
                    Transform dest = cameraPoints[idx];
                    GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
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

            UnityEngine.Events.UnityAction onVisible = null;
            UnityEngine.Events.UnityAction onHidden = null;

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

        public HubSubInput SublocationInput => GetComponent<HubSubInput>();

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
