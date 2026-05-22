using System;
using NaughtyAttributes;
using TMPro;
using Turnroot.Gameplay;
using Turnroot.Gameplay.NonCombatScenes.Hub.Docks;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.UI;
using Turnroot.UI.Components.Notifications;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using Turnroot.Utilities.Weather;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
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
        [InfoBox(
            "Selectable UI elements corresponding to each hub location. Order must match subLocations (excluding special choices like ExploreChoice and BattlefieldsChoice)."
        )]
        public UiChoice[] LocationChoices;

        [BoxGroup("Navigation Choices")]
        public UiChoice EndDay;

        [BoxGroup("Navigation Choices")]
        public UiChoice Settings;

        [BoxGroup("Navigation Choices")]
        [InfoBox(
            "UiChoice for the Explore entry in the main hub menu. Can be embedded in LocationChoices or left standalone."
        )]
        public UiChoice ExploreChoice;

        [BoxGroup("Navigation Choices")]
        [InfoBox(
            "UiChoice for the Battlefields entry in the main hub menu. Can be embedded in LocationChoices or left standalone."
        )]
        public UiChoice BattlefieldsChoice;

        [HorizontalLine(color: EColor.Orange)]
        [BoxGroup("Battles")]
        [InfoBox("The BattleChoiceUI component used to display and navigate available battles.")]
        public BattleChoiceUI BattleChoiceUi;

        private GameObject _menuCanvasInstance;
        private bool _settingsMenuOpen;
        private Action _menuDepthChangedHandler;

        [HorizontalLine(color: EColor.Yellow)]
        [BoxGroup("Sublocations")]
        [InfoBox(
            "All sublocation areas that can be visited from the hub. Do NOT include entries for ExploreChoice or BattlefieldsChoice here — those are special choices handled separately."
        )]
        public HubSubLocation[] subLocations;

        [BoxGroup("Sublocations")]
        [InfoBox(
            "All explore locations available in this scene. Locked ones show as unavailable in the Explore submenu."
        )]
        public HubExploreLocation[] ExploreLocations;

        [BoxGroup("Sublocations")]
        [InfoBox(
            "The carousel UI that drives the Explore submenu. Receives all input while the submenu is open."
        )]
        public ExploreMenuCarousel ExploreCarousel;

        [BoxGroup("Sublocations")]
        public ShopsManager shopsManager;

        [BoxGroup("Sublocations")]
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
        [InfoBox("Fade used when returning from a sublocation back to the hub.")]
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
        [InfoBox("Field of view used for the hub camera when not in a sublocation.")]
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

        public HubSubLocation CurrentSubLocation { get; private set; }

        public Action OnExploreMenuOpened;

        public enum HubInputMode
        {
            None,
            Location,
            Chosen,
            MarketChoice,
            Battlefields,
            Docks,
            Training,
            ExploreMisc,
            ExploreMenu,
        }

        private readonly System.Collections.Generic.Dictionary<
            Transform,
            float
        > _spawnPointHeights = new();

        public void SetCurrentSubLocation(HubSubLocation subLocation)
        {
            if (
                subLocation is HubExploreLocation exploreLocation
                && !exploreLocation.CanBeVisitedToday()
            )
            {
                $"HubManager: Blocking SetCurrentSubLocation for locked explore location {exploreLocation.LocationName}.".LogWarning();
                return;
            }

            CurrentSubLocation = subLocation;

            if (subLocations != null)
            {
                foreach (var loc in subLocations)
                {
                    if (loc != null)
                    {
                        loc.gameObject.SetActive(loc == subLocation);
                    }
                }
            }

            if (ExploreLocations != null)
            {
                foreach (var loc in ExploreLocations)
                {
                    if (loc != null)
                    {
                        loc.gameObject.SetActive(loc == subLocation);
                    }
                }
            }
        }

        public void TransitionBackToHub(UIFade fadeToBlack = null)
        {
            bool returningToExploreMenu = CurrentSubLocation is HubExploreLocation;
            bool wasIndoorExploreLocation =
                CurrentSubLocation is HubExploreLocation iel && iel.Indoors;

            void DoReturn()
            {
                if (CurrentSubLocation != null)
                {
                    foreach (var poi in CurrentSubLocation.GetComponentsInChildren<HubPoiUi>())
                    {
                        poi.Hide();
                    }
                }

                _brain.audioBrain.SetMusic(HubBackgroundMusic);
                GeneralCamera.fieldOfView = HubMainFov;
                BackButtonFade.Hide();

                if (GeneralCamera != null && cameraPoints != null && cameraPoints.Length > 0)
                {
                    int idx = UnityEngine.Random.Range(0, cameraPoints.Length);
                    Transform dest = cameraPoints[idx];
                    GeneralCamera.transform.SetPositionAndRotation(dest.position, dest.rotation);
                }

                CurrentSubLocation = null;

                // Always restore regular sublocations so the hub overview is correct.
                if (subLocations != null)
                {
                    foreach (var loc in subLocations)
                    {
                        if (loc != null)
                        {
                            loc.gameObject.SetActive(true);
                        }
                    }
                }

                // Restore explore locations too — they were deactivated by SetCurrentSubLocation.
                if (ExploreLocations != null)
                {
                    foreach (var loc in ExploreLocations)
                    {
                        if (loc != null)
                        {
                            loc.gameObject.SetActive(true);
                        }
                    }
                }

                // When returning from an indoor explore location, restore outdoor effects.
                // First re-enable all OutdoorEffects as the base outdoor state, then let
                // SceneSkyboxSetter apply the correct weather-specific particle overrides on top.
                if (wasIndoorExploreLocation)
                {
                    foreach (var effect in OutdoorEffects)
                    {
                        if (effect != null)
                        {
                            effect.SetActive(true);
                        }
                    }
                    var skyboxSetter = FindFirstObjectByType<SceneSkyboxSetter>();
                    if (skyboxSetter != null)
                    {
                        skyboxSetter.SetActiveParticles(gameDate.month);
                    }
                }

                if (returningToExploreMenu)
                {
                    SetInputMode(HubInputMode.ExploreMenu);
                    OnExploreMenuOpened?.Invoke();
                }
                else
                {
                    SetInputMode(HubInputMode.Location);
                    UpdateChoiceSelection();
                    UpdateDateText();
                    HubActionsFade.Show();
                    _brain?.charactersBrain.CheckBirthdays();
                }
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

        #endregion
    }
}
